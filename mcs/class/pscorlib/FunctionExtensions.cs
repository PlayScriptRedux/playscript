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
using System.Collections;

namespace _root {

	public static class FunctionExtensions {

		public static object[] BuildArgumentList(MethodInfo methodInfo, IList args)
		{
			ParameterInfo[] paramList = methodInfo.GetParameters();

			// build new argument list
			object[] newargs = new object[paramList.Length];

			// handle parameters we were given
			int i=0;
			if (args != null) {
				for (; i < args.Count; i++)
				{
					newargs[i] = args[i];
					
// TODO make this work, this was an attempt at doing conversion of argument values as needed to match AS3 rules
//
//					Type paramType = paramList[i].ParameterType;
//					object arg = args[i];
//					if ((arg != null) && (paramType != typeof(System.Object)) && (arg.GetType() != paramType)) {
//
//						if (arg != PlayScript.Undefined._undefined) {
//							// perform conversion of argument
//							newargs[i] = arg; // Convert.ChangeType(arg, paramType);
//						} else {
//							// use default values
//							newargs[i] = paramList[i].DefaultValue;
//						}
//					} else {
//						newargs[i] = arg;
//					}
				}
			}

			// add default values
			for (; i < paramList.Length; i++)
			{
				newargs[i] = paramList[i].DefaultValue;
			}
			return newargs;
		}


		public static dynamic apply(this Delegate d, dynamic thisArg, Array argArray) {
			object[] newargs = BuildArgumentList(d.Method, argArray);
			return d.DynamicInvoke(newargs);
		}

		public static dynamic call(this Delegate d, dynamic thisArg, params object[] args) {
			object[] newargs = BuildArgumentList(d.Method, args);
			return d.DynamicInvoke(newargs);
		}

		// this returns the number of arguments to the delegate method
		public static int get_length(this Delegate d) {
			return d.Method.GetParameters().Length;
		}

	}


}
