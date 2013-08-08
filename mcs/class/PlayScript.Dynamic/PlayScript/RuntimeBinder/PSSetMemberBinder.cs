
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PlayScript.Expando;
using PlayScript;

namespace PlayScript.RuntimeBinder
{
	class PSSetMemberBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();
		
		string name;
		System.Type 	type;
		FieldInfo		field;
		PropertyInfo	property;
		object			previousTarget;
		object			previousAction;

		public PSSetMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;
		}

		public static void SetValue<T>(CallSite site, object o, string name, T value)
		{
			var binder = site.Binder as PSSetMemberBinder;
			// if name has changed then invalidate type
			if (binder.name != name)
			{
				binder.name = name;
				binder.type = null;
			}

			SetMember<T>(site, o, value);
		}

		/// <summary>
		/// This is the most generic method for setting a member's value.
		/// It will attempt to resolve the member by name and the set its value by invoking the 
		/// callsite's delegate
		/// </summary>
		private static void SetMember<T> (CallSite site, object o, T value)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.SetMemberBinderInvoked);
#endif

			var binder = (PSSetMemberBinder)site.Binder;

			// resolve as dictionary 
			var dict = o as IDictionary;
			if (dict != null) 
			{
				// special case this since it happens so much in object initialization
				dict[binder.name] = value;
				return;
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
			if (otype == binder.type)
			{
				// use cached resolve
				if (binder.property != null) {
					Action<T> action;
					if (o == binder.previousTarget) {
						action = (Action<T>)binder.previousAction;
					} else {
						binder.previousAction = action = ActionCreator.CreatePropertySetAction<T>(o, binder.property);
						binder.previousTarget = o;
					}
					action(value);
					return;
				}

				if (binder.field != null) {
					object newValue = PlayScript.Dynamic.ConvertValue(value, binder.field.FieldType);
					binder.field.SetValue(o, newValue);
					return;
				}

				// resolve as dynamic class
				var dc = o as IDynamicClass;
				if (dc != null) 
				{
					dc.__SetDynamicValue(binder.name, value);
					return;
				}

				throw new System.InvalidOperationException("Unhandled member type in PSSetMemberBinder");
			}

			// resolve name

#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.SetMemberBinder_Resolve_Invoked);
#endif

			otype = o.GetType();
			isStatic = false;

			// resolve as property
			var property = otype.GetProperty(binder.name);
			if (property != null)
			{
				// found property
				var setter = property.GetSetMethod();
				if (setter != null && setter.IsPublic && setter.IsStatic == isStatic) 
				{
					// setup binding to property
					binder.type     = otype;
					binder.property = property;
					binder.field    = null;
					object newValue = PlayScript.Dynamic.ConvertValue(value, binder.property.PropertyType);
					binder.property.SetValue(o, newValue, null);
					return;
				}
			}
			
			// resolve as field
			var field = otype.GetField(binder.name);
			if (field != null)
			{
				// found field
				if (field.IsPublic && field.IsStatic == isStatic) {
					// setup binding to field
					binder.type     = otype;
					binder.property = null;
					binder.field    = field;
					object newValue = PlayScript.Dynamic.ConvertValue(value, binder.field.FieldType);
					binder.field.SetValue(o, newValue);
					return;
				}
			}

			if (o is IDynamicClass)
			{
				// dynamic class
				binder.type     = otype;
				binder.property = null;
				binder.field    = null;
				((IDynamicClass)o).__SetDynamicValue(binder.name, value);
				return;
			}

			// could not resolve name as property or field, and is not dynamic class or dictionary
			// invoke callback
			if (Binder.OnSetMemberError != null)
			{
				Binder.OnSetMemberError (o, binder.name, value);
			}
		}

		static PSSetMemberBinder ()
		{
			delegates.Add (typeof(Action<CallSite, object, int>), (Action<CallSite, object, int>)SetMember<int>);
			delegates.Add (typeof(Action<CallSite, object, uint>), (Action<CallSite, object, uint>)SetMember<uint>);
			delegates.Add (typeof(Action<CallSite, object, double>), (Action<CallSite, object, double>)SetMember<double>);
			delegates.Add (typeof(Action<CallSite, object, bool>), (Action<CallSite, object, bool>)SetMember<bool>);
			delegates.Add (typeof(Action<CallSite, object, string>), (Action<CallSite, object, string>)SetMember<string>);
			delegates.Add (typeof(Action<CallSite, object, object>), (Action<CallSite, object, object>)SetMember<object>);
		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind set member for target " + delegateType.FullName);
		}
		
	}
}
#endif

