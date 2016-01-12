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
	[Serializable]
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	public class DeprecatedAttribute : Attribute
	{
		private string _message;
		private bool _error;

		public DeprecatedAttribute()
		{
			_message = null;
			_error = false;
		}

		public DeprecatedAttribute (string message)
		{
			_message = message;
			_error = false;
		}

		public DeprecatedAttribute (string message, bool error)
		{
			_message = message;
			_error = error;
		}

		public string message {
			get { return _message; }
		}

		public string replacement {
			get { return _message; }
			set { _message = replacement; }
		}

		public bool IsError{
			get { return _error; }
		}
	}
}
