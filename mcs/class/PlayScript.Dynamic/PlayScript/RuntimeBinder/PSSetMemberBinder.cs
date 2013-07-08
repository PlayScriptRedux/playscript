
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
		System.Type 	convertType;

		public PSSetMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;
		}

		public void SetValue<T>(CallSite site, object o, string name, T value)
		{
			var target = site as CallSite< Action<CallSite,object,T> >;
			var otype = o.GetType();

			if (otype != type || this.name != name)
			{
				// set target name if it changed 
				this.name = name;
				// use the slow member set
				SetMember<T>(site, o, value);
			}
			else
			{
				target.Target(site, o, value);
			}
		}
		
		private void SetField<T>(CallSite site, object o, T value)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				field.SetValue(o, value);
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}
		
		private void ConvertAndSetField<T>(CallSite site, object o, T value)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				if (value != null && !convertType.IsAssignableFrom(value.GetType())) {
					object newValue = Convert.ChangeType(value, convertType);
					field.SetValue(o, newValue);
				} else {
					field.SetValue(o, value);
				}
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}
		
		private void SetProperty<T>(CallSite site, object o, T value)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				property.SetValue(o, value, null);
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}
		
		private void ConvertAndSetProperty<T>(CallSite site, object o, T value)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				if (value != null && !convertType.IsAssignableFrom(value.GetType())) {
					object newValue = Convert.ChangeType(value, convertType);
					property.SetValue(o, newValue, null);
				} else {
					property.SetValue(o, value, null);
				}
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}


		private void SetStaticField<T>(CallSite site, object o, T value)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				field.SetValue(o, value);
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}
		
		private void ConvertAndSetStaticField<T>(CallSite site, object o, T value)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				if (value != null && !convertType.IsAssignableFrom(value.GetType())) {
					object newValue = Convert.ChangeType(value, convertType);
					field.SetValue(o, newValue);
				} else {
					field.SetValue(o, value);
				}
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}
		
		private void SetStaticProperty<T>(CallSite site, object o, T value)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				property.SetValue(null, value, null);
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}
		
		private void ConvertAndSetStaticProperty<T>(CallSite site, object o, T value)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				if (value != null && !convertType.IsAssignableFrom(value.GetType())) {
					object newValue = Convert.ChangeType(value, convertType);
					property.SetValue(null, newValue, null);
				} else {
					property.SetValue(null, value, null);
				}
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}

		private void SetDynamicValue<T>(CallSite site, object o, T value)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				var binder = (PSSetMemberBinder)site.Binder;
				((IDynamicClass)o).__SetDynamicValue(binder.name, value);
			}
			else
			{
				SetMember<T>(site, o, value);
			}
		}
		
		private void ResolveError<T>(CallSite site, object o, T value)
		{
			// invoke callback
			if (Binder.OnSetMemberError != null)
			{
				Binder.OnSetMemberError (o, this.name, (object)value);
			}
		}

		private static void SetMember<T> (CallSite site, object o, T value)
		{
			// resolve as dictionary 
			var dict = o as IDictionary;
			if (dict != null) 
			{
				// special case this since it happens so much in object initialization
				var binder = (PSSetMemberBinder)site.Binder;
				dict[binder.name] = value;
			}
			else
			{
				// cast site
				var target = site as CallSite< Action<CallSite,object,T> >;
				// resolve member
				ResolveMember<T>(target, o);
				// invoke target delegate
				target.Target(site, o, value);
			}
		}

		private static bool DoesNeedConversion<T>(Type target)
		{
			var source = typeof(T);
			return (target != typeof(object) && !target.IsAssignableFrom(source));
		}
		
		private static void ResolveMember<T> (CallSite< Action<CallSite,object,T> > target, object o)
		{
			// update stats
			Binder.MemberResolveCount++;

			var binder = (PSSetMemberBinder)target.Binder;

			// determine if this is a instance member or a static member
			bool isStatic;
			if (o is System.Type) {
				// static member
				binder.type = (System.Type)o;
				isStatic = true;
			} else {
				// instance member
				binder.type = o.GetType();
				isStatic = false;
			}

			// resolve as property
			var property = binder.type.GetProperty(binder.name);
			if (property != null)
			{
				// found property
				var setter = property.GetSetMethod();
				if (setter != null && setter.IsPublic && setter.IsStatic == isStatic) 
				{
					// setup binding to property
					binder.property = property;
					if (DoesNeedConversion<T>(property.PropertyType)) {
						binder.convertType = property.PropertyType;
						if (isStatic) target.Target = binder.ConvertAndSetStaticProperty<T>;
						         else target.Target = binder.ConvertAndSetProperty<T>;
					} else {
						if (isStatic) target.Target = binder.SetStaticProperty<T>;
						         else target.Target = binder.SetProperty<T>;
					}
					return;
				}
			}
				
			// resolve as field
			var field = binder.type.GetField(binder.name);
			if (field != null)
			{
				// found field
				if (field.IsPublic && field.IsStatic == isStatic) {
					// setup binding to field
					binder.field = field;
					if (DoesNeedConversion<T>(field.FieldType)) {
						binder.convertType = field.FieldType;
						if (isStatic) target.Target = binder.ConvertAndSetStaticField<T>;
						         else target.Target = binder.ConvertAndSetField<T>;
					} else {
						if (isStatic) target.Target = binder.SetStaticField<T>;
						         else target.Target = binder.SetField<T>;
					}
					return;
				}
			}

			// resolve as dynamic class
			if (o is IDynamicClass) 
			{
				target.Target = binder.SetDynamicValue<T>;
				return;
			}

			// resolve error
			target.Target = binder.ResolveError<T>;
			return;
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

