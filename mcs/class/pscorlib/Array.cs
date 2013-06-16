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
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using PlayScript;

namespace _root
{
	// this class is used to display a custom view of the vector values to the debugger
	// TODO: we need to make these elements editable 
	internal class ArrayDebugView
	{
		private Array  mArray;
		
		// The constructor for the type proxy class must have a 
		// constructor that takes the target type as a parameter.
		public ArrayDebugView(Array array)
		{
			this.mArray = array;
		}
		
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public object[] Values
		{
			get
			{
				return mArray.ToArray();
			}
		}
	}

    // for now we implement array as a vector of dynamics
    // there may be some subtle differences between array and vector that we need to handle here
	[DynamicClass]
	[DebuggerDisplay("length = {length}")]
	[DebuggerTypeProxy(typeof(ArrayDebugView))]
    public sealed class Array : Vector<dynamic>, IDynamicClass
    {
        //
        // Constants
        //
        
        public const uint CASEINSENSITIVE = 1;
        public const uint DESCENDING = 2;
        public const uint NUMERIC = 16;
        public const uint RETURNINDEXEDARRAY = 8;
        public const uint UNIQUESORT = 4;

        public Array() {
        }
        
        public Array(object arg1, params object[] args) {
            if (arg1 is int || arg1 is double) {
                expand((int)arg1);
            } else {
                push(arg1);
                for ( var i=0; i < args.Length; i++) {
                    push (args[i]);
                }
            }
        }
        
        public Array (IEnumerable list)
        {
            foreach (var i in list) {
                push(i);
            }
        }

        // Sorts the elements in an array according to one or more fields in the array.
        public Array sortOn(object fieldName, object options = null) {
            throw new NotImplementedException();
        }

        public new Array slice(int startIndex = 0, int endIndex = 16777215) {
			if (endIndex > (int)length) endIndex = (int)length;
			
			var result = new Array();
			for (int i=startIndex; i < endIndex; i++) {
				result.Add(this[i]);
			}
			return result;
		}

		public Array filter(Delegate callback, dynamic thisObject = null) {
			throw new System.NotImplementedException();
		}


        public new Array concat(params object[] args) 
        {
            Array v = new Array();
            // add this vector
            v.append (this);
            
            // concat all supplied vectors
            foreach (var o in args) {
                if (o is IEnumerable) {
                    v.append (o as IEnumerable);
                } else {
                    throw new System.NotImplementedException();
                }
            }
            return v;
        }


		public object[] ToSystemObjectArray()
		{
			var a = new object[length];
			for (int i=0; i < length; i++) {
				a[i] = this[i];
			}
			return a;
		}

		#region IDynamicClass Implementation

		dynamic IDynamicClass.__GetDynamicValue(string name) 
		{
			throw new NotImplementedException ();
		}

		void IDynamicClass.__SetDynamicValue(string name, object value)
		{
			throw new NotImplementedException ();
		}

		bool IDynamicClass.__HasDynamicValue(string name)
		{
			throw new NotImplementedException ();
		}

		Array IDynamicClass.__GetDynamicNames()
		{
			throw new NotImplementedException ();
		}

		#endregion

    }
}

