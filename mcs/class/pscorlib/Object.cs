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

using System;

namespace _root
{
	public class Object
	{
		public System.Type constructor
		{
			get {return this.GetType();}
		}

		/// <summary>
		/// This is the standard AS3 toString method. This is override-able by AS3 objects.
		/// The standard .NET ToString() will call this.
		/// </summary>
		public virtual string toString()
		{
			return string.Format("[object {0}]", this.GetType().Name);
		}

		/// <summary>
		/// This is the standard .NET ToString method which will be called in most cases by code wanting to convert this
		/// object into a string (debugger, printing, convert binders, etc). The final and default behaviour is to call
		/// the AS3 toString method which should be overridden in derived AS3 types.
		/// </summary>
		public sealed override string ToString()
		{
			return toString();
		}

		public virtual bool hasOwnProperty(object v = null)
		{
			/*var t =*/ GetType ();
			var name = v as string;
			
			if (name != null) {
				return PlayScript.Dynamic.HasOwnProperty(this, name);
			}
			
			return false;
		}

		// TODO: Add overloads for the variants of in (string, int)
		public virtual bool Contains(object v)
		{
			return hasOwnProperty(v);
		}

		public virtual dynamic valueOf()
		{
			return toString();
		}

	}
}

