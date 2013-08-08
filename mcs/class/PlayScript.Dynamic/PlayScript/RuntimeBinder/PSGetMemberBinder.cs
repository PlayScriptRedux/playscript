
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
		MethodInfo      method;
		object			previousTarget;
		object			previousFunc;

		public PSGetMemberBinder (string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;
		}

		public static T GetValue<T>(CallSite site, object o, string name )
		{
			var binder = (PSGetMemberBinder)site.Binder;

			if (name != binder.name)
			{
				binder.name = name;
				binder.type = null;
			}

			return GetMember<T>(site, o);
		}

		/// <summary>
		/// This is the most generic method for getting a member's value.
		/// It will attempt to resolve the member by name and the get its value by invoking the 
		/// callsite's delegate
		/// </summary>
		private static T GetMember<T> (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.GetMemberBinderInvoked);
#endif

			if (o == null) {
				return default(T);
			}

			var binder = (PSGetMemberBinder)site.Binder;

			// resolve as dictionary 
			var dict = o as IDictionary<string, object>;
			if (dict != null) 
			{
				// special case this for expando objects
				object value;
				if (dict.TryGetValue(binder.name, out value)) {
					return PlayScript.Dynamic.ConvertValue<T>(value);
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

			if (otype == binder.type)
			{
				// use cached resolve
				if (binder.property != null) {
					Func<T> func;
					if (o == binder.previousTarget) {
						func = (Func<T>)binder.previousFunc;
					} else {
						binder.previousFunc = func = ActionCreator.CreatePropertyGetAction<T>(o, binder.property);
						binder.previousTarget = o;
					}
					return func();
				}
				
				if (binder.field != null) {
					return PlayScript.Dynamic.ConvertValue<T>(binder.field.GetValue(o));
				}

				if (binder.method != null) {
					// construct method delegate
					return PlayScript.Dynamic.ConvertValue<T>(Delegate.CreateDelegate(PlayScript.Dynamic.GetDelegateTypeForMethod(binder.method), o, binder.method));
				}
				
				// resolve as dynamic class
				var dc = o as IDynamicClass;
				if (dc != null) 
				{
					object result = dc.__GetDynamicValue(binder.name);
					return PlayScript.Dynamic.ConvertValue<T>(result);
				}
				
				throw new System.InvalidOperationException("Unhandled member type in PSGetMemberBinder");
			}

			// resolve name

#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.GetMemberBinder_Resolve_Invoked);
#endif

			// resolve as property
			var property = otype.GetProperty(binder.name);
			if (property != null)
			{
				// found property
				var getter = property.GetGetMethod();
				if (getter != null && getter.IsPublic && getter.IsStatic == isStatic) 
				{
					// setup binding to property
					binder.type     = otype;
					binder.property = property;
					binder.field    = null;
					binder.method   = null;
					return PlayScript.Dynamic.ConvertValue<T>(property.GetValue(o, null));
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
					binder.method   = null;
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
			var method = otype.GetMethod(binder.name, flags);
			if (method != null)
			{
				// setup binding to method
				binder.type     = otype;
				binder.property = null;
				binder.field    = null;
				binder.method   = method;
				// construct method delegate
				return PlayScript.Dynamic.ConvertValue<T>(Delegate.CreateDelegate(PlayScript.Dynamic.GetDelegateTypeForMethod(binder.method), o, binder.method));
			}

			if (o is IDynamicClass)
			{
				// dynamic class
				binder.type     = otype;
				binder.property = null;
				binder.field    = null;
				binder.method   = null;
				object result = ((IDynamicClass)o).__GetDynamicValue(binder.name);
				return PlayScript.Dynamic.ConvertValue<T>(result);
			}
			
			// could not resolve name as property or field, and is not dynamic class or dictionary
			// invoke callback
			if (Binder.OnGetMemberError != null)
			{
				return PlayScript.Dynamic.ConvertValue<T>(Binder.OnGetMemberError (o, binder.name, null));
			}
			else
			{
				return default(T);
			}
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

