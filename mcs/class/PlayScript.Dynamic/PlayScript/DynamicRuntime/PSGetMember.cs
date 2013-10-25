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
	};
}
#endif
