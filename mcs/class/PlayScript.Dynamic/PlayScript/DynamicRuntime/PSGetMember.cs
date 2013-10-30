//
// PSGetMember.cs
//
// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

#if !DYNAMIC_SUPPORT
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace PlayScript.DynamicRuntime
{

	#if !NEW_PSGETMEMBER
	
	/// <summary>
	/// Implements a 32-bit CRC hash algorithm compatible with Zip etc.
	/// </summary>
	/// <remarks>
	/// Crc32 should only be used for backward compatibility with older file formats
	/// and algorithms. It is not secure enough for new applications.
	/// If you need to call multiple times for the same data either use the HashAlgorithm
	/// interface or remember that the result of one Compute call needs to be ~ (XOR) before
	/// being passed in as the seed for the next Compute call.
	/// </remarks>
	internal static class Crc32
	{
		public const uint DefaultPolynomial = 0xedb88320u;
		public const uint DefaultSeed = 0xffffffffu;

		public static uint[] Table;

		public static void InitializeTable()
		{
			uint polynomial = DefaultPolynomial;
			var createTable = new uint[256];
			for (var i = 0; i < 256; i++)
			{
				var entry = (uint)i;
				for (var j = 0; j < 8; j++)
					if ((entry & 1) == 1)
						entry = (entry >> 1) ^ polynomial;
				else
					entry = entry >> 1;
				createTable[i] = entry;
			}

			Table = createTable;
		}

		public static uint Calculate(string buffer)
		{
			if (buffer == null)
				return 0;
			if (Table == null)
				InitializeTable ();
			uint crc = DefaultSeed;
			int size = buffer.Length;
			for (var i = 0; i < size; i++)
				crc = (crc >> 8) ^ Table[(byte)buffer[i] ^ crc & 0xff];
			return ~crc;
		}

	}

	public class PSGetMember
	{
		public PSGetMember(string name)
		{
			mName = name;
		}

		public T GetNamedMember<T>(object o, string name )
		{
			if (name != mName)
			{
				mName = name;
				mType = null;
			}

			return GetMember<T>(o);
		}

		public object GetMemberAsObject(object o)
		{
			return GetMember<object>(o);
		}

		/// <summary>
		/// This is the most generic method for getting a member's value.
		/// It will attempt to resolve the member by name and the get its value by invoking the 
		/// callsite's delegate
		/// </summary>
		public T GetMember<T> (object o)
		{
			Stats.Increment(StatsCounter.GetMemberBinderInvoked);

			TypeLogger.LogType(o);

			// Handles various versions of IGetMemberProvider for different types
			var getMemberProv = o as IGetMemberProvider<T>;
			if (getMemberProv != null) {
				if (mCrc == 0)
					mCrc = Crc32.Calculate (mName) & 0x1FFFFFFF; // clear top 3 bits
				return getMemberProv.GetMember (mCrc);
			}

			// Handles object version of IGetMemberProvider
			var getMemberObjProv = o as IGetMemberProvider<object>;
			if (getMemberObjProv != null) {
				if (mCrc == 0)
					mCrc = Crc32.Calculate (mName) & 0x1FFFFFFF; // clear top 3 bits
				object value = getMemberProv.GetMember (mCrc);
				return value != null ? (T)value : default(T);
			}

			// resolve as dictionary (this is usually an expando)
			var dict = o as IDictionary<string, object>;
			if (dict != null) 
			{
				Stats.Increment(StatsCounter.GetMemberBinder_Expando);

				// special case this for expando objects
				object value;
				if (dict.TryGetValue(mName, out value)) {
					// fast path empty cast just in case
					if (value is T) {
						return (T)value;
					} else {
						return PlayScript.Dynamic.ConvertValue<T>(value);
					}
				}

				// key not found
				return default(T);
			}

			if (o == null) {
				return default(T);
			}

			// determine if this is a instance member or a static member
			bool isStatic;
			Type otype;
			if (o is System.Type) {
				// static member
				otype = (System.Type)o;
				o = null;
				isStatic = true;
			} else {
				// instance member
				otype = o.GetType();
				isStatic = false;
			}

			if (otype == mType)
			{
				// use cached resolve
				if (mProperty != null) {
					Func<T> func;
					if (o == mPreviousTarget) {
						func = (Func<T>)mPreviousFunc;
					} else {
						mPreviousFunc = func = ActionCreator.CreatePropertyGetAction<T>(o, mProperty);
						mPreviousTarget = o;
					}
					return func();
				}

				if (mField != null) {
					return PlayScript.Dynamic.ConvertValue<T>(mField.GetValue(o));
				}

				if (mMethod != null) {
					// construct method delegate
					return PlayScript.Dynamic.ConvertValue<T>(Delegate.CreateDelegate(mTargetType, o, mMethod));
				}

				// resolve as dynamic class
				var dc = o as IDynamicClass;
				if (dc != null) 
				{
					object result = dc.__GetDynamicValue(mName);
					return PlayScript.Dynamic.ConvertValue<T>(result);
				}

				if (mName == "constructor") {
					return PlayScript.Dynamic.ConvertValue<T> (otype);
				}

				throw new System.InvalidOperationException("Unhandled member type in PSGetMemberBinder");
			}

			// resolve name
			Stats.Increment(StatsCounter.GetMemberBinder_Resolve_Invoked);

			// The constructor is a special synthetic property - we have to handle this for AS compatibility
			if (mName == "constructor") {
				// setup binding to field
				mType = otype;
				mPreviousFunc = null;
				mPreviousTarget = null;
				mProperty = null;
				mField = null;
				mMethod = null;
				mTargetType = typeof(Type);
				return PlayScript.Dynamic.ConvertValue<T> (otype);
			}

			// resolve as property
			// TODO: we allow access to non-public properties for simplicity,
			// should cleanup to check access levels
			var property = otype.GetProperty(mName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				// found property
				var getter = property.GetGetMethod();
				if (getter != null && getter.IsStatic == isStatic)
				{
					// setup binding to property
					mType     = otype;
					mPreviousFunc = null;
					mPreviousTarget = null;
					mProperty = property;
					mPropertyGetter = property.GetGetMethod();
					mField    = null;
					mMethod   = null;
					mTargetType = property.PropertyType;
					return PlayScript.Dynamic.ConvertValue<T>(mPropertyGetter.Invoke(o, null));
				}
			}

			// resolve as field
			// TODO: we allow access to non-public fields for simplicity,
			// should cleanup to check access levels
			var field = otype.GetField(mName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				// found field
				if (field.IsStatic == isStatic) {
					// setup binding to field
					mType     = otype;
					mPreviousFunc = null;
					mPreviousTarget = null;
					mProperty = null;
					mField    = field;
					mMethod   = null;
					mTargetType = field.FieldType;
					return PlayScript.Dynamic.ConvertValue<T>(field.GetValue(o));
				}
			}

			// resolve as method
			BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public;
			if (isStatic) {
				flags |= BindingFlags.Static;
			} else {
				flags |= BindingFlags.Instance;
			}
			var method = otype.GetMethod(mName, flags);
			if (method != null)
			{
				// setup binding to method
				mType     = otype;
				mPreviousFunc = null;
				mPreviousTarget = null;
				mProperty = null;
				mField    = null;
				mMethod   = method;
				mTargetType = PlayScript.Dynamic.GetDelegateTypeForMethod(mMethod);
				if (mTargetType == null) {
				}

				// construct method delegate
				return PlayScript.Dynamic.ConvertValue<T>(Delegate.CreateDelegate(mTargetType, o, mMethod));
			}

			if (o is IDynamicClass)
			{
				// dynamic class
				mType     = otype;
				mPreviousFunc = null;
				mPreviousTarget = null;
				mProperty = null;
				mField    = null;
				mMethod   = null;
				object result = ((IDynamicClass)o).__GetDynamicValue(mName);
				return PlayScript.Dynamic.ConvertValue<T>(result);
			}

			return default(T);
		}


		private string			mName;
		private Type			mType;
		private PropertyInfo	mProperty;
		private FieldInfo		mField;
		private MethodInfo		mMethod;
		private MethodInfo		mPropertyGetter;
		private Type			mTargetType;
		private object			mPreviousTarget;
		private object			mPreviousFunc;
		private uint			mCrc;
	};

#else

	// Optimized PSGetMember - uses delegate to call directly to target method

	public class PSGetMember
	{
		private string			mName;
		private uint			mCrc;
		private Type			mType;
		private object			mFunc;

		public PSGetMember(string name)
		{
			mName = name;
		}

		public T GetNamedMember<T>(object o, string name )
		{
			if (name != mName)
			{
				mName = name;
			}

			return GetMember<T>(o);
		}

		public object GetMemberAsObject(object o)
		{
			// Handles various versions of IGetMemberProvider for different types
			var getMemberProv = o as IGetMemberProvider<object>;
			if (getMemberProv != null) {
				return getMemberProv.GetMember (mCrc);
			}

			return GetMember<object>(o);
		}

		/// <summary>
		/// This is the most generic method for getting a member's value.
		/// It will attempt to resolve the member by name and the get its value by invoking the 
		/// callsite's delegate
		/// </summary>
		public T GetMember<T> (object o)
		{
			Stats.Increment(StatsCounter.GetMemberBinderInvoked);

			TypeLogger.LogType(o);

			if (o == null)
				return default(T);

			// determine if this is a instance member or a static member
			bool isStatic;
			Type otype;
			if (o is System.Type) {
				// static member
				otype = (System.Type)o;
				o = null;
				isStatic = true;
			} else {
				// instance member
				otype = o.GetType();
				isStatic = false;
			}

			// Use previous method
			if (otype == mType) {
				return ((Func<object, T>)this.mFunc)(o);
			}

			mType = otype;

			Func<object, T> func;

			// Handles various versions of IGetMemberProvider for different types
			var getMemberProv = o as IGetMemberProvider<T>;
			if (getMemberProv != null) {
				if (mCrc == 0)
					mCrc = Crc32.Calculate (mName) & 0x1FFFFFFF; // clear top 3 bits
				func = delegate(object obj) {
					return ((IGetMemberProvider<T>)obj).GetMember (mName);
				};
				mFunc = func;
				return func (o);
			}

			// Handles object version of IGetMemberProvider
			var getMemberObjProv = o as IGetMemberProvider<object>;
			if (getMemberObjProv != null) {
				if (mCrc == 0)
					mCrc = Crc32.Calculate (mName) & 0x1FFFFFFF; // clear top 3 bits
				func = delegate(object obj) {
					object value = ((IGetMemberProvider<object>)obj).GetMember(mCrc);
					return value != null ? (T)value : default(T);
				};
				mFunc = func;
				return func (o);
			}

			// resolve as dictionary (this is usually an expando)
			var dict = o as IDictionary<string, object>;
			if (dict != null) 
			{
				Stats.Increment(StatsCounter.GetMemberBinder_Expando);
				func = delegate(object obj) {
					object value;
					if (dict.TryGetValue (mName, out value)) {
						// fast path empty cast just in case
						if (value is T) {
							return (T)value;
						} else {
							return PlayScript.Dynamic.ConvertValue<T> (value);
						}
					}
					return default(T);
				};
				mFunc = func;
				return func (o);
			}

			// resolve name
			Stats.Increment(StatsCounter.GetMemberBinder_Resolve_Invoked);

			// The constructor is a special synthetic property - we have to handle this for AS compatibility
			if (mName == "constructor") {
				func =  delegate(object obj) {
					return PlayScript.Dynamic.ConvertValue<T> (obj as Type ?? (obj != null ? obj.GetType () : null));
				};
				mFunc = func;
				return func(o);
			}

			// resolve as property
			// TODO: we allow access to non-public properties for simplicity,
			// should cleanup to check access levels
			var property = otype.GetProperty(mName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				// found property
				var getter = property.GetGetMethod();
				if (getter != null && getter.IsStatic == isStatic)
				{
					func = delegate(object obj) {
						return PlayScript.Dynamic.ConvertValue<T>(getter.Invoke(obj, null));
					};
					mFunc = func;
					return func (o);
				}
			}

			// resolve as field
			// TODO: we allow access to non-public fields for simplicity,
			// should cleanup to check access levels
			var field = otype.GetField(mName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				// found field
				if (field.IsStatic == isStatic) {
					func = delegate(object obj) {
						return PlayScript.Dynamic.ConvertValue<T>(field.GetValue(obj));
					};
					mFunc = func;
					return func (o);
				}
			}

			// resolve as method
			BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public;
			if (isStatic) {
				flags |= BindingFlags.Static;
			} else {
				flags |= BindingFlags.Instance;
			}
			var method = otype.GetMethod(mName, flags);
			if (method != null)
			{
				Type targetType = PlayScript.Dynamic.GetDelegateTypeForMethod(method);
				func = delegate(object obj) {
					return PlayScript.Dynamic.ConvertValue<T> (Delegate.CreateDelegate (targetType, obj, method));
				};
				mFunc = func;
				return func (o);
			}

			if (o is IDynamicClass)
			{
				func = delegate(object obj) {
					return PlayScript.Dynamic.ConvertValue<T>(((IDynamicClass)obj).__GetDynamicValue(mName));
				};
				mFunc = func;
				return func (o);
			}

			return default(T);
		}

	};

#endif

}
#endif

namespace PlayScript.DynamicRuntime
{
	public interface IGetMemberProvider<T>
	{
		T GetMember(uint crc);
		T GetMember(string key);
	}
}

