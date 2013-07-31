//
// PSGetMemberCallSite.cs
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

			return (T)GetMemberAsObject(o);
		}

		/// <summary>
		/// This is the most generic method for getting a member's value.
		/// It will attempt to resolve the member by name and the get its value by invoking the 
		/// callsite's delegate
		/// </summary>
		public T GetMember<T> (object o)
		{
			if (o == null) {
				return default(T);
			}

			object value = GetMemberAsObject(o);

			if (value is T) {
				return (T)value;
			} else {
				return PlayScript.Dynamic.ConvertValue<T>(value);
			}
		}

		public object GetMemberAsObject(object o)
		{
			// resolve as dictionary 
			var dict = o as IDictionary<string, object>;
			if (dict != null) 
			{
				// special case this for expando objects
				object value;
				if (dict.TryGetValue(mName, out value)) {
					return value;
				}

				// fall through if key not found
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
					return mPropertyGetter.Invoke(o, null); 
				}

				if (mField != null) {
					return mField.GetValue(o);
				}

				if (mMethod != null) {
					// construct method delegate
					return Delegate.CreateDelegate(mTargetType, o, mMethod);
				}

				// resolve as dynamic class
				var dc = o as IDynamicClass;
				if (dc != null) 
				{
					return dc.__GetDynamicValue(mName);
				}

				throw new System.InvalidOperationException("Unhandled member type in PSGetMemberBinder");
			}

			// resolve name

			// resolve as property
			var property = otype.GetProperty(mName);
			if (property != null)
			{
				// found property
				var getter = property.GetGetMethod();
				if (getter != null && getter.IsPublic && getter.IsStatic == isStatic) 
				{
					// setup binding to property
					mType     = otype;
					mProperty = property;
					mPropertyGetter = property.GetGetMethod();
					mField    = null;
					mMethod   = null;
					mTargetType = property.PropertyType;
					return mPropertyGetter.Invoke(o, null); 
				}
			}

			// resolve as field
			var field = otype.GetField(mName);
			if (field != null)
			{
				// found field
				if (field.IsPublic && field.IsStatic == isStatic) {
					// setup binding to field
					mType     = otype;
					mProperty = null;
					mField    = field;
					mMethod   = null;
					mTargetType = field.FieldType;
					return field.GetValue(o);
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
				mProperty = null;
				mField    = null;
				mMethod   = method;
				mTargetType = PlayScript.Dynamic.GetDelegateTypeForMethod(mMethod);

				// construct method delegate
				return Delegate.CreateDelegate(mTargetType, o, mMethod);
			}

			if (o is IDynamicClass)
			{
				// dynamic class
				mType     = otype;
				mProperty = null;
				mField    = null;
				mMethod   = null;
				return ((IDynamicClass)o).__GetDynamicValue(mName);
			}

			return null;
		}


		private string 		   mName;
		private Type 		   mType;
		private PropertyInfo   mProperty;
		private FieldInfo      mField;
		private MethodInfo     mMethod;
		private MethodInfo     mPropertyGetter;
		private Type 		   mTargetType;

	};
}
#endif
