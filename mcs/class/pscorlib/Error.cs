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
using System.Diagnostics;
using System.Runtime.Serialization;

namespace _root
{
	[Serializable]
	public partial class Error : System.Exception, ISerializable
	{
		public Error(SerializationInfo info, StreamingContext context) : base(info, context) {
			_errorID = (int)info.GetValue("errorID", typeof(int));
			_name = (string)info.GetValue("name", typeof(string));
			_message = (string)info.GetValue("message", typeof(string));
			_stackTrace = (StackTrace)info.GetValue("stackTrace", typeof(System.Diagnostics.StackTrace));
		}

		override public void GetObjectData(SerializationInfo info, StreamingContext context) {
			base.GetObjectData(info, context);
			info.AddValue("errorID", _errorID, typeof(int));
			info.AddValue("name", _name, typeof(string));
			info.AddValue("message", _message, typeof(string));
			info.AddValue("stackTrace", _stackTrace, typeof(System.Diagnostics.StackTrace));
		}
	}
}
