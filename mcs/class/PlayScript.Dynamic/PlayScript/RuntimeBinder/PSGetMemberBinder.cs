
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
	class PSGetMemberBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

		string 			name;
		System.Type 	type;
		FieldInfo		field;
		PropertyInfo	property;
		MethodInfo      propertyGetter;
		MethodInfo      method;

		
		public PSGetMemberBinder (string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;
		}

		public T GetValue<T>(CallSite site, object o, string name )
		{
			var target = site as CallSite<Func<CallSite,object,T>>;
			var otype = o.GetType();
			
			if (otype != type || this.name != name)
			{
				// set target name if it changed 
				this.name = name;
				// use the slow member get
				return GetMember<T>(site, o);
			}
			else
			{
				return target.Target(site, o);
			}
		}

		private T GetField<T>(CallSite site, object o)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				return (T)field.GetValue(o);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}

		private T GetFieldAndConvert<T>(CallSite site, object o)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				object value = field.GetValue(o);
				return PlayScript.Dynamic.ConvertValue<T>(value);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}

		private T GetProperty<T>(CallSite site, object o)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				return (T)propertyGetter.Invoke(o, null);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}

		private T GetPropertyAndConvert<T>(CallSite site, object o)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				object value = propertyGetter.Invoke(o, null);
				return PlayScript.Dynamic.ConvertValue<T>(value);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}

		private T GetStaticField<T>(CallSite site, object o)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				return (T)field.GetValue(null);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}
		
		private T GetStaticFieldAndConvert<T>(CallSite site, object o)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				object value = field.GetValue(null);
				return PlayScript.Dynamic.ConvertValue<T>(value);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}
		
		private T GetStaticProperty<T>(CallSite site, object o)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				return (T)propertyGetter.Invoke(null, null);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}
		
		private T GetStaticPropertyAndConvert<T>(CallSite site, object o)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				object value = propertyGetter.Invoke(null, null);
				return PlayScript.Dynamic.ConvertValue<T>(value);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}

		private T GetMethod<T>(CallSite site, object o)
		{
			var otype = o.GetType();
			if (type == otype)
			{
				// construct method delegate
				object value = Delegate.CreateDelegate(PlayScript.Dynamic.GetDelegateTypeForMethod(method), !method.IsStatic ? o : null, method);
				return (T)value;
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}

		private T GetStaticMethod<T>(CallSite site, object o)
		{
			var otype = (Type)o;
			if (type == otype)
			{
				// construct method delegate
				object value = Delegate.CreateDelegate(PlayScript.Dynamic.GetDelegateTypeForMethod(method), null, method);
				return (T)value;
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}


		private T GetConstructor<T>(CallSite site, object o)
		{
			return (T)(object)o.GetType();
		}

		private T GetDynamicValue<T>(CallSite site, object o)
		{
			if (o is IDynamicClass) {
				var binder = (PSGetMemberBinder)site.Binder;
				return (T)(object)((IDynamicClass)o).__GetDynamicValue(binder.name);
			}
			else
			{
				return GetMember<T>(site, o);
			}
		}

		private T ResolveError<T>(CallSite site, object o)
		{
			// invoke callback
			if (Binder.OnGetMemberError != null)
			{
				var value = Binder.OnGetMemberError(o, this.name, typeof(T));
				return (value is T) ?(T)value : default(T);
			} 
			else 
			{
				return default(T);
			}
		}

		/// <summary>
		/// This is the most generic method for getting a member's value.
		/// It will attempt to resolve the member by name and the get its value by invoking the 
		/// callsite's delegate
		/// </summary>
		private static T GetMember<T> (CallSite site, object o)
		{
			// resolve as dictionary 
			var dict = o as IDictionary<string, object>;
			if (dict != null) 
			{
				// special case this for expando objects
				var binder = (PSGetMemberBinder)site.Binder;
				object value;
				if (dict.TryGetValue(binder.name, out value)) {
					return PlayScript.Dynamic.ConvertValue<T>(value);
				}

				// fall through if key not found
			}

			// cast site
			var target = site as CallSite<Func<CallSite,object,T>>;
			// resolve member
			ResolveMember<T>(target, o);
			// invoke target delegate
			return target.Target(site, o);
		}

		private static bool DoesNeedConversion<T>(Type source)
		{
			var target = typeof(T);
			return (target != typeof(object) && !target.IsAssignableFrom(source));
		}

		/// <summary>
		/// Resolves a member (property, field, method, etc) of a type and selects the appropriate specialized target delegate
		/// for a callsite. If the type of the object changes, this method needs to be called again.
		/// </summary>
		private static void ResolveMember<T> (CallSite<Func<CallSite,object,T>> target, object o)
		{
			// update stats
			Binder.MemberResolveCount++;

			var binder = (PSGetMemberBinder)target.Binder;

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
				var getter = property.GetGetMethod();
				if (getter != null && getter.IsPublic && getter.IsStatic == isStatic) 
				{
					// setup binding to property
					binder.property = property;
					binder.propertyGetter = getter;
					if (DoesNeedConversion<T>(property.PropertyType)) {
						if (isStatic) target.Target = binder.GetStaticPropertyAndConvert<T>;
						      else    target.Target = binder.GetPropertyAndConvert<T>;
					} else {
						if (isStatic) target.Target = binder.GetStaticProperty<T>;
						      else    target.Target = binder.GetProperty<T>;
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
						if (isStatic) target.Target = binder.GetStaticFieldAndConvert<T>;
						      else    target.Target = binder.GetFieldAndConvert<T>;
					} else {
						if (isStatic) target.Target = binder.GetStaticField<T>;
						      else    target.Target = binder.GetField<T>;
					}
					return;
				}
			}

			// resolve as method
			var method = binder.type.GetMethod(binder.name);
			if (method != null)
			{
				if (method.IsPublic) {
					binder.method = method;
					if (isStatic)        target.Target = binder.GetStaticMethod<T>;
						  else           target.Target = binder.GetMethod<T>;
					return;
				}
			}

			// special case "constructor"
			if (binder.name == "constructor" && typeof(T) is object) {
				target.Target = binder.GetConstructor<T>;
				return;
			}
			
			// resolve as dynamic class
			if (o is IDynamicClass) 
			{
				target.Target = binder.GetDynamicValue<T>;
				return;
			}

			// resolve error
			target.Target = binder.ResolveError<T>;
			return;
		}

		static PSGetMemberBinder ()
		{
			delegates.Add (typeof(Func<CallSite, object, int>), (Func<CallSite, object, int>)GetMember<int>);
			delegates.Add (typeof(Func<CallSite, object, uint>), (Func<CallSite, object, uint>)GetMember<uint>);
			delegates.Add (typeof(Func<CallSite, object, double>), (Func<CallSite, object, double>)GetMember<double>);
			delegates.Add (typeof(Func<CallSite, object, bool>), (Func<CallSite, object, bool>)GetMember<bool>);
			delegates.Add (typeof(Func<CallSite, object, string>), (Func<CallSite, object, string>)GetMember<string>);
			delegates.Add (typeof(Func<CallSite, object, object>), (Func<CallSite, object, object>)GetMember<object>);
		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind get member for target " + delegateType.FullName);
		}

	}
}
#endif

