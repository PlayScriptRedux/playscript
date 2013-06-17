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
using System.Reflection;

namespace _root {

	public static class FunctionExtensions {

		public static dynamic apply(this Delegate d, dynamic thisArg, Array argArray) {
			object[] args = (argArray != null) ? argArray.ToSystemObjectArray() : null;
			int      argLength = (argArray != null) ? (int)argArray.length : 0;

			var paramList = d.Method.GetParameters();
			if (paramList.Length > argLength) {

				// rebuild argument list with default values
				object[] newargs = new object[paramList.Length];
				for (int i=0; i < newargs.Length; i++) 
				{
					if (i < argLength) {
						newargs[i] = args[i];
					} else {
						newargs[i] = paramList[i].DefaultValue;
					}
				}

				return d.DynamicInvoke(newargs);
				
			}
			else {
				return d.DynamicInvoke(args);
			}
		}

		public static dynamic call(this Delegate d, dynamic thisArg, params object[] args) {
			return d.DynamicInvoke(args);
		}

		// this returns the number of arguments to the delegate method
		public static int get_length(this Delegate d) {
			return d.Method.GetParameters().Length;
		}

	}


}
