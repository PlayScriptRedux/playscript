/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if DYNAMIC_SUPPORT

using System.Linq.Expressions;

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;
using System;

namespace PlayScript.Expando {

    /// <summary>
    /// Represents an object with members that can be dynamically added and removed at runtime.
    /// </summary>
    public sealed class ExpandoObject : IDynamicMetaObjectProvider, IDictionary<string, object>, INotifyPropertyChanged {
        internal readonly object LockObject;                          // the readonly field is used for locking the Expando object
        private ExpandoData _data;                                    // the data currently being held by the Expando object
        private int _count;                                           // the count of available members

        internal readonly static object Uninitialized = new object(); // A marker object used to identify that a value is uninitialized.

        internal const int AmbiguousMatchFound = -2;        // The value is used to indicate there exists ambiguous match in the Expando object
        internal const int NoMatch = -1;                    // The value is used to indicate there is no matching member

        private PropertyChangedEventHandler _propertyChanged;

		// class definition object for use by serialization code (typically AMF3 serialization)
		public object ClassDefinition { get; set;}

		public dynamic this [string key] {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}


        /// <summary>
        /// Creates a new ExpandoObject with no members.
        /// </summary>
        public ExpandoObject() {
            _data = ExpandoData.Empty;
            LockObject = new object();
        }

		/// <summary>
		/// Creates a new ExpandoObject with specified capacity
		/// </summary>
		public ExpandoObject(int capacity) : base() {
		}

        #region Get/Set/Delete Helpers

        /// <summary>
        /// Try to get the data stored for the specified class at the specified index.  If the
        /// class has changed a full lookup for the slot will be performed and the correct
        /// value will be retrieved.
        /// </summary>
        internal bool TryGetValue(object indexClass, int index, string name, bool ignoreCase, out object value) {
            // read the data now.  The data is immutable so we get a consistent view.
            // If there's a concurrent writer they will replace data and it just appears
            // that we won the race
            ExpandoData data = _data;
            if (data.Class != indexClass || ignoreCase) {
                /* Re-search for the index matching the name here if
                 *  1) the class has changed, we need to get the correct index and return
                 *  the value there.
                 *  2) the search is case insensitive:
                 *      a. the member specified by index may be deleted, but there might be other
                 *      members matching the name if the binder is case insensitive.
                 *      b. the member that exactly matches the name didn't exist before and exists now,
                 *      need to find the exact match.
                 */
                index = data.Class.GetValueIndex(name, ignoreCase, this);
                if (index == ExpandoObject.AmbiguousMatchFound) {
                    throw Error.AmbiguousMatchInExpandoObject(name);
                }
            }

            if (index == ExpandoObject.NoMatch) {
                value = null;
                return false;
            }

            // Capture the value into a temp, so it doesn't get mutated after we check
            // for Uninitialized.
            object temp = data[index];
            if (temp == Uninitialized) {
                value = null;
                return false;
            }

            // index is now known to be correct
            value = temp;
            return true;
        }
        
        /// <summary>
        /// Sets the data for the specified class at the specified index.  If the class has
        /// changed then a full look for the slot will be performed.  If the new class does
        /// not have the provided slot then the Expando's class will change. Only case sensitive
        /// setter is supported in ExpandoObject.
        /// </summary>
        internal void TrySetValue(object indexClass, int index, object value, string name, bool ignoreCase, bool add) {
            ExpandoData data;
            object oldValue;

            lock (LockObject) {
                data = _data;

                if (data.Class != indexClass || ignoreCase) {
                    // The class has changed or we are doing a case-insensitive search, 
                    // we need to get the correct index and set the value there.  If we 
                    // don't have the value then we need to promote the class - that 
                    // should only happen when we have multiple concurrent writers.
                    index = data.Class.GetValueIndex(name, ignoreCase, this);
                    if (index == ExpandoObject.AmbiguousMatchFound) {
                        throw Error.AmbiguousMatchInExpandoObject(name);
                    }
                    if (index == ExpandoObject.NoMatch) {
                        // Before creating a new class with the new member, need to check 
                        // if there is the exact same member but is deleted. We should reuse
                        // the class if there is such a member.
                        int exactMatch = ignoreCase ?
                            data.Class.GetValueIndexCaseSensitive(name) :
                            index;
                        if (exactMatch != ExpandoObject.NoMatch) {
                            Debug.Assert(data[exactMatch] == Uninitialized);
                            index = exactMatch;
                        } else {
                            ExpandoClass newClass = data.Class.FindNewClass(name);
                            data = PromoteClassCore(data.Class, newClass);
                            // After the class promotion, there must be an exact match,
                            // so we can do case-sensitive search here.
                            index = data.Class.GetValueIndexCaseSensitive(name);
                            Debug.Assert(index != ExpandoObject.NoMatch);
                        }
                    }
                }

                // Setting an uninitialized member increases the count of available members
                oldValue = data[index];
                if (oldValue == Uninitialized) {
                    _count++;
                } else if (add) {
                    throw Error.SameKeyExistsInExpando(name);
                }

                data[index] = value;
            }

            // Notify property changed, outside of the lock.
            var propertyChanged = _propertyChanged;
            if (propertyChanged != null && value != oldValue) {
                // Use the canonical case for the key.
                propertyChanged(this, new PropertyChangedEventArgs(data.Class.Keys[index]));
            }
        }

        /// <summary>
        /// Provides the implementation of performing a get index operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The index to be used.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        internal bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
			var key = indexes[0] as String;
			if (key != null) {
	            TryGetValue(null, -1, key, false, out result);
				return true;
			}
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a set index operation.  Derived classes can
        /// override this method to custmize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The index to be used.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        internal bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) {
			var key = indexes[0] as String;
			if (key != null) {
				TrySetValue(null, -1, value, key, false, true);
				return true;
			}
            return false;
        }

        /// <summary>
        /// Deletes the data stored for the specified class at the specified index.
        /// </summary>
        internal bool TryDeleteValue(object indexClass, int index, string name, bool ignoreCase, object deleteValue) {
            ExpandoData data;
            lock (LockObject) {
                data = _data;

                if (data.Class != indexClass || ignoreCase) {
                    // the class has changed or we are doing a case-insensitive search,
                    // we need to get the correct index.  If there is no associated index
                    // we simply can't have the value and we return false.
                    index = data.Class.GetValueIndex(name, ignoreCase, this);
                    if (index == ExpandoObject.AmbiguousMatchFound) {
                        throw Error.AmbiguousMatchInExpandoObject(name);
                    }
                }
                if (index == ExpandoObject.NoMatch) {
                    return false;
                }

                object oldValue = data[index];
                if (oldValue == Uninitialized) {
                    return false;
                }

                // Make sure the value matches, if requested.
                //
                // It's a shame we have to call Equals with the lock held but
                // there doesn't seem to be a good way around that, and
                // ConcurrentDictionary in mscorlib does the same thing.
                if (deleteValue != Uninitialized && !object.Equals(oldValue, deleteValue)) {
                    return false;
                }

                data[index] = Uninitialized;

                // Deleting an available member decreases the count of available members
                _count--;
            }

            // Notify property changed, outside of the lock.
            var propertyChanged = _propertyChanged;
            if (propertyChanged != null) {
                // Use the canonical case for the key.
                propertyChanged(this, new PropertyChangedEventArgs(data.Class.Keys[index]));
            }

            return true;
        }

        /// <summary>
        /// Returns true if the member at the specified index has been deleted,
        /// otherwise false. Call this function holding the lock.
        /// </summary>
        internal bool IsDeletedMember(int index) {
            Debug.Assert(index >= 0 && index <= _data.Length);

            if (index == _data.Length) {
                // The member is a newly added by SetMemberBinder and not in data yet
                return false;
            }

            return _data[index] == ExpandoObject.Uninitialized;
        }

        /// <summary>
        /// Exposes the ExpandoClass which we've associated with this 
        /// Expando object.  Used for type checks in rules.
        /// </summary>
        internal ExpandoClass Class {
            get {
                return _data.Class;
            }
        }

        /// <summary>
        /// Promotes the class from the old type to the new type and returns the new
        /// ExpandoData object.
        /// </summary>
        private ExpandoData PromoteClassCore(ExpandoClass oldClass, ExpandoClass newClass) {
            Debug.Assert(oldClass != newClass);

            lock (LockObject) {
                if (_data.Class == oldClass) {
                    _data = _data.UpdateClass(newClass);
                }
                return _data;
            }
        }

        /// <summary>
        /// Internal helper to promote a class.  Called from our RuntimeOps helper.  This
        /// version simply doesn't expose the ExpandoData object which is a private
        /// data structure.
        /// </summary>
        internal void PromoteClass(object oldClass, object newClass) {
            PromoteClassCore((ExpandoClass)oldClass, (ExpandoClass)newClass);
        }

        #endregion

        #region IDynamicMetaObjectProvider Members

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new MetaExpando(parameter, this);
        }
        #endregion

        #region Helper methods
        private void TryAddMember(string key, object value) {
            ContractUtils.RequiresNotNull(key, "key");
            // Pass null to the class, which forces lookup.
            TrySetValue(null, -1, value, key, false, true);
        }

        private bool TryGetValueForKey(string key, out object value) {
            // Pass null to the class, which forces lookup.
            return TryGetValue(null, -1, key, false, out value);
        }

        private bool ExpandoContainsKey(string key) {
            return _data.Class.GetValueIndexCaseSensitive(key) >= 0;
        }

        // We create a non-generic type for the debug view for each different collection type
        // that uses DebuggerTypeProxy, instead of defining a generic debug view type and
        // using different instantiations. The reason for this is that support for generics
        // with using DebuggerTypeProxy is limited. For C#, DebuggerTypeProxy supports only
        // open types (from MSDN http://msdn.microsoft.com/en-us/library/d8eyd8zc.aspx).
        private sealed class KeyCollectionDebugView {
            private ICollection<string> collection;
            public KeyCollectionDebugView(ICollection<string> collection) {
                Debug.Assert(collection != null);
                this.collection = collection;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public string[] Items {
                get {
                    string[] items = new string[collection.Count];
                    collection.CopyTo(items, 0);
                    return items;
                }
            }
        }

        [DebuggerTypeProxy(typeof(KeyCollectionDebugView))]
        [DebuggerDisplay("Count = {Count}")]
        private class KeyCollection : ICollection<string> {
            private readonly ExpandoObject _expando;
            private readonly int _expandoVersion;
            private readonly int _expandoCount;
            private readonly ExpandoData _expandoData;

            internal KeyCollection(ExpandoObject expando) {
                lock (expando.LockObject) {
                    _expando = expando;
                    _expandoVersion = expando._data.Version;
                    _expandoCount = expando._count;
                    _expandoData = expando._data;
                }
            }

            private void CheckVersion() {
                if (_expando._data.Version != _expandoVersion || _expandoData != _expando._data) {
                    //the underlying expando object has changed
                    throw Error.CollectionModifiedWhileEnumerating();
                }
            }

            #region ICollection<string> Members

            public void Add(string item) {
                throw Error.CollectionReadOnly();
            }

            public void Clear() {
                throw Error.CollectionReadOnly();
            }

            public bool Contains(string item) {
                lock (_expando.LockObject) {
                    CheckVersion();
                    return _expando.ExpandoContainsKey(item);
                }
            }

            public void CopyTo(string[] array, int arrayIndex) {
                ContractUtils.RequiresNotNull(array, "array");
                ContractUtils.RequiresArrayRange(array, arrayIndex, _expandoCount, "arrayIndex", "Count");
                lock (_expando.LockObject) {
                    CheckVersion();
                    ExpandoData data = _expando._data;
                    for (int i = 0; i < data.Class.Keys.Length; i++) {
                        if (data[i] != Uninitialized) {
                            array[arrayIndex++] = data.Class.Keys[i];
                        }
                    }
                }
            }

            public int Count {
                get {
                    CheckVersion();
                    return _expandoCount;
                }
            }

            public bool IsReadOnly {
                get { return true; }
            }

            public bool Remove(string item) {
                throw Error.CollectionReadOnly();
            }

            #endregion

            #region IEnumerable<string> Members

            public IEnumerator<string> GetEnumerator() {
                for (int i = 0, n = _expandoData.Class.Keys.Length; i < n; i++) {
                    CheckVersion();
                    if (_expandoData[i] != Uninitialized) {
                        yield return _expandoData.Class.Keys[i];
                    }
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            #endregion
        }

        // We create a non-generic type for the debug view for each different collection type
        // that uses DebuggerTypeProxy, instead of defining a generic debug view type and
        // using different instantiations. The reason for this is that support for generics
        // with using DebuggerTypeProxy is limited. For C#, DebuggerTypeProxy supports only
        // open types (from MSDN http://msdn.microsoft.com/en-us/library/d8eyd8zc.aspx).
        private sealed class ValueCollectionDebugView {
            private ICollection<object> collection;
            public ValueCollectionDebugView(ICollection<object> collection) {
                Debug.Assert(collection != null);
                this.collection = collection;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object[] Items {
                get {
                    object[] items = new object[collection.Count];
                    collection.CopyTo(items, 0);
                    return items;
                }
            }
        }

        [DebuggerTypeProxy(typeof(ValueCollectionDebugView))]
        [DebuggerDisplay("Count = {Count}")]
        private class ValueCollection : ICollection<object> {
            private readonly ExpandoObject _expando;
            private readonly int _expandoVersion;
            private readonly int _expandoCount;
            private readonly ExpandoData _expandoData;

            internal ValueCollection(ExpandoObject expando) {
                lock (expando.LockObject) {
                    _expando = expando;
                    _expandoVersion = expando._data.Version;
                    _expandoCount = expando._count;
                    _expandoData = expando._data;
                }
            }

            private void CheckVersion() {
                if (_expando._data.Version != _expandoVersion || _expandoData != _expando._data) {
                    //the underlying expando object has changed
                    throw Error.CollectionModifiedWhileEnumerating();
                }
            }

            #region ICollection<string> Members

            public void Add(object item) {
                throw Error.CollectionReadOnly();
            }

            public void Clear() {
                throw Error.CollectionReadOnly();
            }

            public bool Contains(object item) {
                lock (_expando.LockObject) {
                    CheckVersion();

                    ExpandoData data = _expando._data;
                    for (int i = 0; i < data.Class.Keys.Length; i++) {

                        // See comment in TryDeleteValue; it's okay to call
                        // object.Equals with the lock held.
                        if (object.Equals(data[i], item)) {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public void CopyTo(object[] array, int arrayIndex) {
                ContractUtils.RequiresNotNull(array, "array");
                ContractUtils.RequiresArrayRange(array, arrayIndex, _expandoCount, "arrayIndex", "Count");
                lock (_expando.LockObject) {
                    CheckVersion();
                    ExpandoData data = _expando._data;
                    for (int i = 0; i < data.Class.Keys.Length; i++) {
                        if (data[i] != Uninitialized) {
                            array[arrayIndex++] = data[i];
                        }
                    }
                }
            }

            public int Count {
                get {
                    CheckVersion();
                    return _expandoCount;
                }
            }

            public bool IsReadOnly {
                get { return true; }
            }

            public bool Remove(object item) {
                throw Error.CollectionReadOnly();
            }

            #endregion

            #region IEnumerable<string> Members

            public IEnumerator<object> GetEnumerator() {
                ExpandoData data = _expando._data;
                for (int i = 0; i < data.Class.Keys.Length; i++) {
                    CheckVersion();
                    // Capture the value into a temp so we don't inadvertently
                    // return Uninitialized.
                    object temp = data[i];
                    if (temp != Uninitialized) {
                        yield return temp;
                    }
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region IDictionary<string, object> Members
        ICollection<string> IDictionary<string, object>.Keys {
            get {
                return new KeyCollection(this);
            }
        }

        ICollection<object> IDictionary<string, object>.Values {
            get {
                return new ValueCollection(this);
            }
        }

        object IDictionary<string, object>.this[string key] {
            get {
                object value;
                if (!TryGetValueForKey(key, out value)) {
                    throw Error.KeyDoesNotExistInExpando(key);
                }
                return value;
            }
            set {
                ContractUtils.RequiresNotNull(key, "key");
                // Pass null to the class, which forces lookup.
                TrySetValue(null, -1, value, key, false, false);
            }
        }

        void IDictionary<string, object>.Add(string key, object value) {
            this.TryAddMember(key, value);
        }

        bool IDictionary<string, object>.ContainsKey(string key) {
            ContractUtils.RequiresNotNull(key, "key");

            ExpandoData data = _data;
            int index = data.Class.GetValueIndexCaseSensitive(key);
            return index >= 0 && data[index] != Uninitialized;
        }

        bool IDictionary<string, object>.Remove(string key) {
            ContractUtils.RequiresNotNull(key, "key");
            // Pass null to the class, which forces lookup.
            return TryDeleteValue(null, -1, key, false, Uninitialized);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value) {
            return TryGetValueForKey(key, out value);
        }

        #endregion

        #region ICollection<KeyValuePair<string, object>> Members
        int ICollection<KeyValuePair<string, object>>.Count {
            get {
                return _count;
            }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly {
            get { return false; }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) {
            TryAddMember(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear() {
            // We remove both class and data!
            ExpandoData data;
            lock (LockObject) {
                data = _data;
                _data = ExpandoData.Empty;
                _count = 0;
            }

            // Notify property changed for all properties.
            var propertyChanged = _propertyChanged;
            if (propertyChanged != null) {
                for (int i = 0, n = data.Class.Keys.Length; i < n; i++) {
                    if (data[i] != Uninitialized) {
                        propertyChanged(this, new PropertyChangedEventArgs(data.Class.Keys[i]));
                    }
                }
            }
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) {
            object value;
            if (!TryGetValueForKey(item.Key, out value)) {
                return false;
            }

            return object.Equals(value, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
            ContractUtils.RequiresNotNull(array, "array");
            ContractUtils.RequiresArrayRange(array, arrayIndex, _count, "arrayIndex", "Count");

            // We want this to be atomic and not throw
            lock (LockObject) {
                foreach (KeyValuePair<string, object> item in this) {
                    array[arrayIndex++] = item;
                }
            }
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) {
            return TryDeleteValue(null, -1, item.Key, false, item.Value);
        }
        #endregion

        #region IEnumerable<KeyValuePair<string, object>> Member

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
            ExpandoData data = _data;
            return GetExpandoEnumerator(data, data.Version);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            ExpandoData data = _data;
            return GetExpandoEnumerator(data, data.Version);
        }

        // Note: takes the data and version as parameters so they will be
        // captured before the first call to MoveNext().
        private IEnumerator<KeyValuePair<string, object>> GetExpandoEnumerator(ExpandoData data, int version) {
            for (int i = 0; i < data.Class.Keys.Length; i++) {
                if (_data.Version != version || data != _data) {
                    // The underlying expando object has changed:
                    // 1) the version of the expando data changed
                    // 2) the data object is changed 
                    throw Error.CollectionModifiedWhileEnumerating();
                }
                // Capture the value into a temp so we don't inadvertently
                // return Uninitialized.
                object temp = data[i];
                if (temp != Uninitialized) {
                    yield return new KeyValuePair<string,object>(data.Class.Keys[i], temp);
                }
            }
        }
        #endregion

        #region MetaExpando

        private class MetaExpando : DynamicMetaObject {
            public MetaExpando(Expression expression, ExpandoObject value)
                : base(expression, BindingRestrictions.Empty, value) {
            }

            private DynamicMetaObject BindGetOrInvokeMember(DynamicMetaObjectBinder binder, string name, bool ignoreCase, DynamicMetaObject fallback, Func<DynamicMetaObject, DynamicMetaObject> fallbackInvoke) {
                ExpandoClass klass = Value.Class;

                //try to find the member, including the deleted members
                int index = klass.GetValueIndex(name, ignoreCase, Value);

                ParameterExpression value = Expression.Parameter(typeof(object), "value");

                Expression tryGetValue = Expression.Call(
                    typeof(RuntimeOps).GetMethod("ExpandoTryGetValue"),
                    GetLimitedSelf(),
                    Expression.Constant(klass, typeof(object)),
                    Expression.Constant(index),
                    Expression.Constant(name),
                    Expression.Constant(ignoreCase),
                    value
                );

                var result = new DynamicMetaObject(value, BindingRestrictions.Empty);
                if (fallbackInvoke != null) {
					try {
	                    result = fallbackInvoke(result);
					} catch (Exception) {
						result = null;
					}
                }

                result = new DynamicMetaObject(
                    Expression.Block(
                        new[] { value },
                        Expression.Condition(
                            tryGetValue,
                            result.Expression,
                            Expression.Constant (null),
                            typeof(object)
                        )
                    ),
                    result.Restrictions
                );

                return AddDynamicTestAndDefer(binder, Value.Class, null, result);
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {
                ContractUtils.RequiresNotNull(binder, "binder");
                return BindGetOrInvokeMember(
                    binder,
                    binder.Name, 
                    binder.IgnoreCase,
                    null,
                    null
                );
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) {
                ContractUtils.RequiresNotNull(binder, "binder");
                return BindGetOrInvokeMember(
                    binder,
                    binder.Name, 
                    binder.IgnoreCase,
                    binder.FallbackInvokeMember(this, args),
                    value => binder.FallbackInvoke(value, args, null)
                );
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) {
                ContractUtils.RequiresNotNull(binder, "binder");
                ContractUtils.RequiresNotNull(value, "value");

                ExpandoClass klass;
                int index;

                ExpandoClass originalClass = GetClassEnsureIndex(binder.Name, binder.IgnoreCase, Value, out klass, out index);

                return AddDynamicTestAndDefer(
                    binder,
                    klass,
                    originalClass,
                    new DynamicMetaObject(
                        Expression.Call(
                            typeof(RuntimeOps).GetMethod("ExpandoTrySetValue"),
                            GetLimitedSelf(),
                            Expression.Constant(klass, typeof(object)),
                            Expression.Constant(index),
                            Expression.Convert(value.Expression, typeof(object)),
                            Expression.Constant(binder.Name),
                            Expression.Constant(binder.IgnoreCase)
                        ),
                        BindingRestrictions.Empty
                    )
                );
            }

            public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder) {
                ContractUtils.RequiresNotNull(binder, "binder");

                int index = Value.Class.GetValueIndex(binder.Name, binder.IgnoreCase, Value);

                Expression tryDelete = Expression.Call(
                    typeof(RuntimeOps).GetMethod("ExpandoTryDeleteValue"),
                    GetLimitedSelf(),
                    Expression.Constant(Value.Class, typeof(object)),
                    Expression.Constant(index),
                    Expression.Constant(binder.Name),
                    Expression.Constant(binder.IgnoreCase)
                );
                DynamicMetaObject fallback = binder.FallbackDeleteMember(this);

                DynamicMetaObject target = new DynamicMetaObject(
                    Expression.IfThen(Expression.Not(tryDelete), fallback.Expression),
                    fallback.Restrictions
                );

                return AddDynamicTestAndDefer(binder, Value.Class, null, target);
            }

            public override DynamicMetaObject BindGetIndex (GetIndexBinder binder, DynamicMetaObject[] indexes)
			{
				if (indexes.Length == 1) {
					var args = new Expression[] { indexes[0].Expression };
					return CallMethodWithResult ("TryGetIndex", binder, args);
				}

				return base.BindGetIndex(binder, indexes);
            }

            public override DynamicMetaObject BindSetIndex (SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
			{
				if (indexes.Length == 1) {
					var args = new Expression[] { indexes[0].Expression };
					return CallMethodReturnLast ("TrySetIndex", binder, args, value.Expression);
				}

				return base.BindSetIndex(binder, indexes, value);
            }

            public override IEnumerable<string> GetDynamicMemberNames() {
                var expandoData = Value._data;
                var klass = expandoData.Class;
                for (int i = 0; i < klass.Keys.Length; i++) {
                    object val = expandoData[i];
                    if (val != ExpandoObject.Uninitialized) {
                        yield return klass.Keys[i];
                    }
                }
            }

            /// <summary>
            /// Adds a dynamic test which checks if the version has changed.  The test is only necessary for
            /// performance as the methods will do the correct thing if called with an incorrect version.
            /// </summary>
            private DynamicMetaObject AddDynamicTestAndDefer(DynamicMetaObjectBinder binder, ExpandoClass klass, ExpandoClass originalClass, DynamicMetaObject succeeds) {

                Expression ifTestSucceeds = succeeds.Expression;
                if (originalClass != null) {
                    // we are accessing a member which has not yet been defined on this class.
                    // We force a class promotion after the type check.  If the class changes the 
                    // promotion will fail and the set/delete will do a full lookup using the new
                    // class to discover the name.
                    Debug.Assert(originalClass != klass);

                    ifTestSucceeds = Expression.Block(
                        Expression.Call(
                            null,
                            typeof(RuntimeOps).GetMethod("ExpandoPromoteClass"),
                            GetLimitedSelf(),
                            Expression.Constant(originalClass, typeof(object)),
                            Expression.Constant(klass, typeof(object))
                        ),
                        succeeds.Expression
                    );
                }

                return new DynamicMetaObject(
                    Expression.Condition(
                        Expression.Call(
                            null,
                            typeof(RuntimeOps).GetMethod("ExpandoCheckVersion"),
                            GetLimitedSelf(),
                            Expression.Constant(originalClass ?? klass, typeof(object))
                        ),
                        ifTestSucceeds,
                        binder.GetUpdateExpression(ifTestSucceeds.Type)
                    ),
                    GetRestrictions().Merge(succeeds.Restrictions)
                );
            }

            /// <summary>
            /// Gets the class and the index associated with the given name.  Does not update the expando object.  Instead
            /// this returns both the original and desired new class.  A rule is created which includes the test for the
            /// original class, the promotion to the new class, and the set/delete based on the class post-promotion.
            /// </summary>
            private ExpandoClass GetClassEnsureIndex(string name, bool caseInsensitive, ExpandoObject obj, out ExpandoClass klass, out int index) {
                ExpandoClass originalClass = Value.Class;

                index = originalClass.GetValueIndex(name, caseInsensitive, obj) ;
                if (index == ExpandoObject.AmbiguousMatchFound) {
                    klass = originalClass;
                    return null;
                }
                if (index == ExpandoObject.NoMatch) {
                    // go ahead and find a new class now...
                    ExpandoClass newClass = originalClass.FindNewClass(name);

                    klass = newClass;
                    index = newClass.GetValueIndexCaseSensitive(name);

                    Debug.Assert(index != ExpandoObject.NoMatch);
                    return originalClass;
                } else {
                    klass = originalClass;
                    return null;
                }                
            }

            /// <summary>
            /// Returns our Expression converted to our known LimitType
            /// </summary>
            private Expression GetLimitedSelf() {
                if (TypeUtils.AreEquivalent(Expression.Type, LimitType)) {
                    return Expression;
                }
                return Expression.Convert(Expression, LimitType);
            }

            /// <summary>
            /// Returns a Restrictions object which includes our current restrictions merged
            /// with a restriction limiting our type
            /// </summary>
            private BindingRestrictions GetRestrictions() {
                Debug.Assert(Restrictions == BindingRestrictions.Empty, "We don't merge, restrictions are always empty");

                return BindingRestrictionsEx.GetTypeRestriction(this);
            }

            public new ExpandoObject Value {
                get {
                    return (ExpandoObject)base.Value;
                }
            }

            private delegate DynamicMetaObject Fallback(DynamicMetaObject errorSuggestion);

            private readonly static Expression[] NoArgs = new Expression[0];

            private static Expression[] GetConvertedArgs(params Expression[] args) {
                ReadOnlyCollectionBuilder<Expression> paramArgs = new ReadOnlyCollectionBuilder<Expression>(args.Length);

                for (int i = 0; i < args.Length; i++) {
                    paramArgs.Add(Expression.Convert(args[i], typeof(object)));
                }

                return paramArgs.ToArray();
            }

            /// <summary>
            /// Helper method for generating expressions that assign byRef call
            /// parameters back to their original variables
            /// </summary>
            private static Expression ReferenceArgAssign(Expression callArgs, Expression[] args) {
                ReadOnlyCollectionBuilder<Expression> block = null;

                for (int i = 0; i < args.Length; i++) {
                    ContractUtils.Requires(args[i] is ParameterExpression);
                    if (((ParameterExpression)args[i]).IsByRef) {
                        if (block == null)
                            block = new ReadOnlyCollectionBuilder<Expression>();

                        block.Add(
                            Expression.Assign(
                                args[i],
                                Expression.Convert(
                                    Expression.ArrayIndex(
                                        callArgs,
                                        Expression.Constant(i)
                                    ),
                                    args[i].Type
                                )
                            )
                        );
                    }
                }

                if (block != null)
                    return Expression.Block(block);
                else
                    return Expression.Empty();
            }

            /// <summary>
            /// Helper method for generating arguments for calling methods
            /// on ExpandoObject.  parameters is either a list of ParameterExpressions
            /// to be passed to the method as an object[], or NoArgs to signify that
            /// the target method takes no object[] parameter.
            /// </summary>
            private static Expression[] BuildCallArgs(DynamicMetaObjectBinder binder, Expression[] parameters, Expression arg0, Expression arg1) {
                if (!object.ReferenceEquals(parameters, NoArgs))
                    return arg1 != null ? new Expression[] { Constant(binder), arg0, arg1 } : new Expression[] { Constant(binder), arg0 };
                else
                    return arg1 != null ? new Expression[] { Constant(binder), arg1 } : new Expression[] { Constant(binder) };
            }

            private static ConstantExpression Constant(DynamicMetaObjectBinder binder) {
                Type t = binder.GetType();
                while (!t.IsVisible) {
                    t = t.BaseType;
                }
                return Expression.Constant(binder, t);
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic that returns a result
            /// </summary>
            private DynamicMetaObject CallMethodWithResult(string methodName, DynamicMetaObjectBinder binder, Expression[] args) {

                //
                // Build a new expression like:
                // {
                //   object result;
                //   TryGetMember(payload, out result) ? fallbackInvoke(result) : fallbackResult
                // }
                //
                var result = Expression.Parameter(typeof(object), null);
                ParameterExpression callArgs = methodName != "TryBinaryOperation" ? Expression.Parameter(typeof(object[]), null) : Expression.Parameter(typeof(object), null);
                var callArgsValue = GetConvertedArgs(args);

                var resultMO = new DynamicMetaObject(result, BindingRestrictions.Empty);

                // Need to add a conversion if calling TryConvert
                if (binder.ReturnType != typeof(object)) {
                    Debug.Assert(binder is ConvertBinder);

                    var convert = Expression.Convert(resultMO.Expression, binder.ReturnType);
                    // will always be a cast or unbox
                    Debug.Assert(convert.Method == null);

                    // Prepare a good exception message in case the convert will fail
                    string convertFailed = "Convert failed";

                    var checkedConvert = Expression.Condition(
                        Expression.TypeIs(resultMO.Expression, binder.ReturnType),
                        convert,
                        Expression.Throw(
                            Expression.New(typeof(InvalidCastException).GetConstructor(new Type[]{typeof(string)}),
                                Expression.Call(
                                    typeof(string).GetMethod("Format", new Type[] {typeof(string), typeof(object)}),
                                    Expression.Constant(convertFailed),
                                    Expression.Condition(
                                        Expression.Equal(resultMO.Expression, Expression.Constant(null)),
                                        Expression.Constant("null"),
                                        Expression.Call(
                                            resultMO.Expression,
                                            typeof(object).GetMethod("GetType")
                                        ),
                                        typeof(object)
                                    )
                                )
                            ),
                            binder.ReturnType
                        ),
                        binder.ReturnType
                    );

                    resultMO = new DynamicMetaObject(checkedConvert, resultMO.Restrictions);
                }

				var method = typeof(ExpandoObject).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new[] { result, callArgs },
                        methodName != "TryBinaryOperation" ? Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)) : Expression.Assign(callArgs, callArgsValue[0]),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                method,
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    result
                                )
                            ),
                            Expression.Block(
                                methodName != "TryBinaryOperation" ? ReferenceArgAssign(callArgs, args) : Expression.Empty(),
                                resultMO.Expression
                            ),
                            Expression.Block(
                                resultMO.Expression
                            ),
                            binder.ReturnType
                        )
                    ),
                    GetRestrictions().Merge(resultMO.Restrictions)
                );

                return callDynamic;
            }


            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic, but uses one of the arguments for
            /// the result.
            /// 
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject CallMethodReturnLast(string methodName, DynamicMetaObjectBinder binder, Expression[] args, Expression value) {

                //
                // Build a new expression like:
                // {
                //   object result;
                //   TrySetMember(payload, result = value) ? result;
                // }
                //

                var result = Expression.Parameter(typeof(object), null);
                var callArgs = Expression.Parameter(typeof(object[]), null);
                var callArgsValue = GetConvertedArgs(args);

				var method = typeof(ExpandoObject).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new[] { result, callArgs },
                        Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                method,
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    Expression.Assign(result, Expression.Convert(value, typeof(object)))
                                )
                            ),
                            Expression.Block(
                                ReferenceArgAssign(callArgs, args),
                                result
                            ),
                            Expression.Block(
                                result
                            ),
                            typeof(object)
                        )
                    ),
                    GetRestrictions()
                );

                return callDynamic;
            }


            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic, but uses one of the arguments for
            /// the result.
            /// 
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject CallMethodNoResult(string methodName, DynamicMetaObjectBinder binder, Expression[] args) {
                //
                // First, call fallback to do default binding
                // This produces either an error or a call to a .NET member
                //
                var callArgs = Expression.Parameter(typeof(object[]), null);
                var callArgsValue = GetConvertedArgs(args);

				var method = typeof(ExpandoObject).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                //
                // Build a new expression like:
                //   if (TryDeleteMember(payload)) { } else { }
                //
                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new[] { callArgs },
                        Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                method,
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    null
                                )
                            ),
                            Expression.Block(
                                ReferenceArgAssign(callArgs, args),
                                Expression.Empty()
                            ),
							Expression.Empty(),
                            typeof(void)
                        )
                    ),
                    GetRestrictions()
                );

                return callDynamic;
            }

            /// <summary>
            /// Checks if the derived type has overridden the specified method.  If there is no
            /// implementation for the method provided then Dynamic falls back to the base class
            /// behavior which lets the call site determine how the binder is performed.
            /// </summary>
            private bool IsOverridden(string method) {
				return true;
            }

            // It is okay to throw NotSupported from this binder. This object
            // is only used by ExpandoObject.GetMember--it is not expected to
            // (and cannot) implement binding semantics. It is just so the DO
            // can use the Name and IgnoreCase properties.
            private sealed class GetBinderAdapter : GetMemberBinder {
                internal GetBinderAdapter(InvokeMemberBinder binder)
                    : base(binder.Name, binder.IgnoreCase) {
                }

                public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion) {
                    throw new NotSupportedException();
                }
            }

        }

        #endregion

        #region ExpandoData
        
        /// <summary>
        /// Stores the class and the data associated with the class as one atomic
        /// pair.  This enables us to do a class check in a thread safe manner w/o
        /// requiring locks.
        /// </summary>
        private class ExpandoData {
            internal static ExpandoData Empty = new ExpandoData();

            /// <summary>
            /// the dynamically assigned class associated with the Expando object
            /// </summary>
            internal readonly ExpandoClass Class;

            /// <summary>
            /// data stored in the expando object, key names are stored in the class.
            /// 
            /// Expando._data must be locked when mutating the value.  Otherwise a copy of it 
            /// could be made and lose values.
            /// </summary>
            private readonly object[] _dataArray;

            /// <summary>
            /// Indexer for getting/setting the data
            /// </summary>
            internal object this[int index] {
                get {
                    return _dataArray[index];
                }
                set {
                    //when the array is updated, version increases, even the new value is the same
                    //as previous. Dictionary type has the same behavior.
                    _version++;
                    _dataArray[index] = value;
                }
            }

            internal int Version {
                get { return _version; }
            }

            internal int Length {
                get { return _dataArray.Length; }
            }

            /// <summary>
            /// Constructs an empty ExpandoData object with the empty class and no data.
            /// </summary>
            private ExpandoData() {
                Class = ExpandoClass.Empty;
                _dataArray = new object[0];
            }

            /// <summary>
            /// the version of the ExpandoObject that tracks set and delete operations
            /// </summary>
            private int _version;

            /// <summary>
            /// Constructs a new ExpandoData object with the specified class and data.
            /// </summary>
            internal ExpandoData(ExpandoClass klass, object[] data, int version) {
                Class = klass;
                _dataArray = data;
                _version = version;
            }

            /// <summary>
            /// Update the associated class and increases the storage for the data array if needed.
            /// </summary>
            /// <returns></returns>
            internal ExpandoData UpdateClass(ExpandoClass newClass) {
                if (_dataArray.Length >= newClass.Keys.Length) {
                    // we have extra space in our buffer, just initialize it to Uninitialized.
                    this[newClass.Keys.Length - 1] = ExpandoObject.Uninitialized;
                    return new ExpandoData(newClass, this._dataArray, this._version);
                } else {
                    // we've grown too much - we need a new object array
                    int oldLength = _dataArray.Length;
                    object[] arr = new object[GetAlignedSize(newClass.Keys.Length)];
                    Array.Copy(_dataArray, arr, _dataArray.Length);
                    ExpandoData newData = new ExpandoData(newClass, arr, this._version);
                    newData[oldLength] = ExpandoObject.Uninitialized;
                    return newData;
                }
            }

            private static int GetAlignedSize(int len) {
                // the alignment of the array for storage of values (must be a power of two)
                const int DataArrayAlignment = 8;

                // round up and then mask off lower bits
                return (len + (DataArrayAlignment - 1)) & (~(DataArrayAlignment - 1));
            }
        }

        #endregion            
    
        #region INotifyPropertyChanged Members

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        #endregion
    }
}

namespace PlayScript.Expando {

    //
    // Note: these helpers are kept as simple wrappers so they have a better 
    // chance of being inlined.
    //
    public static partial class RuntimeOps {

        /// <summary>
        /// Gets the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <param name="value">The out parameter containing the value of the member.</param>
        /// <returns>True if the member exists in the expando object, otherwise false.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoTryGetValue(ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase, out object value) {
            return expando.TryGetValue(indexClass, index, name, ignoreCase, out value);
        }

        /// <summary>
        /// Sets the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="value">The value of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <returns>
        /// Returns the index for the set member.
        /// </returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static object ExpandoTrySetValue(ExpandoObject expando, object indexClass, int index, object value, string name, bool ignoreCase) {
            expando.TrySetValue(indexClass, index, value, name, ignoreCase, false);
            return value;
        }

        /// <summary>
        /// Deletes the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <returns>true if the item was successfully removed; otherwise, false.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoTryDeleteValue(ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase) {
            return expando.TryDeleteValue(indexClass, index, name, ignoreCase, ExpandoObject.Uninitialized);
        }

        /// <summary>
        /// Checks the version of the expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="version">The version to check.</param>
        /// <returns>true if the version is equal; otherwise, false.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoCheckVersion(ExpandoObject expando, object version) {
            return expando.Class == version;
        }

        /// <summary>
        /// Promotes an expando object from one class to a new class.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="oldClass">The old class of the expando object.</param>
        /// <param name="newClass">The new class of the expando object.</param>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static void ExpandoPromoteClass(ExpandoObject expando, object oldClass, object newClass) {
            expando.PromoteClass(oldClass, newClass);
        }
    }
}

#else

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using PlayScript;

namespace PlayScript.Expando {

	/* 
	 * Declare this outside the main class so it doesn't have to be inflated for each
	 * instantiation of Dictionary.
	 */
	internal struct Link {
		public int HashCode;
		public int Next;
	}

	[ComVisible(false)]
	[Serializable]
	[DebuggerDisplay ("Count = {Count}")]
	[DebuggerTypeProxy (typeof (ExpandoDebugView))]
	public class ExpandoObject : IDictionary<string, object>, IDictionary, ISerializable, IDeserializationCallback, IDynamicClass, IKeyEnumerable
#if NET_4_5
		, IReadOnlyDictionary<string, object>
#endif
	{
		// The implementation of this class uses a hash table and linked lists
		// (see: http://msdn2.microsoft.com/en-us/library/ms379571(VS.80).aspx).
		//		
		// We use a kind of "mini-heap" instead of reference-based linked lists:
		// "keySlots" and "valueSlots" is the heap itself, it stores the data
		// "linkSlots" contains information about how the slots in the heap
		//             are connected into linked lists
		//             In addition, the HashCode field can be used to check if the
		//             corresponding key and value are present (HashCode has the
		//             HASH_FLAG bit set in this case), so, to iterate over all the
		//             items in the dictionary, simply iterate the linkSlots array
		//             and check for the HASH_FLAG bit in the HashCode field.
		//             For this reason, each time a hashcode is calculated, it needs
		//             to be ORed with HASH_FLAG before comparing it with the save hashcode.
		// "touchedSlots" and "emptySlot" manage the free space in the heap 
		
		const int INITIAL_SIZE = 10;
		const float DEFAULT_LOAD_FACTOR = (90f / 100);
		const int NO_SLOT = -1;
		const int HASH_FLAG = -2147483648;
		
		// The hash table contains indices into the linkSlots array
		int [] table;
		
		// All (key,value) pairs are chained into linked lists. The connection
		// information is stored in "linkSlots" along with the key's hash code
		// (for performance reasons).
		// TODO: get rid of the hash code in Link (this depends on a few
		// JIT-compiler optimizations)
		// Every link in "linkSlots" corresponds to the (key,value) pair
		// in "keySlots"/"valueSlots" with the same index.
		Link [] linkSlots;
		string [] keySlots;
		object [] valueSlots;
		
		//Leave those 2 fields here to improve heap layout.
		IEqualityComparer<string> hcp;
		SerializationInfo serialization_info;
		
		// The number of slots in "linkSlots" and "keySlots"/"valueSlots" that
		// are in use (i.e. filled with data) or have been used and marked as
		// "empty" later on.
		int touchedSlots;
		
		// The index of the first slot in the "empty slots chain".
		// "Remove()" prepends the cleared slots to the empty chain.
		// "Add()" fills the first slot in the empty slots chain with the
		// added item (or increases "touchedSlots" if the chain itself is empty).
		int emptySlot;
		
		// The number of (key,value) pairs in this dictionary.
		int count;
		
		// The number of (key,value) pairs the dictionary can hold without
		// resizing the hash table and the slots arrays.
		int threshold;
		
		// The number of changes made to this dictionary. Used by enumerators
		// to detect changes and invalidate themselves.
		int generation;

		// class definition object for use by serialization code (typically AMF3 serialization)
		public object ClassDefinition { get; set;}
		
		public int Count {
			get { return count; }
		}

		public int Generation {
			get { return generation; }
		}
		
		public dynamic this [string key] {
			get {
				key = PlayScript.Dynamic.FormatKeyForAs (key);
				if (key == null)
					throw new ArgumentNullException ("key");

				// get first item of linked list corresponding to given key
				int hashCode = hcp.GetHashCode (key) | HASH_FLAG;
				int cur = table [(hashCode & int.MaxValue) % table.Length] - 1;
				
				// walk linked list until right slot is found or end is reached 
				while (cur != NO_SLOT) {
					// The ordering is important for compatibility with MS and strange
					// Object.Equals () implementations
					if (linkSlots [cur].HashCode == hashCode && hcp.Equals (keySlots [cur], key))
						return valueSlots [cur];
					cur = linkSlots [cur].Next;
				}
				// this is not an exceptional condition although we should be returning undefined instead of null
				return null;
				//throw new KeyNotFoundException ();
			}
			
			set {
				key = PlayScript.Dynamic.FormatKeyForAs (key);
				if (key == null)
					throw new ArgumentNullException ("key");

				// get first item of linked list corresponding to given key
				int hashCode = hcp.GetHashCode (key) | HASH_FLAG;
				int index = (hashCode & int.MaxValue) % table.Length;
				int cur = table [index] - 1;
				
				// walk linked list until right slot (and its predecessor) is
				// found or end is reached
				int prev = NO_SLOT;
				if (cur != NO_SLOT) {
					do {
						// The ordering is important for compatibility with MS and strange
						// Object.Equals () implementations
						if (linkSlots [cur].HashCode == hashCode && hcp.Equals (keySlots [cur], key))
							break;
						prev = cur;
						cur = linkSlots [cur].Next;
					} while (cur != NO_SLOT);
				}
				
				// is there no slot for the given key yet? 				
				if (cur == NO_SLOT) {
					// there is no existing slot for the given key,
					// allocate one and prepend it to its corresponding linked
					// list
					
					if (++count > threshold) {
						Resize ();
						index = (hashCode & int.MaxValue) % table.Length;
					}
					
					// find an empty slot
					cur = emptySlot;
					if (cur == NO_SLOT)
						cur = touchedSlots++;
					else 
						emptySlot = linkSlots [cur].Next;
					
					// prepend the added item to its linked list,
					// update the hash table
					linkSlots [cur].Next = table [index] - 1;
					table [index] = cur + 1;
					
					// store the new item and its hash code
					linkSlots [cur].HashCode = hashCode;
					keySlots [cur] = key;
				} else {
					// we already have a slot for the given key,
					// update the existing slot		
					
					// if the slot is not at the front of its linked list,
					// we move it there
					if (prev != NO_SLOT) {
						linkSlots [prev].Next = linkSlots [cur].Next;
						linkSlots [cur].Next = table [index] - 1;
						table [index] = cur + 1;
					}
				}
				
				// store the item's data itself
				valueSlots [cur] = value;
				
				generation++;
			}
		}
		
		public ExpandoObject ()
		{
			Init (INITIAL_SIZE, null);
		}
		
		public ExpandoObject (IEqualityComparer<string> comparer)
		{
			Init (INITIAL_SIZE, comparer);
		}
		
		public ExpandoObject (IDictionary<string, object> dictionary)
			: this (dictionary, null)
		{
		}
		
		public ExpandoObject (int capacity)
		{
			Init (capacity, null);
		}
		
		public ExpandoObject (IDictionary<string, object> dictionary, IEqualityComparer<string> comparer)
		{
			if (dictionary == null)
				throw new ArgumentNullException ("dictionary");
			int capacity = dictionary.Count;
			Init (capacity, comparer);
			foreach (KeyValuePair<string, object> entry in dictionary)
				this.Add (entry.Key, entry.Value);
		}
		
		public ExpandoObject (int capacity, IEqualityComparer<string> comparer)
		{
			Init (capacity, comparer);
		}
		
		protected ExpandoObject (SerializationInfo info, StreamingContext context)
		{
			serialization_info = info;
		}
		
		private void Init (int capacity, IEqualityComparer<string> hcp)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException ("capacity");
			this.hcp = (hcp != null) ? hcp : EqualityComparer<string>.Default;
			if (capacity == 0)
				capacity = INITIAL_SIZE;
			
			/* Modify capacity so 'capacity' elements can be added without resizing */
			capacity = (int)(capacity / DEFAULT_LOAD_FACTOR) + 1;
			
			InitArrays (capacity);
			generation = 0;
		}
		
		private void InitArrays (int size) {
			table = new int [size];
			
			linkSlots = new Link [size];
			emptySlot = NO_SLOT;
			
			keySlots = new string [size];
			valueSlots = new object [size];
			touchedSlots = 0;
			
			threshold = (int)(table.Length * DEFAULT_LOAD_FACTOR);
			if (threshold == 0 && table.Length > 0)
				threshold = 1;
		}
		
		void CopyToCheck (Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index");
			// we want no exception for index==array.Length && Count == 0
			if (index > array.Length)
				throw new ArgumentException ("index larger than largest valid index of array");
			if (array.Length - index < Count)
				throw new ArgumentException ("Destination array cannot hold the requested elements!");
		}
		
		void CopyKeys (string[] array, int index)
		{
			for (int i = 0; i < touchedSlots; i++) {
				if ((linkSlots [i].HashCode & HASH_FLAG) != 0)
					array [index++] = keySlots [i];
			}
		}
		
		void CopyValues (object[] array, int index)
		{
			for (int i = 0; i < touchedSlots; i++) {
				if ((linkSlots [i].HashCode & HASH_FLAG) != 0)
					array [index++] = valueSlots [i];
			}
		}
		
		delegate TRet Transform<TRet> (string key, object value);
		
		
		static KeyValuePair<string, object> make_pair (string key, object value)
		{
			return new KeyValuePair<string, object> (key, value);
		}
		
		static string pick_key (string key, object value)
		{
			return key;
		}
		
		static object pick_value (string key, object value)
		{
			return value;
		}
		
		void CopyTo (KeyValuePair<string, object> [] array, int index)
		{
			CopyToCheck (array, index);
			for (int i = 0; i < touchedSlots; i++) {
				if ((linkSlots [i].HashCode & HASH_FLAG) != 0)
					array [index++] = new KeyValuePair<string, object> (keySlots [i], valueSlots [i]);
			}
		}
		
		void Do_ICollectionCopyTo<TRet> (Array array, int index, Transform<TRet> transform)
		{
			Type src = typeof (TRet);
			Type tgt = array.GetType ().GetElementType ();
			
			try {
				if ((src.IsPrimitive || tgt.IsPrimitive) && !tgt.IsAssignableFrom (src))
					throw new Exception (); // we don't care.  it'll get transformed to an ArgumentException below
				
#if BOOTSTRAP_BASIC
				// BOOTSTRAP: gmcs 2.4.x seems to have trouble compiling the alternative
				throw new Exception ();
#else
				object[] dest = (object[])array;
				for (int i = 0; i < touchedSlots; i++) {
					if ((linkSlots [i].HashCode & HASH_FLAG) != 0)
						dest [index++] = transform (keySlots [i], valueSlots [i]);
				}
#endif
				
			} catch (Exception e) {
				throw new ArgumentException ("Cannot copy source collection elements to destination array", "array", e);
			}
		}
		
		private void Resize ()
		{
			// From the SDK docs:
			//	 Hashtable is automatically increased
			//	 to the smallest prime number that is larger
			//	 than twice the current number of Hashtable buckets
			int newSize = PlayScript.Expando.Hashtable.ToPrime ((table.Length << 1) | 1);
			
			// allocate new hash table and link slots array
			int [] newTable = new int [newSize];
			Link [] newLinkSlots = new Link [newSize];
			
			for (int i = 0; i < table.Length; i++) {
				int cur = table [i] - 1;
				while (cur != NO_SLOT) {
					int hashCode = newLinkSlots [cur].HashCode = hcp.GetHashCode(keySlots [cur]) | HASH_FLAG;
					int index = (hashCode & int.MaxValue) % newSize;
					newLinkSlots [cur].Next = newTable [index] - 1;
					newTable [index] = cur + 1;
					cur = linkSlots [cur].Next;
				}
			}
			table = newTable;
			linkSlots = newLinkSlots;
			
			// allocate new data slots, copy data
			string [] newKeySlots = new string [newSize];
			object [] newValueSlots = new object [newSize];
			Array.Copy (keySlots, 0, newKeySlots, 0, touchedSlots);
			Array.Copy (valueSlots, 0, newValueSlots, 0, touchedSlots);
			keySlots = newKeySlots;
			valueSlots = newValueSlots;			
			
			threshold = (int)(newSize * DEFAULT_LOAD_FACTOR);
		}
		
		public void Add (string key, object value)
		{
			key = PlayScript.Dynamic.FormatKeyForAs (key);
			if (key == null)
				throw new ArgumentNullException ("key");

			// get first item of linked list corresponding to given key
			int hashCode = hcp.GetHashCode (key) | HASH_FLAG;
			int index = (hashCode & int.MaxValue) % table.Length;
			int cur = table [index] - 1;
			
			// walk linked list until end is reached (throw an exception if a
			// existing slot is found having an equivalent key)
			while (cur != NO_SLOT) {
				// The ordering is important for compatibility with MS and strange
				// Object.Equals () implementations
				if (linkSlots [cur].HashCode == hashCode && hcp.Equals (keySlots [cur], key))
					throw new ArgumentException ("An element with the same key already exists in the dictionary.");
				cur = linkSlots [cur].Next;
			}
			
			if (++count > threshold) {
				Resize ();
				index = (hashCode & int.MaxValue) % table.Length;
			}
			
			// find an empty slot
			cur = emptySlot;
			if (cur == NO_SLOT)
				cur = touchedSlots++;
			else 
				emptySlot = linkSlots [cur].Next;
			
			// store the hash code of the added item,
			// prepend the added item to its linked list,
			// update the hash table
			linkSlots [cur].HashCode = hashCode;
			linkSlots [cur].Next = table [index] - 1;
			table [index] = cur + 1;
			
			// store item's data 
			keySlots [cur] = key;
			valueSlots [cur] = value;
			
			generation++;
		}
		
		public IEqualityComparer<string> Comparer {
			get { return hcp; }
		}
		
		public void Clear ()
		{
			count = 0;
			// clear the hash table
			Array.Clear (table, 0, table.Length);
			// clear arrays
			Array.Clear (keySlots, 0, keySlots.Length);
			Array.Clear (valueSlots, 0, valueSlots.Length);
			Array.Clear (linkSlots, 0, linkSlots.Length);
			
			// empty the "empty slots chain"
			emptySlot = NO_SLOT;
			
			touchedSlots = 0;
			generation++;
		}

		public override string ToString ()
		{
			return "[object Object]";
		}

		public bool ContainsKey (string key)
		{
			key = PlayScript.Dynamic.FormatKeyForAs (key);
			if (key == null)
				return false;
			
			// get first item of linked list corresponding to given key
			int hashCode = hcp.GetHashCode (key) | HASH_FLAG;
			int cur = table [(hashCode & int.MaxValue) % table.Length] - 1;
			
			// walk linked list until right slot is found or end is reached
			while (cur != NO_SLOT) {
				// The ordering is important for compatibility with MS and strange
				// Object.Equals () implementations
				if (linkSlots [cur].HashCode == hashCode && hcp.Equals (keySlots [cur], key))
					return true;
				cur = linkSlots [cur].Next;
			}
			
			return false;
		}
		
		public bool ContainsValue (object value)
		{
			IEqualityComparer<object> cmp = EqualityComparer<object>.Default;
			
			for (int i = 0; i < table.Length; i++) {
				int cur = table [i] - 1;
				while (cur != NO_SLOT) {
					if (cmp.Equals (valueSlots [cur], value))
						return true;
					cur = linkSlots [cur].Next;
				}
			}
			return false;
		}
		
		[SecurityPermission (SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
		public virtual void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException ("info");
			
			info.AddValue ("Version", generation);
			info.AddValue ("Comparer", hcp);
			// MS.NET expects either *no* KeyValuePairs field (when count = 0)
			// or a non-null KeyValuePairs field. We don't omit the field to
			// remain compatible with older monos, but we also doesn't serialize
			// it as null to make MS.NET happy.
			KeyValuePair<string, object> [] data = new KeyValuePair<string,object> [count];
			if (count > 0)
				CopyTo (data, 0);
			info.AddValue ("HashSize", table.Length);
			info.AddValue ("KeyValuePairs", data);
		}
		
		public virtual void OnDeserialization (object sender)
		{
			if (serialization_info == null)
				return;
			
			int hashSize = 0;
			KeyValuePair<string, object> [] data = null;
			
			// We must use the enumerator because MS.NET doesn't
			// serialize "KeyValuePairs" for count = 0.
			SerializationInfoEnumerator e = serialization_info.GetEnumerator ();
			while (e.MoveNext ()) {
				switch (e.Name) {
				case "Version":
					generation = (int) e.Value;
					break;
					
				case "Comparer":
					hcp = (IEqualityComparer<string>) e.Value;
					break;
					
				case "HashSize":
					hashSize = (int) e.Value;
					break;
					
				case "KeyValuePairs":
					data = (KeyValuePair<string, object> []) e.Value;
					break;
				}
			}
			
			if (hcp == null)
				hcp = EqualityComparer<string>.Default;
			if (hashSize < INITIAL_SIZE)
				hashSize = INITIAL_SIZE;
			InitArrays (hashSize);
			count = 0;
			
			if (data != null) {
				for (int i = 0; i < data.Length; ++i)
					Add (data [i].Key, data [i].Value);
			}
			generation++;
			serialization_info = null;
		}
		
		public bool Remove (string key)
		{
			key = PlayScript.Dynamic.FormatKeyForAs (key);
			if (key == null)
				throw new ArgumentNullException ("key");

			// get first item of linked list corresponding to given key
			int hashCode = hcp.GetHashCode (key) | HASH_FLAG;
			int index = (hashCode & int.MaxValue) % table.Length;
			int cur = table [index] - 1;
			
			// if there is no linked list, return false
			if (cur == NO_SLOT)
				return false;
			
			// walk linked list until right slot (and its predecessor) is
			// found or end is reached
			int prev = NO_SLOT;
			do {
				// The ordering is important for compatibility with MS and strange
				// Object.Equals () implementations
				if (linkSlots [cur].HashCode == hashCode && hcp.Equals (keySlots [cur], key))
					break;
				prev = cur;
				cur = linkSlots [cur].Next;
			} while (cur != NO_SLOT);
			
			// if we reached the end of the chain, return false
			if (cur == NO_SLOT)
				return false;
			
			count--;
			// remove slot from linked list
			// is slot at beginning of linked list?
			if (prev == NO_SLOT)
				table [index] = linkSlots [cur].Next + 1;
			else
				linkSlots [prev].Next = linkSlots [cur].Next;
			
			// mark slot as empty and prepend it to "empty slots chain"				
			linkSlots [cur].Next = emptySlot;
			emptySlot = cur;
			
			linkSlots [cur].HashCode = 0;
			// clear empty key and value slots
			keySlots [cur] = default (string);
			valueSlots [cur] = default (object);
			
			generation++;
			return true;
		}
		
		public bool TryGetValue (string key, out object value)
		{
			key = PlayScript.Dynamic.FormatKeyForAs (key);
			if (key == null)
				throw new ArgumentNullException ("key");

			// get first item of linked list corresponding to given key
			int hashCode = hcp.GetHashCode (key) | HASH_FLAG;
			int cur = table [(hashCode & int.MaxValue) % table.Length] - 1;
			
			// walk linked list until right slot is found or end is reached
			while (cur != NO_SLOT) {
				// The ordering is important for compatibility with MS and strange
				// Object.Equals () implementations
				if (linkSlots [cur].HashCode == hashCode && hcp.Equals (keySlots [cur], key)) {
					value = valueSlots [cur];
					return true;
				}
				cur = linkSlots [cur].Next;
			}
			
			// we did not find the slot
			value = default (object);
			return false;
		}

		public bool hasOwnProperty(string key)
		{
			if (key == null) return false;
			object value;
			return TryGetValue(key, out value);
		}
		
		ICollection<string> IDictionary<string, object>.Keys {
			get { return Keys; }
		}
		
		ICollection<object> IDictionary<string, object>.Values {
			get { return Values; }
		}
		
#if NET_4_5
		IEnumerable<string> IReadOnlyDictionary<string, object>.Keys {
			get { return Keys; }
		}
		
		IEnumerable<object> IReadOnlyDictionary<string, object>.Values {
			get { return Values; }
		}
#endif
		
		public KeyCollection Keys {
			get { return new KeyCollection (this); }
		}
		
		public ValueCollection Values {
			get { return new ValueCollection (this); }
		}
		
		ICollection IDictionary.Keys {
			get { return Keys; }
		}
		
		ICollection IDictionary.Values {
			get { return Values; }
		}
		
		bool IDictionary.IsFixedSize {
			get { return false; }
		}
		
		bool IDictionary.IsReadOnly {
			get { return false; }
		}
		
		static string Tostring (object key)
		{
			key = PlayScript.Dynamic.FormatKeyForAs (key);

			// Optimize for the most common case - strings
			string keyString = key as string;
			if (keyString != null) {
				return keyString;
			}
			if (key == null)
				throw new ArgumentNullException ("key");

			return key.ToString();
		}
		
		static object Toobject (object value)
		{
			if (value == null && !typeof (object).IsValueType)
				return default (object);
			if (!(value is object))
				throw new ArgumentException ("not of type: " + typeof (object).ToString (), "value");
			return (object) value;
		}
		
		object IDictionary.this [object key] {
			get {
				return this [Tostring (key)];
			}
			set { this [Tostring (key)] = Toobject (value); }
		}
		
		void IDictionary.Add (object key, object value)
		{
			this.Add (Tostring (key), Toobject (value));
		}
		
		bool IDictionary.Contains (object key)
		{
			return ContainsKey(Tostring(key));
		}
		
		void IDictionary.Remove (object key)
		{
			Remove (Tostring(key));
		}
		
		bool ICollection.IsSynchronized {
			get { return false; }
		}
		
		object ICollection.SyncRoot {
			get { return this; }
		}
		
		bool ICollection<KeyValuePair<string, object>>.IsReadOnly {
			get { return false; }
		}
		
		void ICollection<KeyValuePair<string, object>>.Add (KeyValuePair<string, object> keyValuePair)
		{
			Add (keyValuePair.Key, keyValuePair.Value);
		}
		
		bool ICollection<KeyValuePair<string, object>>.Contains (KeyValuePair<string, object> keyValuePair)
		{
			return ContainsKeyValuePair (keyValuePair);
		}
		
		void ICollection<KeyValuePair<string, object>>.CopyTo (KeyValuePair<string, object> [] array, int index)
		{
			this.CopyTo (array, index);
		}
		
		bool ICollection<KeyValuePair<string, object>>.Remove (KeyValuePair<string, object> keyValuePair)
		{
			if (!ContainsKeyValuePair (keyValuePair))
				return false;
			
			return Remove (keyValuePair.Key);
		}
		
		bool ContainsKeyValuePair (KeyValuePair<string, object> pair)
		{
			object value;
			if (!TryGetValue (pair.Key, out value))
				return false;
			
			return EqualityComparer<object>.Default.Equals (pair.Value, value);
		}
		
		void ICollection.CopyTo (Array array, int index)
		{
			KeyValuePair<string, object> [] pairs = array as KeyValuePair<string, object> [];
			if (pairs != null) {
				this.CopyTo (pairs, index);
				return;
			}
			
			CopyToCheck (array, index);
			DictionaryEntry [] entries = array as DictionaryEntry [];
			if (entries != null) {
				for (int i = 0; i < touchedSlots; i++) {
					if ((linkSlots [i].HashCode & HASH_FLAG) != 0)
						entries [index++] = new DictionaryEntry (keySlots [i], valueSlots [i]);
				}
				return;
			}
			
			Do_ICollectionCopyTo<KeyValuePair<string, object>> (array, index, make_pair);
		}
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			// enumerate over values
			return Values.GetEnumerator();
		}

		IEnumerator IKeyEnumerable.GetKeyEnumerator ()
		{
			// enumerate over keys
			return Keys.GetEnumerator();
		}


		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator ()
		{
			return new Enumerator (this);
		}
		
		IDictionaryEnumerator IDictionary.GetEnumerator ()
		{
			return new ShimEnumerator (this);
		}
		
		public Enumerator GetKVPEnumerator ()
		{
			return new Enumerator (this);
		}
		
		[Serializable]
		private class ShimEnumerator : IDictionaryEnumerator, IEnumerator
		{
			Enumerator host_enumerator;
			public ShimEnumerator (ExpandoObject host)
			{
				host_enumerator = host.GetKVPEnumerator ();
			}
			
			public void Dispose ()
			{
				host_enumerator.Dispose ();
			}
			
			public bool MoveNext ()
			{
				return host_enumerator.MoveNext ();
			}
			
			public DictionaryEntry Entry {
				get { return ((IDictionaryEnumerator) host_enumerator).Entry; }
			}
			
			public object Key {
				get { return host_enumerator.Current.Key; }
			}
			
			public object Value {
				get { return host_enumerator.Current.Value; }
			}
			
			// This is the raison d' etre of this $%!@$%@^@ class.
			// We want: IDictionary.GetEnumerator ().Current is DictionaryEntry
			public object Current {
				get { return Entry; }
			}
			
			public void Reset ()
			{
				host_enumerator.Reset ();
			}
		}
		
		[Serializable]
		public struct Enumerator : IEnumerator<KeyValuePair<string,object>>,
		IDisposable, IDictionaryEnumerator, IEnumerator
		{
			ExpandoObject dictionary;
			int next;
			int stamp;
			
			internal KeyValuePair<string, object> current;
			
			internal Enumerator (ExpandoObject dictionary)
			: this ()
			{
				this.dictionary = dictionary;
				stamp = dictionary.generation;
			}
			
			public bool MoveNext ()
			{
				VerifyState ();
				
				if (next < 0)
					return false;
				
				while (next < dictionary.touchedSlots) {
					int cur = next++;
					if ((dictionary.linkSlots [cur].HashCode & HASH_FLAG) != 0) {
						current = new KeyValuePair <string, object> (
							dictionary.keySlots [cur],
							dictionary.valueSlots [cur]
							);
						return true;
					}
				}
				
				next = -1;
				return false;
			}
			
			// No error checking happens.  Usually, Current is immediately preceded by a MoveNext(), so it's wasteful to check again
			public KeyValuePair<string, object> Current {
				get { return current; }
			}
			
			internal string CurrentKey {
				get {
					VerifyCurrent ();
					return current.Key;
				}
			}
			
			internal object CurrentValue {
				get {
					VerifyCurrent ();
					return current.Value;
				}
			}
			
			object IEnumerator.Current {
				get {
					VerifyCurrent ();
					return current;
				}
			}
			
			void IEnumerator.Reset ()
			{
				Reset ();
			}
			
			internal void Reset ()
			{
				VerifyState ();
				next = 0;
			}
			
			DictionaryEntry IDictionaryEnumerator.Entry {
				get {
					VerifyCurrent ();
					return new DictionaryEntry (current.Key, current.Value);
				}
			}
			
			object IDictionaryEnumerator.Key {
				get { return CurrentKey; }
			}
			
			object IDictionaryEnumerator.Value {
				get { return CurrentValue; }
			}
			
			void VerifyState ()
			{
				if (dictionary == null)
					throw new ObjectDisposedException (null);
				if (dictionary.generation != stamp)
					throw new InvalidOperationException ("Enumeration modified during iteration");
			}
			
			void VerifyCurrent ()
			{
				VerifyState ();
				if (next <= 0)
					throw new InvalidOperationException ("Current is not valid");
			}
			
			public void Dispose ()
			{
				dictionary = null;
			}
		}
		
		// This collection is a read only collection
		[Serializable]
		public sealed class KeyCollection : ICollection<string>, IEnumerable<string>, ICollection, IEnumerable {
			ExpandoObject dictionary;
			
			public KeyCollection (ExpandoObject dictionary)
			{
				if (dictionary == null)
					throw new ArgumentNullException ("dictionary");
				this.dictionary = dictionary;
			}
			
			
			public void CopyTo (string [] array, int index)
			{
				dictionary.CopyToCheck (array, index);
				dictionary.CopyKeys (array, index);
			}
			
			public Enumerator GetEnumerator ()
			{
				return new Enumerator (dictionary);
			}
			
			void ICollection<string>.Add (string item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}
			
			void ICollection<string>.Clear ()
			{
				throw new NotSupportedException ("this is a read-only collection");
			}
			
			bool ICollection<string>.Contains (string item)
			{
				return dictionary.ContainsKey (item);
			}
			
			bool ICollection<string>.Remove (string item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}
			
			IEnumerator<string> IEnumerable<string>.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}
			
			void ICollection.CopyTo (Array array, int index)
			{
				var target = array as string [];
				if (target != null) {
					CopyTo (target, index);
					return;
				}
				
				dictionary.CopyToCheck (array, index);
				dictionary.Do_ICollectionCopyTo<string> (array, index, pick_key);
			}
			
			IEnumerator IEnumerable.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}
			
			public int Count {
				get { return dictionary.Count; }
			}
			
			bool ICollection<string>.IsReadOnly {
				get { return true; }
			}
			
			bool ICollection.IsSynchronized {
				get { return false; }
			}
			
			object ICollection.SyncRoot {
				get { return ((ICollection) dictionary).SyncRoot; }
			}
			
			[Serializable]
			public struct Enumerator : IEnumerator<string>, IDisposable, IEnumerator {
				ExpandoObject.Enumerator host_enumerator;
				
				internal Enumerator (ExpandoObject host)
				{
					host_enumerator = host.GetKVPEnumerator ();
				}
				
				public void Dispose ()
				{
					host_enumerator.Dispose ();
				}
				
				public bool MoveNext ()
				{
					return host_enumerator.MoveNext ();
				}
				
				public string Current {
					get { return host_enumerator.current.Key; }
				}
				
				object IEnumerator.Current {
					get { return host_enumerator.CurrentKey; }
				}
				
				void IEnumerator.Reset ()
				{
					host_enumerator.Reset ();
				}
			}
		}
		
		// This collection is a read only collection
		[Serializable]
		public sealed class ValueCollection : ICollection<object>, IEnumerable<object>, ICollection, IEnumerable {
			ExpandoObject dictionary;
			
			public ValueCollection (ExpandoObject dictionary)
			{
				if (dictionary == null)
					throw new ArgumentNullException ("dictionary");
				this.dictionary = dictionary;
			}
			
			public void CopyTo (object [] array, int index)
			{
				dictionary.CopyToCheck (array, index);
				dictionary.CopyValues (array, index);
			}
			
			public Enumerator GetEnumerator ()
			{
				return new Enumerator (dictionary);
			}
			
			void ICollection<object>.Add (object item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}
			
			void ICollection<object>.Clear ()
			{
				throw new NotSupportedException ("this is a read-only collection");
			}
			
			bool ICollection<object>.Contains (object item)
			{
				return dictionary.ContainsValue (item);
			}
			
			bool ICollection<object>.Remove (object item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}
			
			IEnumerator<object> IEnumerable<object>.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}
			
			void ICollection.CopyTo (Array array, int index)
			{
				var target = array as object [];
				if (target != null) {
					CopyTo (target, index);
					return;
				}
				
				dictionary.CopyToCheck (array, index);
				dictionary.Do_ICollectionCopyTo<object> (array, index, pick_value);
			}
			
			IEnumerator IEnumerable.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}
			
			public int Count {
				get { return dictionary.Count; }
			}
			
			bool ICollection<object>.IsReadOnly {
				get { return true; }
			}
			
			bool ICollection.IsSynchronized {
				get { return false; }
			}
			
			object ICollection.SyncRoot {
				get { return ((ICollection) dictionary).SyncRoot; }
			}
			
			[Serializable]
			public struct Enumerator : IEnumerator<object>, IDisposable, IEnumerator {
				ExpandoObject.Enumerator host_enumerator;
				
				internal Enumerator (ExpandoObject host)
				{
					host_enumerator = host.GetKVPEnumerator ();
				}
				
				public void Dispose ()
				{
					host_enumerator.Dispose ();
				}
				
				public bool MoveNext ()
				{
					return host_enumerator.MoveNext ();
				}
				
				public object Current {
					get { return host_enumerator.current.Value; }
				}
				
				object IEnumerator.Current {
					get { return host_enumerator.CurrentValue; }
				}
				
				void IEnumerator.Reset ()
				{
					host_enumerator.Reset ();
				}
			}
		}

		[DebuggerDisplay("{value}", Name = "{key}", Type = "{ValueTypeName}")]
		internal class KeyValuePairDebugView
		{
			public string key   {get { return _key; }}
			public object value 
			{
				get { return _expando[_key];}
				set { _expando[_key] = value;}
			}
			
			public KeyValuePairDebugView(ExpandoObject expando, string key)
			{
				_expando = expando;
				_key = key;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string ValueTypeName
			{
				get {
					var v = value;
					if (v != null) {
						return v.GetType().Name;
					} else {
						return "";
					}
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly ExpandoObject _expando;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly string        _key;
		}

		internal class ExpandoDebugView
		{
			private ExpandoObject expando;

			public ExpandoDebugView(ExpandoObject expando)
			{
				this.expando = expando;
			}
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePairDebugView[] Keys
			{
				get
				{
					var keys = new KeyValuePairDebugView[expando.Count];
					
					int i = 0;
					foreach(string key in expando.Keys)
					{
						keys[i] = new KeyValuePairDebugView(expando, key);
						i++;
					}
					return keys;
				}
			}
		}

		#region IDynamicClass implementation
		dynamic IDynamicClass.__GetDynamicValue(string name)
		{
			return this[name];
		}
		bool IDynamicClass.__TryGetDynamicValue(string name, out object value)
		{
			return this.TryGetValue(name, out value);
		}
		void IDynamicClass.__SetDynamicValue(string name, object value)
		{
			this[name] = value;
		}
		bool IDynamicClass.__DeleteDynamicValue(object name)
		{
			return this.Remove((string)name);
		}
		bool IDynamicClass.__HasDynamicValue(string name)
		{
			return this.ContainsKey(name);
		}
		IEnumerable IDynamicClass.__GetDynamicNames()
		{
			return this.Keys;
		}
		#endregion
	}
}

#endif
