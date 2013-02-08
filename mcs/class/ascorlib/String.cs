using System;
using System.Text;

namespace _root
{
	public static class String
	{
		public static int get_length(this string s) {
			return s.Length;
		}

		public static string charAt(this string s, double index = 0) {
			return s[ (int)index ].ToString();
		}				

		public static int charCodeAt(this string s, double index) {
			return s[(int)index];
		}

		public static string concat(this string s, params object[] args) {
			throw new NotImplementedException();
		}

		public static string fromCharCode (params object[] charCodes)
		{
			var sb = new StringBuilder ();
			foreach (int obj in charCodes) {
				sb.Append( (char) obj);
			}
			return sb.ToString();
		}

		public static int indexOf(this string s, string val, double startIndex = 0) {
			return s.IndexOf(val, (int)startIndex);
		}
						
		public static int lastIndexOf(this string s, string val, double startIndex = 0x7FFFFFFF) {
			throw new NotImplementedException();
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
			if (pattern is RegExp) {
				// pattern is a regexp
				var re = pattern as RegExp;
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
				return s.IndexOf(pattern.ToString ());
			}
		}

		public static string slice(this string s) {
			throw new NotImplementedException();
		}

		public static string slice(this string s, int startIndex) {
			return s.Substring(startIndex);
		}

		public static string slice(this string s, int startIndex, int endIndex) {
			return s.Substring(startIndex, endIndex - startIndex);
		}

		public static Array split (this string s, object delimiter, int limit = 0x7fffffff)
		{
			if (limit != 0x7fffffff) {
				throw new NotImplementedException ();
			}

			return new Array( s.Split( delimiter.ToString()[0] ) );
		}

		public static string substr(this string s, double startIndex = 0, double len = 0x7fffffff) {
			throw new NotImplementedException();
		}

		public static string substring(this string s, double startIndex = 0, double endIndex = 0x7fffffff) {
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

	}
}

