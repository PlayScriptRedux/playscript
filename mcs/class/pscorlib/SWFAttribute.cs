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
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	public class SWFAttribute : Attribute
	{
		public object width { get; set; }
		public object height { get; set; }
		public object frameRate { get; set; }
		public object backgroundColor { get; set; }
		public object quality {get;set;}
		
		public SWFAttribute ()
		{
		}

		private static int? TryParseInt(object o)
		{
			if (o != null)	{
				if (o is int) {
					return (int)o;
				} else if (o is string) {
					int v;
					if (int.TryParse((string)o, out v)) {
						return v;
					}
				}
			}
			return null;
		}

		public System.Drawing.Size? GetDesiredSize()
		{
			// parse attributes
			int? width = TryParseInt(this.width);
			int? height = TryParseInt(this.height);
			if (width.HasValue && height.HasValue) {
				return new System.Drawing.Size(width.Value, height.Value);
			} else {
				return null;
			}
		}

		public int? GetDesiredFrameRate()
		{
			return TryParseInt(frameRate);
		}

	}
}

