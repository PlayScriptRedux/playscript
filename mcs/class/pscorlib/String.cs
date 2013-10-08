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
using System.Text;

namespace _root
{
	[PlayScript.Extension(typeof(string))]
	public static class String
	{
		public static int get_length(this string s) {
			return (s!=null) ? s.Length : -1;
		}

		public static string charAt(this string s, double index = 0) {
			return s[ (int)index ].ToString();
		}				

		public static int charCodeAt(this string s, double index) {
			return s[(int)index];
		}

		public static string concat(this string s, params object[] args) {
			foreach (object arg in args)
			{
				s += arg.ToString();
			}
			return s;
		}

		private static char objectToChar(object o)
		{
			if (o is int) {
				return (char)(int)o;
			} else if (o is uint) {
				return (char)(uint)o;
			} else if (o is char) {
				return (char)o;
			} else {
				throw new NotImplementedException();
			}
		}

		public static string fromCharCode (params object[] charCodes)
		{
			if (charCodes.Length == 1)
			{
				return new string(objectToChar(charCodes[0]), 1);
			}
			else
			{
				var chars = new char[charCodes.Length];
				for (int i=0; i < charCodes.Length; i++) {
					chars[i] = objectToChar(charCodes[i]);
				}
				return new string(chars);
			}
		}

		public static int indexOf(this string s, string val, double startIndex = 0) {
			if (s == null || val == null || startIndex >= s.Length) {
				return -1;
			}
			if (startIndex < 0) {
				startIndex = 0;
			}
			return s.IndexOf(val, (int)startIndex, StringComparison.Ordinal);
		}

		public static int lastIndexOf(this string s, string val, double startIndex = 0x7FFFFFFF) {
			if (startIndex < 0) {
				return -1;
			}
			if (startIndex == 0x7FFFFFFF || startIndex > s.Length) {
				startIndex = s.Length;
			}
			return s.LastIndexOf(val, (int)startIndex, StringComparison.Ordinal);
		}

		public static int localeCompare(this string s, string other, params object[] values) {
			throw new NotImplementedException();
		}

		public static Array match(this string s, object pattern) {
			if (pattern is RegExp) {
				// pattern is a regexp
				var re = pattern as RegExp;
				return re.match(s);
			} else {
				// pattern is a string or other object
				throw new NotImplementedException();
			}
		}

		public static string replace (this string s, object pattern, object repl)
		{
			if (s == null) {
				return null;
			}

			var re = pattern as RegExp;

			if (repl is Delegate) {
				if (re == null) {
					re = new RegExp (pattern.ToString ());
				}
				return re.replace (s, repl as Delegate);
			}

			if (re != null) {
				// pattern is a regexp
				return re.replace(s, repl.ToString());
			} else {
				// pattern is a string or other object
				return s.Replace(pattern.ToString (), repl.ToString());
			}
		}

		public static int search(this string s, object pattern) {
			if (pattern is RegExp) {
				// pattern is a regexp
				var re = pattern as RegExp;
				return re.search(s);
			} else {
				// pattern is a string or other object
				return s.IndexOf(pattern.ToString (), StringComparison.Ordinal);
			}
		}

		public static string slice(this string s) {
			throw new NotImplementedException();
		}

		public static string slice(this string s, int startIndex) {
			return s.Substring(startIndex);
		}

		public static string slice(this string s, int startIndex, int endIndex) {
			if (endIndex < 0) {
				endIndex = s.Length + endIndex;
			}
			return s.Substring(startIndex, endIndex - startIndex);
		}

		public static Array split (this string s, object delimiter, int limit = 0x7fffffff)
		{
			if (s == null) return new Array();

			if (limit != 0x7fffffff) {
				if (delimiter.ToString() != "") {
					var split = s.Split(new string[] {(string)delimiter}, limit, StringSplitOptions.None);
					return new Array(split);
				} else {
					throw new NotImplementedException ();
				}
			}

			if (delimiter is RegExp) {
				var re = delimiter as RegExp;
				return re.split(s);
			} else if (delimiter is string) {
				if (delimiter.ToString() != "") {
					var split = s.Split(new string[] {(string)delimiter}, StringSplitOptions.None );
					return new Array(split);
				} else {
					// split everything
					var split = new string[s.Length];
					for(int i=0; i < split.Length; i++) {
						split[i] = s[i].ToString();
					}
					return new Array(split);
				}
			} else {
				throw new NotImplementedException ();
			}
		}

		public static string substr(this string s, double startIndex = 0, double len = 0x7fffffff) {
			if (startIndex < 0) {
				startIndex = s.Length + startIndex;
				startIndex = (startIndex >= 0) ? startIndex : 0;
			}
			if (len == 0x7fffffff) {
				return s.Substring((int)startIndex);
			} else {
				len = Math.min(len, s.Length - startIndex);
				return s.Substring((int)startIndex, (int)len);
			}
		}

		public static string substring(this string s, double startIndex = 0, double endIndex = 0x7fffffff) {
			if (startIndex < 0) {
				startIndex = s.Length + startIndex;
				startIndex = (startIndex >= 0) ? startIndex : 0;
			}
			if (endIndex == 0x7fffffff) {
				return s.Substring((int)startIndex);
			} else {
				// TODO: should this throw or be silent if length exceeded?
				return s.Substring((int)startIndex, (int)endIndex - (int)startIndex);
			}
		}

		public static string toLocaleLowerCase(this string s) {
			throw new NotImplementedException();
		}

		public static string toLocaleUpperCase(this string s) {
			throw new NotImplementedException();
		}

		public static string toLowerCase(this string s) {
			return s.ToLowerInvariant();
		}

		public static string toUpperCase(this string s) {
			return s.ToUpperInvariant();
		}

		public static string valueOf(this string s) {
			return s;
		}

		public static string toString(this string s) {
			return s;
		}

	}
}

