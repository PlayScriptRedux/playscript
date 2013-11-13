//
// PSSetMember.cs
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

namespace PlayScript.DynamicRuntime
{
	public class PSSetMember
	{
		public PSSetMember(string name)
		{
			mName = name;
		}

		public void SetNamedMember<T>( object o, string name, T value)
		{
			// if name has changed then invalidate type
			if (mName != name)
			{
				mName = name;
				mNameHint = 0; // invalidate name hint when name changes
				mType = null;
			}

			SetMember<T>(o, value, typeof(T) != typeof(System.Object) );
		}

		public object SetMemberAsObject(object o, object value, bool valueTypeIsConstant)
		{
			return SetMember<object>(o, value, valueTypeIsConstant);
		}

		public T SetMember<T>(object o, T value, bool valueTypeIsConstant = false)
		{
			Stats.Increment(StatsCounter.SetMemberBinderInvoked);

			TypeLogger.LogType(o);

			// get accessor for value type T
			var accessor = o as IDynamicAccessor<T>;
			if (accessor != null) {
				accessor.SetMember(mName, ref mNameHint, value);
				return value;
			}

			// fallback on untyped accessor
			var untypedAccessor = o as IDynamicAccessorUntyped;
			if (untypedAccessor != null) {
				untypedAccessor.SetMember(mName, ref mNameHint, (object)value);
				return value;
			}

			// resolve as dictionary 
			var dict = o as IDictionary;
			if (dict != null) 
			{
				// special case this since it happens so much in object initialization
				dict[mName] = value;
				return value;
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

			// see if binding type is the same
			if (otype == mType)
			{
				// use cached resolve
				if (mProperty != null) {
					Action<T> action;
					if (o == mPreviousTarget) {
						action = (Action<T>)mPreviousAction;
					} else {
						mPreviousAction = action = ActionCreator.CreatePropertySetAction<T>(o, mProperty);
						mPreviousTarget = o;
					}
					action(value);
					return value;
				}

				// use cached resolve
				if (mProperty != null) {
					mArgs[0] = value;
					mPropertySetter.Invoke(o, BindingFlags.SuppressChangeType, null, mArgs, null);
					return value;
				}

				if (mField != null) {
					mField.SetValue(o, value);
					return value;
				}

				// resolve as dynamic class
				var dc = o as IDynamicClass;
				if (dc != null) 
				{
					dc.__SetDynamicValue(mName, value);
					return value;
				}

				throw new System.InvalidOperationException("Unhandled member type in PSSetMemberBinder");
			}

			// resolve name
			Stats.Increment(StatsCounter.SetMemberBinder_Resolve_Invoked);

			// resolve as property
			// TODO: we allow access to non-public properties for simplicity,
			// should cleanup to check access levels
			var property = otype.GetProperty(mName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				// found property
				var setter = property.GetSetMethod();
				if (setter != null && setter.IsStatic == isStatic)
				{
					// setup binding to property
					mType     = otype;
					mProperty = property;
					mPropertySetter = property.GetSetMethod();
					mField    = null;
					mPreviousAction = null;
					mPreviousTarget = null;

					mArgs[0] = PlayScript.Dynamic.ConvertValue(value, property.PropertyType);
					mPropertySetter.Invoke(o, mArgs);
					return value;
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
					mProperty = null;
					mField    = field;
					mPreviousAction = null;
					mPreviousTarget = null;

					// resolve conversion function
					object newValue = PlayScript.Dynamic.ConvertValue(value, mField.FieldType);
					mField.SetValue(o, newValue);
					return value;
				}
			}

			if (o is IDynamicClass)
			{
				// dynamic class
				mType     = otype;
				mProperty = null;
				mField    = null;
				mPreviousAction = null;
				mPreviousTarget = null;
				((IDynamicClass)o).__SetDynamicValue(mName, value);
				return value;
			}		

			// failed
			return default(T);
		}



		private string			mName;
		private uint 			mNameHint;
		private Type			mType;
		private PropertyInfo	mProperty;
		private FieldInfo		mField;
		private MethodInfo		mPropertySetter;
		private object[]		mArgs = new object[1];
		object					mPreviousTarget;
		object					mPreviousAction;
	};
}
#endif
