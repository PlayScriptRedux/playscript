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
using System.Net;

namespace flash.net {

	partial class URLLoader {

		// We use a partial C# class to mix C# with PlayScript
		private static void DoWithResponse(WebRequest request, Action<HttpWebResponse> responseAction)
		{
			Action wrapperAction = () =>
			{
				request.BeginGetResponse(new AsyncCallback((iar) =>
				                                           {
					var response = (HttpWebResponse)((WebRequest)iar.AsyncState).EndGetResponse(iar);
					responseAction(response);
				}), request);
			};
			wrapperAction.BeginInvoke(new AsyncCallback((iar) =>
			                                            {
				var action = (Action)iar.AsyncState;
				action.EndInvoke(iar);
			}), wrapperAction);
		}
	}

}
