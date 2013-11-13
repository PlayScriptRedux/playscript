/*
 * Copyright (c) 2013 Calvin Rien
 *
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 *
 * Simplified it so that it doesn't throw exceptions
 * and can be used in Unity iPhone with maximum code stripping.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using PlayScript.Expando;
using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MiniJson {

	public class Json
	{
		private const int TOKEN_NONE = 0;
		private const int TOKEN_CURLY_OPEN = 1;
		private const int TOKEN_CURLY_CLOSE = 2;
		private const int TOKEN_SQUARED_OPEN = 3;
		private const int TOKEN_SQUARED_CLOSE = 4;
		private const int TOKEN_COLON = 5;
		private const int TOKEN_COMMA = 6;
		private const int TOKEN_STRING = 7;
		private const int TOKEN_NUMBER = 8;
		private const int TOKEN_TRUE = 9;
		private const int TOKEN_FALSE = 10;
		private const int TOKEN_NULL = 11;
		private const int BUILDER_CAPACITY = 2000;

		/// <summary>
		/// On decoding, this value holds the position at which the parse failed (-1 = no error).
		/// </summary>
		protected static int lastErrorIndex = -1;
		protected static string lastDecode = "";


		/// <summary>
		/// Parses the string json into a value
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <returns>An Array, an ExpandoObject, a double, a string, null, true, or false</returns>
		public static object Parse( string json, bool ordered = false )
		{
			// save the string for debug information
			lastDecode = json;

			if( json != null )
			{
				char[] charArray = json.ToCharArray();
				int index = 0;
				bool success = true;
				object value = parseValue( charArray, ref index, ref success, ordered );

				if( success )
					lastErrorIndex = -1;
				else
					lastErrorIndex = index;

				return value;
			}
			else
			{
				return null;
			}
		}


		/// <summary>
		/// Converts a ExpandoObject / Array / primitive into a JSON string
		/// </summary>
		/// <param name="json">A ExpandoObject / Array / primitive</param>
		/// <param name="pretty">If set to true, encode with newlines and tabs to make more human readable</param>
		/// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
		public static string Stringify( object json, bool pretty = false )
		{
			var depth = pretty ? 0 : -1; 
			var builder = new StringBuilder( BUILDER_CAPACITY );
			var success = serializeValue( json, builder, depth );

			return ( success ? builder.ToString() : null );
		}


		/// <summary>
		/// On decoding, this function returns the position at which the parse failed (-1 = no error).
		/// </summary>
		/// <returns></returns>
		public static bool lastDecodeSuccessful()
		{
			return ( lastErrorIndex == -1 );
		}


		/// <summary>
		/// On decoding, this function returns the position at which the parse failed (-1 = no error).
		/// </summary>
		/// <returns></returns>
		public static int getLastErrorIndex()
		{
			return lastErrorIndex;
		}


		/// <summary>
		/// If a decoding error occurred, this function returns a piece of the JSON string 
		/// at which the error took place. To ease debugging.
		/// </summary>
		/// <returns></returns>
		public static string getLastErrorSnippet()
		{
			if( lastErrorIndex == -1 )
			{
				return "";
			}
			else
			{
				int startIndex = lastErrorIndex - 5;
				int endIndex = lastErrorIndex + 15;
				if( startIndex < 0 )
					startIndex = 0;

				if( endIndex >= lastDecode.Length )
					endIndex = lastDecode.Length - 1;

				return lastDecode.Substring( startIndex, endIndex - startIndex + 1 );
			}
		}


		#region Parsing

		protected static object parseObject( char[] json, ref int index, bool ordered )
		{
			ExpandoObject o = new ExpandoObject();
			int token;

			// {
			nextToken( json, ref index );

			bool done = false;
			while( !done )
			{
				token = lookAhead( json, index );
				if( token == TOKEN_NONE )
				{
					return null;
				}
				else if( token == TOKEN_COMMA )
				{
					nextToken( json, ref index );
				}
				else if( token == TOKEN_CURLY_CLOSE )
				{
					nextToken( json, ref index );
					return o;
				}
				else
				{
					// name
					string name = parseString( json, ref index );
					if( name == null )
					{
						return null;
					}

					// :
					token = nextToken( json, ref index );
					if( token != TOKEN_COLON )
						return null;

					// value
					bool success = true;
					object value = parseValue( json, ref index, ref success, ordered );
					if( !success )
						return null;

					o[name] = value;
				}
			}

			return o;
		}

		protected static IList parseArray( char[] json, ref int index, bool ordered )
		{
			IList array = new _root.Array();

			// [
			nextToken( json, ref index );

			bool done = false;
			while( !done )
			{
				int token = lookAhead( json, index );
				if( token == TOKEN_NONE )
				{
					return null;
				}
				else if( token == TOKEN_COMMA )
				{
					nextToken( json, ref index );
				}
				else if( token == TOKEN_SQUARED_CLOSE )
				{
					nextToken( json, ref index );
					break;
				}
				else
				{
					bool success = true;
					object value = parseValue( json, ref index, ref success, ordered );
					if( !success )
						return null;

					array.Add( value );
				}
			}

			return array;
		}


		protected static object parseValue( char[] json, ref int index, ref bool success, bool ordered )
		{
			switch( lookAhead( json, index ) )
			{
			case TOKEN_STRING:
				return parseString( json, ref index );
			case TOKEN_NUMBER:
				return parseNumber( json, ref index );
			case TOKEN_CURLY_OPEN:
				return parseObject( json, ref index, ordered );
			case TOKEN_SQUARED_OPEN:
				return parseArray( json, ref index, ordered );
			case TOKEN_TRUE:
				nextToken( json, ref index );
				return Boolean.Parse( "TRUE" );
			case TOKEN_FALSE:
				nextToken( json, ref index );
				return Boolean.Parse( "FALSE" );
			case TOKEN_NULL:
				nextToken( json, ref index );
				return null;
			case TOKEN_NONE:
				break;
			}

			success = false;
			return null;
		}


		protected static string parseString( char[] json, ref int index )
		{
			StringBuilder sb = new StringBuilder(); // <Kooz> Because string.operator+= is ASTOUNDINGLY slow on large strings when you do it char-by-char.
			char c;

			eatWhitespace( json, ref index );

			// "
			c = json[index++];

			bool complete = false;
			while( !complete )
			{
				if( index == json.Length )
					break;

				c = json[index++];
				if( c == '"' )
				{
					complete = true;
					break;
				}
				else if( c == '\\' )
				{
					if( index == json.Length )
						break;

					c = json[index++];
					if( c == '"' )
					{
						sb.Append('"');
					}
					else if( c == '\\' )
					{
						sb.Append('\\');
					}
					else if( c == '/' )
					{
						sb.Append('/');
					}
					else if( c == 'b' )
					{
						sb.Append('\b');
					}
					else if( c == 'f' )
					{
						sb.Append('\f');
					}
					else if( c == 'n' )
					{
						sb.Append('\n');
					}
					else if( c == 'r' )
					{
						sb.Append('\r');
					}
					else if( c == 't' )
					{
						sb.Append('\t');
					}
					else if( c == 'u' )
					{
						int remainingLength = json.Length - index;
						if( remainingLength >= 4 )
						{
							char[] unicodeCharArray = new char[4];
							Array.Copy( json, index, unicodeCharArray, 0, 4 );

							// Drop in the HTML markup for the unicode character
							sb.Append("&#x" + new string( unicodeCharArray ) + ";");

								/*
	uint codePoint = UInt32.Parse(new string(unicodeCharArray), NumberStyles.HexNumber);
	// convert the integer codepoint to a unicode char and add to string
	s += Char.ConvertFromUtf32((int)codePoint);
	*/

							// skip 4 chars
							index += 4;
						}
						else
						{
							break;
						}

					}
				}
				else
				{
					sb.Append(c);
				}

			}

			if( !complete )
				return null;

			return sb.ToString();
		}
		
		
		protected static double parseNumber( char[] json, ref int index )
		{
			eatWhitespace( json, ref index );

			int lastIndex = getLastIndexOfNumber( json, index );
			int charLength = ( lastIndex - index ) + 1;
			char[] numberCharArray = new char[charLength];

			Array.Copy( json, index, numberCharArray, 0, charLength );
			index = lastIndex + 1;
			return double.Parse( new string( numberCharArray ) ); // , CultureInfo.InvariantCulture);
		}
		
		
		protected static int getLastIndexOfNumber( char[] json, int index )
		{
			int lastIndex;
			for( lastIndex = index; lastIndex < json.Length; ++lastIndex )
			if( "0123456789+-.eE".IndexOf( json[lastIndex] ) == -1 )
			{
				break;
			}
			return lastIndex - 1;
		}
		
		
		protected static void eatWhitespace( char[] json, ref int index )
		{
			for( ; index < json.Length; ++index )
				if( " \t\n\r".IndexOf( json[index] ) == -1 )
				{
					break;
				}
		}
		
		
		protected static int lookAhead( char[] json, int index )
		{
			int saveIndex = index;
			return nextToken( json, ref saveIndex );
		}

		
		protected static int nextToken( char[] json, ref int index )
		{
			eatWhitespace( json, ref index );

			if( index == json.Length )
			{
				return TOKEN_NONE;
			}
			
			char c = json[index];
			index++;
			switch( c )
			{
				case '{':
					return TOKEN_CURLY_OPEN;
				case '}':
					return TOKEN_CURLY_CLOSE;
				case '[':
					return TOKEN_SQUARED_OPEN;
				case ']':
					return TOKEN_SQUARED_CLOSE;
				case ',':
					return TOKEN_COMMA;
				case '"':
					return TOKEN_STRING;
				case '0':
				case '1':
				case '2':
				case '3':
				case '4': 
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '-': 
					return TOKEN_NUMBER;
				case ':':
					return TOKEN_COLON;
			}
			index--;

			int remainingLength = json.Length - index;

			// false
			if( remainingLength >= 5 )
			{
				if( json[index] == 'f' &&
					json[index + 1] == 'a' &&
					json[index + 2] == 'l' &&
					json[index + 3] == 's' &&
					json[index + 4] == 'e' )
				{
					index += 5;
					return TOKEN_FALSE;
				}
			}

			// true
			if( remainingLength >= 4 )
			{
				if( json[index] == 't' &&
					json[index + 1] == 'r' &&
					json[index + 2] == 'u' &&
					json[index + 3] == 'e' )
				{
					index += 4;
					return TOKEN_TRUE;
				}
			}

			// null
			if( remainingLength >= 4 )
			{
				if( json[index] == 'n' &&
					json[index + 1] == 'u' &&
					json[index + 2] == 'l' &&
					json[index + 3] == 'l' )
				{
					index += 4;
					return TOKEN_NULL;
				}
			}

			return TOKEN_NONE;
		}

		#endregion
		
		
		#region Serialization
		
		protected static bool serializeObjectOrArray( object objectOrArray, StringBuilder builder, int depth )
		{
			if( objectOrArray is IDictionary )
			{
				return serializeObject( (IDictionary)objectOrArray, builder, depth);
			}
			else if( objectOrArray is IList )
			{
				return serializeArray( (IList)objectOrArray, builder, depth );
			}
			else
			{
				return false;
			}
		}

		protected static bool serializeObject( IEnumerable< KeyValuePair<string, object> > anObject, StringBuilder builder , int depth )
		{
			String tabs;
			String tabsEntry;
			if( depth >= 0 ) {
				tabs = getTabs(depth);
				tabsEntry = getTabs (depth+1);
				builder.Append( "{\n" );
			} else {
				tabs = "";
				tabsEntry = "";
				builder.Append( "{" );
			}

			bool first = true;
			foreach (var kvp in anObject)
			{
				string key = kvp.Key;
				object value = kvp.Value;

				if( !first )
				{
					builder.Append( ", " );
					if( depth >= 0 ) {
						builder.Append( "\n" );
					}
				}

				if( depth >= 0  ) {
					builder.Append( tabsEntry );
				}
				serializeString( key, builder );
				builder.Append( ": " );
				int newDepth = ( depth >= 0 ) ? depth + 1 : depth;
				if( !serializeValue( value, builder, newDepth) )
				{
					return false;
				}

				first = false;
			}

			if( depth >= 0 ) {
				builder.Append( "\n" );
				builder.Append( tabs );
			}
			builder.Append( "}" );
			return true;
		}


		protected static bool serializeObject( IDictionary anObject, StringBuilder builder , int depth )
		{
			String tabs;
			String tabsEntry;
			if( depth >= 0 ) {
				tabs = getTabs(depth);
				tabsEntry = getTabs (depth+1);
				builder.Append( "{\n" );
			} else {
				tabs = "";
				tabsEntry = "";
				builder.Append( "{" );
			}
				
			IDictionaryEnumerator e = anObject.GetEnumerator();
			bool first = true;
			while( e.MoveNext() )
			{
				string key = e.Key.ToString();
				object value = e.Value;

				if( !first )
				{
					builder.Append( ", " );
					if( depth >= 0 ) {
						builder.Append( "\n" );
					}
				}

				if( depth >= 0  ) {
					builder.Append( tabsEntry );
				}
				serializeString( key, builder );
				builder.Append( ": " );
				int newDepth = ( depth >= 0 ) ? depth + 1 : depth;
				if( !serializeValue( value, builder, newDepth) )
				{
					return false;
				}

				first = false;
			}
			
			if( depth >= 0 ) {
				builder.Append( "\n" );
				builder.Append( tabs );
			}
			builder.Append( "}" );
			return true;
		}

		protected static bool serializeArray( IList anArray, StringBuilder builder , int depth )
		{
			String tabs;
			String tabsEntry;
			if( depth >= 0 ) {
				tabs = getTabs(depth);
				tabsEntry = getTabs (depth+1);
			
				builder.Append( "[\n" );
			} else {
				tabs = "";
				tabsEntry = "";
				builder.Append( "[" );
			}

			bool first = true;
			for( int i = 0; i < anArray.Count; ++i )
			{
				object value = anArray[i];

				if( !first )
				{
					if( depth >= 0 ) {
						builder.Append( ", \n" );
					} else {
					    builder.Append( ", " );
					}
				}

				if( depth >= 0 ) {
					builder.Append( tabsEntry );
				}
				int newDepth = ( depth >= 0 ) ? depth + 1 : depth;
				if( !serializeValue( value, builder, newDepth ) )
				{
					return false;
				}

				first = false;
			}
			
			if( depth >= 0 ) {
				builder.Append( "\n" );
				builder.Append( tabs );
			}
			builder.Append( "]" );
			return true;
		}

		protected static StringBuilder sTabsBuilder = new StringBuilder();
		protected static string getTabs(int depth) {
			if( depth >= 0 ) {
				sTabsBuilder.Length = 0;
				sTabsBuilder.Capacity = 0;
				for (int i = 0; i < depth; i++) {
					sTabsBuilder.Append("\t");
				}
				return sTabsBuilder.ToString();
			} else {
				return "";
			}
		}

		
		protected static bool serializeValue( object value, StringBuilder builder, int depth )
		{
			// Type t = value.GetType();
			// Debug.Log("type: " + t.ToString() + " isArray: " + t.IsArray);

			if( value == null )
			{
				builder.Append( "null" );
			}
			else if( value is string )
			{
				serializeString( (string)value, builder );
			}
			else if( value is Char )
			{
				serializeString( Convert.ToString( (char)value ), builder );
			}
			else if( value is IEnumerable< KeyValuePair<string, object> > )
			{
				serializeObject( (IEnumerable< KeyValuePair<string, object> >)value, builder, depth );
			}
			else if( value is IDictionary )
			{
				serializeObject( (IDictionary)value, builder, depth );
			}
			else if( value is IList )
			{
				serializeArray( (IList)value, builder, depth );
			}
			else if( ( value is Boolean ) && ( (Boolean)value == true ) )
			{
				builder.Append( "true" );
			}
			else if( ( value is Boolean ) && ( (Boolean)value == false ) )
			{
				builder.Append( "false" );
			}
			else if( value.GetType().IsPrimitive )
			{
				serializeNumber( Convert.ToDouble( value ), builder);
			}
			else
			{
				return false;
			}

			return true;
		}

		
		protected static void serializeString( string aString, StringBuilder builder)
		{
			builder.Append( "\"" );

			char[] charArray = aString.ToCharArray();
			for( int i = 0; i < charArray.Length; ++i )
			{
				char c = charArray[i];
				if( c == '"' )
				{
					builder.Append( "\\\"" );
				}
				else if( c == '\\' )
				{
					builder.Append( "\\\\" );
				}
				else if( c == '\b' )
				{
					builder.Append( "\\b" );
				}
				else if( c == '\f' )
				{
					builder.Append( "\\f" );
				}
				else if( c == '\n' )
				{
					builder.Append( "\\n" );
				}
				else if( c == '\r' )
				{
					builder.Append( "\\r" );
				}
				else if( c == '\t' )
				{
					builder.Append( "\\t" );
				}
				else
				{
					int codepoint = Convert.ToInt32( c );
					if( ( codepoint >= 32 ) && ( codepoint <= 126 ) )
					{
						builder.Append( c );
					}
					else
					{
						builder.Append( "\\u" + Convert.ToString( codepoint, 16 ).PadLeft( 4, '0' ) );
					}
				}
			}

			builder.Append( "\"" );
		}

		protected static void serializeNumber( double number, StringBuilder builder )
		{
			builder.Append (number.ToString ());
		}
		
		#endregion
		
	}
}
