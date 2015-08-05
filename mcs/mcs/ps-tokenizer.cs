//
// cs-tokenizer.cs: The Tokenizer for the PlayScript compiler
//                  This also implements the preprocessor
//
// Author: Miguel de Icaza (miguel@gnu.org)
//         Marek Safar (marek.safar@gmail.com)
//         Ben Cooley (bcooley@zynga.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//

using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Collections;
using Mono.CSharp;

namespace Mono.PlayScript
{
	//
	// This class has to be used by parser only, it reuses token
	// details once a file is parsed
	//
	public class LocatedToken
	{
		public int row, column;
		public string value;
		public SourceFile file;

		public LocatedToken ()
		{
		}

		public LocatedToken (string value, Location loc)
		{
			this.value = value;
			file = loc.SourceFile;
			row = loc.Row;
			column = loc.Column;
		}

		public override string ToString ()
		{
			return string.Format ("Token '{0}' at {1},{2}", Value, row, column);
		}

		public Location Location
		{
			get { return new Location (file, row, column); }
		}

		public string Value
		{
			get { return value; }
		}
	}

	/// <summary>
	///    Tokenizer for C# source code. 
	/// </summary>
	public class Tokenizer : yyParser.yyInput
	{
		class KeywordEntry<T>
		{
			public readonly T Token;
			public KeywordEntry<T> Next;
			public readonly char[] Value;

			public KeywordEntry (string value, T token)
			{
				this.Value = value.ToCharArray ();
				this.Token = token;
			}
		}

		sealed class IdentifiersComparer : IEqualityComparer<char[]>
		{
			readonly int length;

			public IdentifiersComparer (int length)
			{
				this.length = length;
			}

			public bool Equals (char[] x, char[] y)
			{
				for (int i = 0; i < length; ++i)
					if (x [i] != y [i])
						return false;

				return true;
			}

			public int GetHashCode (char[] obj)
			{
				int h = 0;
				for (int i = 0; i < length; ++i)
					h = (h << 5) - h + obj [i];

				return h;
			}
		}

		public class LocatedTokenBuffer
		{
			readonly LocatedToken[] buffer;
			public int pos;

			public LocatedTokenBuffer ()
			{
				buffer = new LocatedToken[0];
			}

			public LocatedTokenBuffer (LocatedToken[] buffer)
			{
				this.buffer = buffer ?? new LocatedToken[0];
			}

			public LocatedToken Create (SourceFile file, int row, int column)
			{
				return Create (null, file, row, column);
			}
			
			public LocatedToken Create (string value, SourceFile file, int row, int column)
			{
				//
				// TODO: I am not very happy about the logic but it's the best
				// what I could come up with for now.
				// Ideally we should be using just tiny buffer (256 elements) which
				// is enough to hold all details for currect stack and recycle elements
				// poped from the stack but there is a trick needed to recycle
				// them properly.
				//
				LocatedToken entry;
				if (pos >= buffer.Length) {
					entry = new LocatedToken ();
				} else {
					entry = buffer[pos];
					if (entry == null) {
						entry = new LocatedToken ();
						buffer[pos] = entry;
					}

					++pos;
				}
				entry.value = value;
				entry.file = file;
				entry.row = row;
				entry.column = column;
				return entry;
			}

			//
			// Used for token not required by expression evaluator
			//
			[Conditional ("FULL_AST")]
			public void CreateOptional (SourceFile file, int row, int col, ref object token)
			{
				token = Create (file, row, col);
			}
		}

		public enum PreprocessorDirective
		{
			Invalid = 0,

			Region = 1,
			Endregion = 2,
			If = 3 | RequiresArgument,
			Endif = 4,
			Elif = 5 | RequiresArgument,
			Else = 6,
			Define = 7 | RequiresArgument,
			Undef = 8 | RequiresArgument,
			Error = 9,
			Warning = 10,
			Pragma = 11 | CustomArgumentsParsing,
			Line = 12 | CustomArgumentsParsing,

			CustomArgumentsParsing = 1 << 10,
			RequiresArgument = 1 << 11
		}

		readonly SeekableStreamReader reader;
		readonly CompilationSourceFile source_file;
		readonly CompilerContext context;

		SourceFile current_source;
		Location hidden_block_start;
		int ref_line = 1;
		int line = 1;
		int col = 0;
		int previous_col;
		int prev_token;
		int prev_token_line;
		int current_token;
		int current_token_line;
		int putback_token = -1;
		int parse_regex_xml = 0;
		int parse_colon = 0;
		bool prev_allow_auto_semi = true;
		bool allow_auto_semi = true;
		int allow_auto_semi_after = 0;
		bool has_temp_auto_semi_after_tokens = false;
		List<int> temp_auto_semi_after_tokens = new List<int>();
		readonly int tab_size;
		bool parsing_playscript = false;
		bool handle_namespace = true;
		bool handle_get_set = false;
		bool handle_dynamic = true;
		bool handle_each = false;
		bool handle_remove_add = false;
		bool handle_where = false;
		bool handle_typeof = false;
		bool handle_for_in = false;
		bool eat_block = false;
		int eat_block_braces = 0;
		List<Location> escaped_identifiers;
		int parsing_generic_less_than;
		readonly bool doc_processing;
		readonly LocatedTokenBuffer ltb;

		private static BitArray allowed_auto_semi_tokens = new BitArray(750, false);  
		private static BitArray disallowed_next_auto_semi_tokens = new BitArray(750, false);  

		//
		// Used mainly for parser optimizations. Some expressions for instance
		// can appear only in block (including initializer, base initializer)
		// scope only
		//
		public int parsing_block;
		internal bool query_parsing;
		
		// 
		// When parsing type only, useful for ambiguous nullable types
		//
		public int parsing_type;
		
		//
		// Set when parsing generic declaration (type or method header)
		//
		public bool parsing_generic_declaration;
		public bool parsing_generic_declaration_doc;
		
		//
		// The value indicates that we have not reach any declaration or
		// namespace yet
		//
		public int parsing_declaration;

		public bool parsing_attribute_section;

		public bool parsing_modifiers;

		//
		// The special characters to inject on streams to run the unit parser
		// in the special expression mode. Using private characters from
		// Plane Sixteen (U+100000 to U+10FFFD)
		//
		// This character is only tested just before the tokenizer is about to report
		// an error;   So on the regular operation mode, this addition will have no
		// impact on the tokenizer's performance.
		//
		
		public const int EvalStatementParserCharacter = 0x100000;
		public const int EvalCompilationUnitParserCharacter = 0x100001;
		public const int EvalUsingDeclarationsParserCharacter = 0x100002;
		public const int DocumentationXref = 0x100003;
		
		const int UnicodeLS = 0x2028;
		const int UnicodePS = 0x2029;
		
		//
		// XML documentation buffer. The save point is used to divide
		// comments on types and comments on members.
		//
		StringBuilder xml_comment_buffer;

		//
		// See comment on XmlCommentState enumeration.
		//
		XmlCommentState xml_doc_state = XmlCommentState.Allowed;

		//
		// Whether tokens have been seen on this line
		//
		bool tokens_seen = false;

		//
		// Set to true once the GENERATE_COMPLETION token has bee
		// returned.   This helps produce one GENERATE_COMPLETION,
		// as many COMPLETE_COMPLETION as necessary to complete the
		// AST tree and one final EOF.
		//
		bool generated;
		
		//
		// Whether a token has been seen on the file
		// This is needed because `define' is not allowed to be used
		// after a token has been seen.
		//
		bool any_token_seen;

		//
		// Class variables
		// 
		static readonly KeywordEntry<int>[][] keywords;
		static readonly KeywordEntry<PreprocessorDirective>[][] keywords_preprocessor;
		static readonly HashSet<string> keyword_strings;
		static readonly NumberStyles styles;
		static readonly NumberFormatInfo csharp_format_info;

		// Pragma arguments
		static readonly char[] pragma_warning = "warning".ToCharArray ();
		static readonly char[] pragma_warning_disable = "disable".ToCharArray ();
		static readonly char[] pragma_warning_restore = "restore".ToCharArray ();
		static readonly char[] pragma_checksum = "checksum".ToCharArray ();
		static readonly char[] line_hidden = "hidden".ToCharArray ();
		static readonly char[] line_default = "default".ToCharArray ();

		static readonly char[] simple_whitespaces = new char[] { ' ', '\t' };

		public bool ParsingPlayScript {
			get { return parsing_playscript; }
			set { parsing_playscript = value; }
		}

		public bool NamespaceParsing {
			get { return handle_namespace; }
			set { handle_namespace = value; }
		}

		public bool PropertyParsing {
			get { return handle_get_set; }
			set { handle_get_set = value; }
		}

		public bool EventParsing {
			get { return handle_remove_add; }
			set { handle_remove_add = value; }
		}

		public bool ConstraintsParsing {
			get { return handle_where; }
			set { handle_where = value; }
		}

		public bool TypeOfParsing {
			get { return handle_typeof; }
			set { handle_typeof = value; }
		}

		public bool ForInParsing {
			get { return handle_for_in; }
			set { handle_for_in = value; }
		}

		public bool DynamicParsing {
			get { return handle_dynamic; }
			set { handle_dynamic = value; }
		}

		public bool AutoSemiInsertion {
			get { return allow_auto_semi; }
			set { 
				prev_allow_auto_semi = allow_auto_semi;
				allow_auto_semi = value; 
				allow_auto_semi_after = 0; 
			}
		}

		public bool PrevAutoSemiInsertion {
			get { return prev_allow_auto_semi; }
		}

		public int AutoSemiInsertionAfter 
		{
			get { return allow_auto_semi_after; }
			set { 
				allow_auto_semi = true;
				allow_auto_semi_after = value + 1;
			}
		}

		public void AllowAutoSemiAfterToken (int token, bool allow)
		{
			allowed_auto_semi_tokens.Set (token, allow);
			if (true) {
				has_temp_auto_semi_after_tokens = true;
				temp_auto_semi_after_tokens.Add (token);
			}
		}

		public bool RegexXmlParsing {
			get { return parse_regex_xml > 0; }
		}

		public bool EatBlock {
			get { return eat_block; }
			set { 
				eat_block = value; 
				if (eat_block) {
					eat_block_braces = 0; 
				}
			}
		}

		public XmlCommentState doc_state {
			get { return xml_doc_state; }
			set {
				if (value == XmlCommentState.Allowed) {
					check_incorrect_doc_comment ();
					reset_doc_comment ();
				}
				xml_doc_state = value;
			}
		}

		//
		// This is used to trigger completion generation on the parser
		public bool CompleteOnEOF;
		
		void AddEscapedIdentifier (Location loc)
		{
			if (escaped_identifiers == null)
				escaped_identifiers = new List<Location> ();

			escaped_identifiers.Add (loc);
		}

		public bool IsEscapedIdentifier (ATypeNameExpression name)
		{
			return escaped_identifiers != null && escaped_identifiers.Contains (name.Location);
		}

		//
		// Values for the associated token returned
		//
		internal int putback_char; 	// Used by repl only
		object val;

		//
		// Pre-processor
		//
		const int TAKING        = 1;
		const int ELSE_SEEN     = 4;
		const int PARENT_TAKING = 8;
		const int REGION        = 16;		

		//
		// pre-processor if stack state:
		//
		Stack<int> ifstack;

		public const int MaxIdentifierLength = 512;
		public const int MaxNumberLength = 512;

		readonly char[] id_builder;
		readonly Dictionary<char[], string>[] identifiers;
		readonly char[] number_builder;
		int number_pos;

		char[] value_builder = new char[64];

		public int Line {
			get {
				return ref_line;
			}
		}

		//
		// This is used when the tokenizer needs to save
		// the current position as it needs to do some parsing
		// on its own to deamiguate a token in behalf of the
		// parser.
		//
		Stack<Position> position_stack = new Stack<Position> (2);

		class Position {
			public int position;
			public int line;
			public int ref_line;
			public int col;
			public Location hidden;
			public int putback_char;
			public int previous_col;
			public Stack<int> ifstack;
			public int parsing_generic_less_than;
			public int parse_regex_xml;
			public int parse_colon;
			public bool prev_allow_auto_semi;
			public bool allow_auto_semi;
			public int allow_auto_semi_after;
			public object val;
			public int prev_token;
			public int prev_token_line;
			public int current_token;
			public int current_token_line;
			public int putback_token;

			public Position (Tokenizer t)
			{
				position = t.reader.Position;
				line = t.line;
				ref_line = t.ref_line;
				col = t.col;
				hidden = t.hidden_block_start;
				putback_char = t.putback_char;
				previous_col = t.previous_col;
				if (t.ifstack != null && t.ifstack.Count != 0) {
					// There is no simple way to clone Stack<T> all
					// methods reverse the order
					var clone = t.ifstack.ToArray ();
					Array.Reverse (clone);
					ifstack = new Stack<int> (clone);
				}
				parsing_generic_less_than = t.parsing_generic_less_than;
				parse_regex_xml = t.parse_regex_xml;
				parse_colon = t.parse_colon;
				prev_allow_auto_semi = t.prev_allow_auto_semi;
				allow_auto_semi = t.allow_auto_semi;
				allow_auto_semi_after = t.allow_auto_semi_after;
				prev_token = t.prev_token;
				prev_token_line = t.prev_token_line;
				current_token = t.current_token;
				current_token_line = t.current_token_line;
				putback_token = t.putback_token;
				val = t.val;
			}
		}

		public Tokenizer (SeekableStreamReader input, CompilationSourceFile file, ParserSession session)
		{
			this.source_file = file;
			// SourceFile will be null if we are running in the repl;
			// TODO: Determine if REPL should allow Extended PlayScript language, 
			// for now to run Tamarin tests, only ActionScript
			if (file.SourceFile != null) {
				this.parsing_playscript = file.SourceFile.PsExtended;
			}
			this.context = file.Compiler;
			this.current_source = file.SourceFile;
			this.identifiers = session.Identifiers;
			this.id_builder = session.IDBuilder;
			this.number_builder = session.NumberBuilder;
			this.ltb = new LocatedTokenBuffer (session.AsLocatedTokens);

			reader = input;

			putback_char = -1;

			xml_comment_buffer = new StringBuilder ();
			doc_processing = context.Settings.DocumentationFile != null;

			tab_size = context.Settings.TabSize;
		}
		
		public void PushPosition ()
		{
			position_stack.Push (new Position (this));
		}

		public void PopPosition ()
		{
			Position p = position_stack.Pop ();

			reader.Position = p.position;
			ref_line = p.ref_line;
			line = p.line;
			col = p.col;
			hidden_block_start = p.hidden;
			putback_char = p.putback_char;
			previous_col = p.previous_col;
			ifstack = p.ifstack;
			parsing_generic_less_than = p.parsing_generic_less_than;
			parse_regex_xml = p.parse_regex_xml;
			parse_colon = p.parse_colon;
			prev_token = p.prev_token;
			prev_token_line = p.prev_token_line;
			prev_allow_auto_semi = p.prev_allow_auto_semi;
			allow_auto_semi = p.allow_auto_semi;
			allow_auto_semi_after = p.allow_auto_semi_after;
			current_token = p.current_token;
			current_token_line = p.current_token_line;
			putback_token = p.putback_token;
			val = p.val;
		}

		// Do not reset the position, ignore it.
		public void DiscardPosition ()
		{
			position_stack.Pop ();
		}
		
		static void AddKeyword (string kw, int token)
		{
			keyword_strings.Add (kw);

			AddKeyword (keywords, kw, token);
		}

		static void AddPreprocessorKeyword (string kw, PreprocessorDirective directive)
		{
			AddKeyword (keywords_preprocessor, kw, directive);
		}

		static void AddKeyword<T> (KeywordEntry<T>[][] keywords, string kw, T token)
		{
			int length = kw.Length;
			if (keywords[length] == null) {
				keywords[length] = new KeywordEntry<T>['z' - '_' + 1];
			}

			int char_index = kw[0] - '_';
			var kwe = keywords[length][char_index];
			if (kwe == null) {
				keywords[length][char_index] = new KeywordEntry<T> (kw, token);
				return;
			}

			while (kwe.Next != null) {
				kwe = kwe.Next;
			}

			kwe.Next = new KeywordEntry<T> (kw, token);
		}

		static void AddAllowedAutoSemiTokens(int[] tokens) {
			var len = tokens.Length;
			for (var i = 0; i < len; i++) {
				allowed_auto_semi_tokens.Set (tokens[i], true);
			}
		}

		static void AddDisallowedNextAutoSemiTokens(int[] tokens) {
			var len = tokens.Length;
			for (var i = 0; i < len; i++) {
				disallowed_next_auto_semi_tokens.Set (tokens[i], true);
			}
		}

		//
		// Class initializer
		// 
		static Tokenizer ()
		{
			keyword_strings = new HashSet<string> ();

			// 13 is the length of the longest keyword for now
			keywords = new KeywordEntry<int>[13][];

			AddKeyword ("abstract", Token.ABSTRACT);
			AddKeyword ("as", Token.AS);
			AddKeyword ("add", Token.ADD);
			AddKeyword ("bool", Token.BOOL);
			AddKeyword ("boolean", Token.BOOLEAN);
			AddKeyword ("break", Token.BREAK);
			AddKeyword ("byte", Token.BYTE);
			AddKeyword ("case", Token.CASE);
			AddKeyword ("catch", Token.CATCH);
			AddKeyword ("char", Token.CHAR);
			AddKeyword ("checked", Token.CHECKED);
			AddKeyword ("class", Token.CLASS);
			AddKeyword ("const", Token.CONST);
			AddKeyword ("continue", Token.CONTINUE);
			AddKeyword ("decimal", Token.DECIMAL);
			AddKeyword ("default", Token.DEFAULT);
			AddKeyword ("delegate", Token.DELEGATE);
			AddKeyword ("delete", Token.DELETE);
			AddKeyword ("do", Token.DO);
			AddKeyword ("double", Token.DOUBLE);
			AddKeyword ("double2", Token.DOUBLE2);
			AddKeyword ("double3", Token.DOUBLE3);
			AddKeyword ("double4", Token.DOUBLE4);
			AddKeyword ("dynamic", Token.DYNAMIC);
			AddKeyword ("each", Token.EACH);
			AddKeyword ("else", Token.ELSE);
			AddKeyword ("enum", Token.ENUM);
			AddKeyword ("event", Token.EVENT);
			AddKeyword ("explicit", Token.EXPLICIT);
			AddKeyword ("extends", Token.EXTENDS);
			AddKeyword ("extern", Token.EXTERN);
			AddKeyword ("false", Token.FALSE);
			AddKeyword ("final", Token.FINAL);
			AddKeyword ("finally", Token.FINALLY);
			AddKeyword ("fixed", Token.FIXED);
			AddKeyword ("float", Token.FLOAT);
			AddKeyword ("float2", Token.FLOAT2);
			AddKeyword ("float3", Token.FLOAT3);
			AddKeyword ("float4", Token.FLOAT4);
			AddKeyword ("for", Token.FOR);
			AddKeyword ("function", Token.FUNCTION);
			AddKeyword ("goto", Token.GOTO);
			AddKeyword ("get", Token.GET);
			AddKeyword ("if", Token.IF);
			AddKeyword ("implements", Token.IMPLEMENTS);
			AddKeyword ("implicit", Token.IMPLICIT);
			AddKeyword ("import", Token.IMPORT);
			AddKeyword ("in", Token.IN);
			AddKeyword ("indexer", Token.INDEXER);
			AddKeyword ("instanceof", Token.INSTANCEOF);
			AddKeyword ("int", Token.INT);
			AddKeyword ("interface", Token.INTERFACE);
			AddKeyword ("internal", Token.INTERNAL);
			AddKeyword ("is", Token.IS);
			AddKeyword ("lock", Token.LOCK);
			AddKeyword ("long", Token.LONG);
			AddKeyword ("namespace", Token.NAMESPACE);
			AddKeyword ("native", Token.NATIVE);
			AddKeyword ("new", Token.NEW);
			AddKeyword ("null", Token.NULL);
			AddKeyword ("object", Token.OBJECT);
			AddKeyword ("operator", Token.OPERATOR);
			AddKeyword ("out", Token.OUT);
			AddKeyword ("override", Token.OVERRIDE);
			AddKeyword ("overload", Token.OVERLOAD);
			AddKeyword ("package", Token.PACKAGE);
			AddKeyword ("params", Token.PARAMS);
			AddKeyword ("property", Token.PROPERTY);
			AddKeyword ("private", Token.PRIVATE);
			AddKeyword ("protected", Token.PROTECTED);
			AddKeyword ("public", Token.PUBLIC);
			AddKeyword ("readonly", Token.READONLY);
			AddKeyword ("ref", Token.REF);
			AddKeyword ("remove", Token.REMOVE);
			AddKeyword ("return", Token.RETURN);
			AddKeyword ("sbyte", Token.SBYTE);
			AddKeyword ("set", Token.SET);
			AddKeyword ("short", Token.SHORT);
			AddKeyword ("sizeof", Token.SIZEOF);
			AddKeyword ("stackalloc", Token.STACKALLOC);
			AddKeyword ("static", Token.STATIC);
			AddKeyword ("string", Token.STRING);
			AddKeyword ("struct", Token.STRUCT);
			AddKeyword ("super", Token.SUPER);
			AddKeyword ("switch", Token.SWITCH);
			AddKeyword ("this", Token.THIS);
			AddKeyword ("throw", Token.THROW);
			AddKeyword ("true", Token.TRUE);
			AddKeyword ("try", Token.TRY);
			AddKeyword ("typeof", Token.TYPEOF);
			AddKeyword ("uint", Token.UINT);
			AddKeyword ("ulong", Token.ULONG);
			AddKeyword ("unchecked", Token.UNCHECKED);
			AddKeyword ("undefined", Token.UNDEFINED);
			AddKeyword ("unsafe", Token.UNSAFE);
			AddKeyword ("use", Token.USE);
			AddKeyword ("ushort", Token.USHORT);
			AddKeyword ("using", Token.USING);
			AddKeyword ("var", Token.VAR);
			AddKeyword ("virtual", Token.VIRTUAL);
			AddKeyword ("void", Token.VOID);
			AddKeyword ("volatile", Token.VOLATILE);
			AddKeyword ("while", Token.WHILE);
			AddKeyword ("partial", Token.PARTIAL);
			AddKeyword ("where", Token.WHERE);

			// LINQ keywords
			AddKeyword ("from", Token.FROM);
			AddKeyword ("join", Token.JOIN);
			AddKeyword ("on", Token.ON);
			AddKeyword ("equals", Token.EQUALS);
			AddKeyword ("select", Token.SELECT);
			AddKeyword ("group", Token.GROUP);
			AddKeyword ("by", Token.BY);
			AddKeyword ("let", Token.LET);
			AddKeyword ("orderby", Token.ORDERBY);
			AddKeyword ("ascending", Token.ASCENDING);
			AddKeyword ("descending", Token.DESCENDING);
			AddKeyword ("into", Token.INTO);

			// Contextual async keywords
			AddKeyword ("async", Token.ASYNC);
			AddKeyword ("await", Token.AWAIT);

			keywords_preprocessor = new KeywordEntry<PreprocessorDirective>[10][];

			AddPreprocessorKeyword ("region", PreprocessorDirective.Region);
			AddPreprocessorKeyword ("endregion", PreprocessorDirective.Endregion);
			AddPreprocessorKeyword ("if", PreprocessorDirective.If);
			AddPreprocessorKeyword ("endif", PreprocessorDirective.Endif);
			AddPreprocessorKeyword ("elif", PreprocessorDirective.Elif);
			AddPreprocessorKeyword ("else", PreprocessorDirective.Else);
			AddPreprocessorKeyword ("define", PreprocessorDirective.Define);
			AddPreprocessorKeyword ("undef", PreprocessorDirective.Undef);
			AddPreprocessorKeyword ("error", PreprocessorDirective.Error);
			AddPreprocessorKeyword ("warning", PreprocessorDirective.Warning);
			AddPreprocessorKeyword ("pragma", PreprocessorDirective.Pragma);
			AddPreprocessorKeyword ("line", PreprocessorDirective.Line);

			// Semicolons will be auto-inserted after these tokens by default (unless manually disabled by the parser).
			AddAllowedAutoSemiTokens(new int [] {
				Token.CLOSE_BRACKET,
				Token.CLOSE_PARENS,
				Token.IDENTIFIER,
				Token.LITERAL,
				Token.OP_INC,
				Token.OP_DEC,
				Token.TRUE,
				Token.FALSE,
				Token.UNDEFINED,
				Token.NULL,
				Token.CHAR,
				Token.INT,
				Token.UINT,
				Token.OBJECT,
				Token.DECIMAL,
				Token.BYTE,
				Token.SBYTE,
				Token.LONG,
				Token.ULONG,
				Token.VOID,
				Token.DOUBLE,
				Token.DOUBLE2,
				Token.DOUBLE3,
				Token.DOUBLE4,
				Token.FLOAT,
				Token.FLOAT2,
				Token.FLOAT3,
				Token.FLOAT4,
				Token.STRING,
				Token.BOOL,
				Token.BOOLEAN,
				Token.SHORT,
				Token.USHORT,
				Token.BREAK,
				Token.CONTINUE,
				Token.RETURN,
				Token.STAR,
				Token.OP_GENERICS_GT
			});

			AddDisallowedNextAutoSemiTokens(new int [] {
				Token.PLUS,
				Token.MINUS,
				Token.DIV,
				Token.PERCENT,
				Token.STAR,
				Token.DOT,
				Token.DOT_AT,
				Token.DOT_STAR,
				Token.DOTDOT,
				Token.DOTDOT_AT,
				Token.DOTDOT_STAR,
				Token.OP_SHIFT_LEFT,
				Token.OP_SHIFT_RIGHT,
				Token.OP_USHIFT_RIGHT,
				Token.LOGICAL_AND_ASSIGN,
				Token.LOGICAL_OR_ASSIGN,
				Token.CLOSE_BRACKET,
				Token.CLOSE_PARENS,
				Token.OP_ADD_ASSIGN,
				Token.OP_AT,
				Token.OP_IN,
				Token.AS,
				Token.IN,
				Token.ARROW,
				Token.ASSIGN,
				Token.COLON,
				Token.COMMA,
				Token.OP_ADD_ASSIGN,
				Token.OP_SUB_ASSIGN,
				Token.OP_MOD_ASSIGN,
				Token.OP_MULT_ASSIGN,
				Token.OP_DIV_ASSIGN,
				Token.OP_COALESCING,
				Token.OP_AND_ASSIGN,
				Token.OP_OR_ASSIGN,
				Token.OP_XOR_ASSIGN,
				Token.OP_SHIFT_LEFT_ASSIGN,
				Token.OP_SHIFT_RIGHT_ASSIGN,
				Token.OP_USHIFT_RIGHT_ASSIGN,
				Token.OP_EQ,
				Token.OP_NE,
				Token.OP_STRICT_EQ,
				Token.OP_STRICT_NE,
				Token.OP_LT,
				Token.OP_GT,
				Token.OP_GE,
				Token.OP_LE,
				Token.OP_AND,
				Token.OP_OR,
				Token.BITWISE_AND,
				Token.BITWISE_OR,
				Token.CARRET,
				Token.INTERR
			});

			csharp_format_info = NumberFormatInfo.InvariantInfo;
			styles = NumberStyles.Float;
		}

		int GetKeyword (char[] id, int id_len)
		{
			//
			// Keywords are stored in an array of arrays grouped by their
			// length and then by the first character
			//
			if (id_len >= keywords.Length || keywords [id_len] == null)
				return -1;

			int first_index = id [0] - '_';
			if (first_index > 'z' - '_')
				return -1;

			var kwe = keywords [id_len] [first_index];
			if (kwe == null)
				return -1;

			int res;
			do {
				res = kwe.Token;
				for (int i = 1; i < id_len; ++i) {
					if (id [i] != kwe.Value [i]) {
						res = 0;
						kwe = kwe.Next;
						break;
					}
				}
			} while (res == 0 && kwe != null);

			if (res == 0)
				return -1;

			int next_token;
			switch (res) {
			case Token.FOR:
				this.handle_each = true;
				next_token = peek_token ();
				if (next_token == Token.EACH) {
					token ();
					res = Token.FOR_EACH;
				}
				this.handle_each = false;
				break;
			case Token.FUNCTION:
				parsing_modifiers = false;
				this.AutoSemiInsertion = false;
				PushPosition();
				var fn_token = token ();
				if (fn_token == Token.IDENTIFIER)
				{
					var get_set = (string)((LocatedToken)val).Value;
					if (get_set == "get" || get_set == "set") {
						fn_token = token ();
						if (fn_token == Token.IDENTIFIER) {
							res = (get_set == "get") ? Token.FUNCTION_GET : Token.FUNCTION_SET;
						}
					}
				}
				PopPosition ();
				if (res != Token.FUNCTION) 
					token ();
				break;
			case Token.GET:
			case Token.SET:
				if (!handle_get_set)
					res = -1;
				break;
			case Token.IF:
			case Token.WHILE:
			case Token.DO:
			case Token.TRY:
			case Token.CATCH:
			case Token.SWITCH:
			case Token.CASE:
				this.AutoSemiInsertion = false;
				break;
			case Token.DYNAMIC:
				if (!handle_dynamic)
					res = -1;
				break;
			case Token.EACH:
				if (!handle_each)
					res = -1;
				break;
			case Token.REMOVE:
			case Token.ADD:
				if (!handle_remove_add || !parsing_playscript)
					res = -1;
				break;
			case Token.EXTERN:
				if (parsing_declaration != 0 || !parsing_playscript)
					res = -1;
				break;
			case Token.DEFAULT:
				if (peek_token () == Token.COLON) {
					token ();
					res = Token.DEFAULT_COLON;
				}
				break;
			case Token.WHERE:
				if (!handle_where && !query_parsing || !parsing_playscript)
					res = -1;
				break;
			case Token.FROM:
				//
				// A query expression is any expression that starts with `from identifier'
				// followed by any token except ; , =
				// 
				if (!parsing_playscript) {
					res = -1;
				} else if (!query_parsing) {
					PushPosition ();
					// HACK: to disable generics micro-parser, because PushPosition does not
					// store identifiers array
					parsing_generic_less_than = 1;
					switch (xtoken ()) {
					case Token.IDENTIFIER:
					case Token.INT:
					case Token.BOOL:
					case Token.BYTE:
					case Token.CHAR:
					case Token.DECIMAL:
					case Token.FLOAT:
					case Token.LONG:
					case Token.OBJECT:
					case Token.STRING:
					case Token.UINT:
					case Token.ULONG:
						next_token = xtoken ();
						if (next_token == Token.SEMICOLON || next_token == Token.COMMA || next_token == Token.EQUALS)
							goto default;
						
						res = Token.FROM_FIRST;
						query_parsing = true;
						if (context.Settings.Version <= LanguageVersion.ISO_2)
							Report.FeatureIsNotAvailable (context, Location, "query expressions");
						break;
					case Token.VOID:
						Expression.Error_VoidInvalidInTheContext (Location, Report);
						break;
					default:
						PopPosition ();
						// HACK: A token is not a keyword so we need to restore identifiers buffer
						// which has been overwritten before we grabbed the identifier
						id_builder [0] = 'f'; id_builder [1] = 'r'; id_builder [2] = 'o'; id_builder [3] = 'm';
						return -1;
					}
					PopPosition ();
				}
				break;
			case Token.JOIN:
			case Token.ON:
			case Token.EQUALS:
			case Token.SELECT:
			case Token.GROUP:
			case Token.BY:
			case Token.LET:
			case Token.ORDERBY:
			case Token.ASCENDING:
			case Token.DESCENDING:
			case Token.INTO:
				if (!query_parsing || !parsing_playscript)
					res = -1;
				break;

			case Token.IN:
				if (!handle_for_in)
					res = Token.OP_IN;
				break;

			case Token.USE:
				handle_namespace = true;
				break;
			case Token.USING:
				// TODO: some explanation needed
				check_incorrect_doc_comment ();
				break;
			case Token.NAMESPACE:
				// TODO: some explanation needed
				if (!handle_namespace)
					res = -1;
				else 
					check_incorrect_doc_comment ();
				break;
				
			case Token.PARTIAL:
				if (parsing_block > 0 || !parsing_playscript) {
					res = -1;
					break;
				}

				// Save current position and parse next token.
				PushPosition ();

				next_token = token ();
				bool ok = (next_token == Token.CLASS) ||
					(next_token == Token.STRUCT) ||
					(next_token == Token.INTERFACE) ||
					(next_token == Token.VOID);

				PopPosition ();

				if (ok) {
					if (next_token == Token.VOID) {
						if (context.Settings.Version <= LanguageVersion.ISO_2)
							Report.FeatureIsNotAvailable (context, Location, "partial methods");
					} else if (context.Settings.Version == LanguageVersion.ISO_1)
						Report.FeatureIsNotAvailable (context, Location, "partial types");

					return res;
				}

				if (next_token < Token.LAST_KEYWORD) {
					Report.Error (267, Location,
						"The `partial' modifier can be used only immediately before `class', `struct', `interface', or `void' keyword");
					return token ();
				}					

				res = -1;
				break;

			case Token.ASYNC:
				if (!parsing_playscript) {
					return -1;
				} else if (parsing_modifiers) {
					//
					// Skip attributes section or constructor called async
					//
					if (parsing_attribute_section || peek_token () == Token.OPEN_PARENS) {
						res = -1;
					} else {
						// async is keyword
					}
				} else if (parsing_block > 0) {
					switch (peek_token ()) {
					case Token.DELEGATE:
						// async is keyword
						break;
					case Token.IDENTIFIER:
						PushPosition ();
						xtoken ();
						if (xtoken () != Token.ARROW)
							res = -1;

						PopPosition ();
						break;
					default:
						res = -1;
						break;
					}
				} else {
					res = -1;
				}

				if (res == Token.ASYNC && context.Settings.Version <= LanguageVersion.V_4) {
					Report.FeatureIsNotAvailable (context, Location, "asynchronous functions");
				}
				
				break;

			case Token.AWAIT:
				if (parsing_block == 0 || !parsing_playscript)
					res = -1;

				break;

				// PLAYSCRIPT Extension Type keywords
			case Token.BOOL:
			case Token.CHAR:
			case Token.BYTE:
			case Token.SBYTE:
			case Token.DECIMAL:
			case Token.OBJECT:
			case Token.STRING:
			case Token.LONG:
			case Token.ULONG:
			case Token.SHORT:
			case Token.USHORT:
			case Token.FLOAT:
			case Token.FLOAT2:
			case Token.FLOAT3:
			case Token.FLOAT4:
			case Token.DOUBLE:
			case Token.DOUBLE2:
			case Token.DOUBLE3:
			case Token.DOUBLE4:
				if (!parsing_playscript)
					res = -1;

				break;

				// PLAYSCRIPT Extension keywords
			case Token.CHECKED:
			case Token.EXPLICIT:
			case Token.IMPLICIT:
			case Token.OVERLOAD:
			case Token.LOCK:
			case Token.OUT:
			case Token.PARAMS:
			case Token.READONLY:
			case Token.REF:
			case Token.UNCHECKED:
			case Token.UNSAFE:
			case Token.FIXED:
			case Token.GOTO:
				if (!parsing_playscript)
					res = -1;

				break;

			case Token.EVENT:
			case Token.INDEXER:
			case Token.OPERATOR:
			case Token.PROPERTY:
				if (!parsing_playscript)
					res = -1;
				else
					parsing_modifiers = false;
				
				break;

			case Token.STRUCT:
			case Token.DELEGATE:
			case Token.ENUM:
				if (!parsing_playscript)
					res = -1;
				else
					parsing_modifiers = handle_namespace = false;
				
				break;

			case Token.CLASS:
			case Token.INTERFACE:
				parsing_modifiers = handle_namespace = false;
				break;

			}


			return res;
		}

		static PreprocessorDirective GetPreprocessorDirective (char[] id, int id_len)
		{
			//
			// Keywords are stored in an array of arrays grouped by their
			// length and then by the first character
			//
			if (id_len >= keywords_preprocessor.Length || keywords_preprocessor[id_len] == null)
				return PreprocessorDirective.Invalid;

			int first_index = id[0] - '_';
			if (first_index > 'z' - '_')
				return PreprocessorDirective.Invalid;

			var kwe = keywords_preprocessor[id_len][first_index];
			if (kwe == null)
				return PreprocessorDirective.Invalid;

			PreprocessorDirective res = PreprocessorDirective.Invalid;
			do {
				res = kwe.Token;
				for (int i = 1; i < id_len; ++i) {
					if (id[i] != kwe.Value[i]) {
						res = 0;
						kwe = kwe.Next;
						break;
					}
				}
			} while (res == PreprocessorDirective.Invalid && kwe != null);

			return res;
		}

		public Location Location {
			get {
				return new Location (current_source, ref_line, col);
			}
		}

		static bool is_identifier_start_character (int c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '$' || Char.IsLetter ((char)c);
		}

		static bool is_identifier_part_character (char c)
		{
			if (c >= 'a' && c <= 'z')
				return true;

			if (c >= 'A' && c <= 'Z')
				return true;

			if (c == '_' || c == '$' || (c >= '0' && c <= '9'))
				return true;

			if (c < 0x80)
				return false;

			return Char.IsLetter (c) || Char.GetUnicodeCategory (c) == UnicodeCategory.ConnectorPunctuation;
		}

		public static bool IsKeyword (string s)
		{
			return keyword_strings.Contains (s);
		}

		//
		// Open parens micro parser. Only detects simple open parens at the moment.
		//	
		int TokenizeOpenParens ()
		{
			return Token.OPEN_PARENS;
		}

		public static bool IsValidIdentifier (string s)
		{
			if (s == null || s.Length == 0)
				return false;

			if (!is_identifier_start_character (s [0]))
				return false;
			
			for (int i = 1; i < s.Length; i ++)
				if (! is_identifier_part_character (s [i]))
					return false;
			
			return true;
		}

		bool parse_less_than ()
		{
		start:
			int the_token = token ();
			if (the_token == Token.OPEN_BRACKET) {
				do {
					the_token = token ();
				} while (the_token != Token.CLOSE_BRACKET);
				the_token = token ();
			} else if (the_token == Token.IN || the_token == Token.OUT) {
				the_token = token ();
			}
			switch (the_token) {
			case Token.IDENTIFIER:
			case Token.OBJECT:
			case Token.STRING:
			case Token.BOOL:
			case Token.DECIMAL:
			case Token.FLOAT:
			case Token.DOUBLE:
			case Token.SBYTE:
			case Token.BYTE:
			case Token.SHORT:
			case Token.USHORT:
			case Token.INT:
			case Token.UINT:
			case Token.LONG:
			case Token.ULONG:
			case Token.CHAR:
			case Token.VOID:
				break;
			case Token.OP_GENERICS_GT:
			case Token.IN:
			case Token.OUT:
				return true;

			default:
				return false;
			}
		again:
			the_token = token ();

			if (the_token == Token.OP_GENERICS_GT)
				return true;
			else if (the_token == Token.COMMA || the_token == Token.DOT || the_token == Token.DOUBLE_COLON)
				goto start;
			else if (the_token == Token.INTERR_NULLABLE || the_token == Token.STAR)
				goto again;
			else if (the_token == Token.OP_GENERICS_LT) {
				if (!parse_less_than ())
					return false;
				goto again;
			} else if (the_token == Token.OPEN_BRACKET) {
			rank_specifiers:
				the_token = token ();
				if (the_token == Token.CLOSE_BRACKET)
					goto again;
				else if (the_token == Token.COMMA)
					goto rank_specifiers;
				return false;
			}

			return false;
		}

		bool parse_generic_dimension (out int dimension)
		{
			dimension = 1;

		again:
			int the_token = token ();
			if (the_token == Token.OP_GENERICS_GT)
				return true;
			else if (the_token == Token.COMMA) {
				dimension++;
				goto again;
			}

			return false;
		}
		
		public int peek_token ()
		{
			int the_token;

			PushPosition ();
			the_token = token ();
			PopPosition ();
			
			return the_token;
		}
					
		//
		// Tonizes `?' using custom disambiguous rules to return one
		// of following tokens: INTERR_NULLABLE, OP_COALESCING, INTERR
		//
		// Tricky expression look like:
		//
		// Foo ? a = x ? b : c;
		//
		int TokenizePossibleNullableType ()
		{
			if (parsing_block == 0 || parsing_type > 0)
				return Token.INTERR_NULLABLE;

			int d = peek_char ();
			if (d == '?') {
				get_char ();
				return Token.OP_COALESCING;
			}

			switch (current_token) {
			case Token.CLOSE_PARENS:
			case Token.TRUE:
			case Token.FALSE:
			case Token.NULL:
			case Token.LITERAL:
				return Token.INTERR;
			}

			if (d != ' ') {
				if (d == ',' || d == ';' || d == '>')
					return Token.INTERR_NULLABLE;
				if (d == '*' || (d >= '0' && d <= '9'))
					return Token.INTERR;
			}

			PushPosition ();
//			current_token = Token.NONE;  // Doesn't work with auto semi-insertion - needs prev token history always
			int next_token;
			switch (xtoken ()) {
			case Token.LITERAL:
			case Token.TRUE:
			case Token.FALSE:
			case Token.NULL:
			case Token.THIS:
			case Token.NEW:
				next_token = Token.INTERR;
				break;
				
			case Token.SEMICOLON:
			case Token.COMMA:
			case Token.CLOSE_PARENS:
			case Token.OPEN_BRACKET:
			case Token.OP_GENERICS_GT:
			case Token.INTERR:
				next_token = Token.INTERR_NULLABLE;
				break;
				
			default:
				next_token = -1;
				break;
			}

			if (next_token == -1) {
				switch (xtoken ()) {
				case Token.COMMA:
				case Token.SEMICOLON:
				case Token.OPEN_BRACE:
				case Token.CLOSE_PARENS:
				case Token.IN:
					next_token = Token.INTERR_NULLABLE;
					break;
					
				case Token.COLON:
					next_token = Token.INTERR;
					break;							
					
				default:
					int ntoken;
					int interrs = 1;
					int colons = 0;
					int braces = 0;
					//
					// All shorcuts failed, do it hard way
					//
					while ((ntoken = xtoken ()) != Token.EOF) {
						if (ntoken == Token.OPEN_BRACE) {
							++braces;
							continue;
						}

						if (ntoken == Token.CLOSE_BRACE) {
							--braces;
							continue;
						}

						if (braces != 0)
							continue;

						if (ntoken == Token.SEMICOLON)
							break;
						
						if (ntoken == Token.COLON) {
							if (++colons == interrs)
								break;
							continue;
						}
						
						if (ntoken == Token.INTERR) {
							++interrs;
							continue;
						}
					}
					
					next_token = colons != interrs && braces == 0 ? Token.INTERR_NULLABLE : Token.INTERR;
					break;
				}
			}
			
			PopPosition ();
			return next_token;
		}

		bool decimal_digits (int c)
		{
			int d;
			bool seen_digits = false;
			
			if (c != -1){
				if (number_pos == MaxNumberLength)
					Error_NumericConstantTooLong ();
				number_builder [number_pos++] = (char) c;
			}
			
			//
			// We use peek_char2, because decimal_digits needs to do a 
			// 2-character look-ahead (5.ToString for example).
			//
			while ((d = peek_char2 ()) != -1){
				if (d >= '0' && d <= '9'){
					if (number_pos == MaxNumberLength)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = (char) d;
					get_char ();
					seen_digits = true;
				} else
					break;
			}
			
			return seen_digits;
		}

		static bool is_hex (int e)
		{
			return (e >= '0' && e <= '9') || (e >= 'A' && e <= 'F') || (e >= 'a' && e <= 'f');
		}

		static TypeCode real_type_suffix (int c)
		{
			switch (c){
			case 'F': case 'f':
				return TypeCode.Single;
			case 'D': case 'd':
				return TypeCode.Double;
			case 'M': case 'm':
				return TypeCode.Decimal;
			default:
				return TypeCode.Empty;
			}
		}

		ILiteralConstant integer_type_suffix (ulong ul, int c, Location loc)
		{
			bool is_unsigned = false;
			bool is_long = false;

			if (c != -1){
				bool scanning = true;
				do {
					switch (c){
					case 'U': case 'u':
						if (is_unsigned)
							scanning = false;
						is_unsigned = true;
						get_char ();
						break;

					case 'l':
						if (!is_unsigned){
							//
							// if we have not seen anything in between
							// report this error
							//
							Report.Warning (78, 4, Location, "The 'l' suffix is easily confused with the digit '1' (use 'L' for clarity)");
						}

						goto case 'L';

					case 'L': 
						if (is_long)
							scanning = false;
						is_long = true;
						get_char ();
						break;
						
					default:
						scanning = false;
						break;
					}
					c = peek_char ();
				} while (scanning);
			}

			if (is_long && is_unsigned){
				return new ULongLiteral (context.BuiltinTypes, ul, loc);
			}
			
			if (is_unsigned){
				// uint if possible, or ulong else.

				if ((ul & 0xffffffff00000000) == 0)
					return new UIntLiteral (context.BuiltinTypes, (uint) ul, loc);
				else
					return new ULongLiteral (context.BuiltinTypes, ul, loc);
			} else if (is_long){
				// long if possible, ulong otherwise
				if ((ul & 0x8000000000000000) != 0)
					return new ULongLiteral (context.BuiltinTypes, ul, loc);
				else
					return new LongLiteral (context.BuiltinTypes, (long) ul, loc);
			} else {
				// int, uint, long or ulong in that order
				if ((ul & 0xffffffff00000000) == 0){
					uint ui = (uint) ul;
					
					if ((ui & 0x80000000) != 0)
						return new UIntLiteral (context.BuiltinTypes, ui, loc);
					else
						return new IntLiteral (context.BuiltinTypes, (int) ui, loc);
				} else {
					if ((ul & 0x8000000000000000) != 0)
						return new ULongLiteral (context.BuiltinTypes, ul, loc);
					else
						return new LongLiteral (context.BuiltinTypes, (long) ul, loc);
				}
			}
		}
				
		//
		// given `c' as the next char in the input decide whether
		// we need to convert to a special type, and then choose
		// the best representation for the integer
		//
		ILiteralConstant adjust_int (int c, Location loc)
		{
			try {
				if (number_pos > 9){
					ulong ul = (uint) (number_builder [0] - '0');

					for (int i = 1; i < number_pos; i++){
						ul = checked ((ul * 10) + ((uint)(number_builder [i] - '0')));
					}

					return integer_type_suffix (ul, c, loc);
				} else {
					uint ui = (uint) (number_builder [0] - '0');

					for (int i = 1; i < number_pos; i++){
						ui = checked ((ui * 10) + ((uint)(number_builder [i] - '0')));
					}

					return integer_type_suffix (ui, c, loc);
				}
			} catch (OverflowException) {
				Error_NumericConstantTooLong ();
				return new IntLiteral (context.BuiltinTypes, 0, loc);
			}
			catch (FormatException) {
				Report.Error (1013, Location, "Invalid number");
				return new IntLiteral (context.BuiltinTypes, 0, loc);
			}
		}
		
		ILiteralConstant adjust_real (TypeCode t, Location loc)
		{
			string s = new string (number_builder, 0, number_pos);
			const string error_details = "Floating-point constant is outside the range of type `{0}'";

			switch (t){
			case TypeCode.Decimal:
				try {
					return new DecimalLiteral (context.BuiltinTypes, decimal.Parse (s, styles, csharp_format_info), loc);
				} catch (OverflowException) {
					Report.Error (594, Location, error_details, "decimal");
					return new DecimalLiteral (context.BuiltinTypes, 0, loc);
				}
			case TypeCode.Single:
				try {
					return new FloatLiteral (context.BuiltinTypes, float.Parse (s, styles, csharp_format_info), loc);
				} catch (OverflowException) {
					Report.Error (594, Location, error_details, "float");
					return new FloatLiteral (context.BuiltinTypes, 0, loc);
				}
			default:
				try {
					return new DoubleLiteral (context.BuiltinTypes, double.Parse (s, styles, csharp_format_info), loc);
				} catch (OverflowException) {
					Report.Error (594, loc, error_details, "double");
					return new DoubleLiteral (context.BuiltinTypes, 0, loc);
				}
			}
		}

		ILiteralConstant handle_hex (Location loc)
		{
			int d;
			ulong ul;
			
			get_char ();
			while ((d = peek_char ()) != -1){
				if (is_hex (d)){
					number_builder [number_pos++] = (char) d;
					get_char ();
				} else
					break;
			}
			
			string s = new String (number_builder, 0, number_pos);

			try {
				if (number_pos <= 8)
					ul = System.UInt32.Parse (s, NumberStyles.HexNumber);
				else
					ul = System.UInt64.Parse (s, NumberStyles.HexNumber);

				return integer_type_suffix (ul, peek_char (), loc);
			} catch (OverflowException){
				Error_NumericConstantTooLong ();
				return new IntLiteral (context.BuiltinTypes, 0, loc);
			}
			catch (FormatException) {
				Report.Error (1013, Location, "Invalid number");
				return new IntLiteral (context.BuiltinTypes, 0, loc);
			}
		}

		//
		// Invoked if we know we have .digits or digits
		//
		int is_number (int c)
		{
			ILiteralConstant res;

#if FULL_AST
			int read_start = reader.Position - 1;
			if (c == '.') {
				//
				// Caller did peek_char
				//
				--read_start;
			}
#endif
			number_pos = 0;
			var loc = Location;

			if (c >= '0' && c <= '9'){
				if (c == '0'){
					int peek = peek_char ();

					if (peek == 'x' || peek == 'X') {
						val = res = handle_hex (loc);
#if FULL_AST
						res.ParsedValue = reader.ReadChars (read_start, reader.Position - 1);
#endif

						return Token.LITERAL;
					}
				}
				decimal_digits (c);
				c = get_char ();
			}

			//
			// We need to handle the case of
			// "1.1" vs "1.string" (LITERAL_FLOAT vs NUMBER DOT IDENTIFIER)
			//
			bool is_real = false;
			if (c == '.'){
				if (decimal_digits ('.')){
					is_real = true;
					c = get_char ();
				} else {
					putback ('.');
					number_pos--;
					val = res = adjust_int (-1, loc);

#if FULL_AST
					res.ParsedValue = reader.ReadChars (read_start, reader.Position - 1);
#endif
					return Token.LITERAL;
				}
			}
			
			if (c == 'e' || c == 'E'){
				is_real = true;
				if (number_pos == MaxNumberLength)
					Error_NumericConstantTooLong ();
				number_builder [number_pos++] = (char) c;
				c = get_char ();
				
				if (c == '+'){
					if (number_pos == MaxNumberLength)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = '+';
					c = -1;
				} else if (c == '-') {
					if (number_pos == MaxNumberLength)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = '-';
					c = -1;
				} else {
					if (number_pos == MaxNumberLength)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = '+';
				}
					
				decimal_digits (c);
				c = get_char ();
			}

			var type = real_type_suffix (c);
			if (type == TypeCode.Empty && !is_real) {
				putback (c);
				res = adjust_int (c, loc);
			} else {
				is_real = true;

				if (type == TypeCode.Empty) {
					putback (c);
				}

				res = adjust_real (type, loc);
			}

			val = res;

#if FULL_AST
			var chars = reader.ReadChars (read_start, reader.Position - (type == TypeCode.Empty && c > 0 ? 1 : 0));
			if (chars[chars.Length - 1] == '\r')
				Array.Resize (ref chars, chars.Length - 1);
			res.ParsedValue = chars;
#endif

			return Token.LITERAL;
		}

		//
		// Accepts exactly count (4 or 8) hex, no more no less
		//
		int getHex (int count, out int surrogate, out bool error)
		{
			int i;
			int total = 0;
			int c;
			int top = count != -1 ? count : 4;
			
			get_char ();
			error = false;
			surrogate = 0;
			for (i = 0; i < top; i++){
				c = get_char ();

				if (c >= '0' && c <= '9')
					c = (int) c - (int) '0';
				else if (c >= 'A' && c <= 'F')
					c = (int) c - (int) 'A' + 10;
				else if (c >= 'a' && c <= 'f')
					c = (int) c - (int) 'a' + 10;
				else {
					error = true;
					return 0;
				}
				
				total = (total * 16) + c;
				if (count == -1){
					int p = peek_char ();
					if (p == -1)
						break;
					if (!is_hex ((char)p))
						break;
				}
			}

			if (top == 8) {
				if (total > 0x0010FFFF) {
					error = true;
					return 0;
				}

				if (total >= 0x00010000) {
					surrogate = ((total - 0x00010000) % 0x0400 + 0xDC00);					
					total = ((total - 0x00010000) / 0x0400 + 0xD800);
				}
			}

			return total;
		}

		int escape (int c, out int surrogate)
		{
			bool error;
			int d;
			int v;

			d = peek_char ();
			if (c != '\\') {
				surrogate = 0;
				return c;
			}
			
			switch (d){
			case 'a':
				v = '\a'; break;
			case 'b':
				v = '\b'; break;
			case 'n':
				v = '\n'; break;
			case 't':
				v = '\t'; break;
			case 'v':
				v = '\v'; break;
			case 'r':
				v = '\r'; break;
			case '\\':
				v = '\\'; break;
			case '/':
				v = '/'; break;
			case 'f':
				v = '\f'; break;
			case '0':
				v = 0; break;
			case '"':
				v = '"'; break;
			case '\'':
				v = '\''; break;
			case 'x':
				v = getHex (-1, out surrogate, out error);
				if (error)
					goto default;
				return v;
			case 'u':
			case 'U':
				return EscapeUnicode (d, out surrogate);
			default:
				surrogate = 0;
				Report.Error (1009, Location, "Unrecognized escape sequence `\\{0}'", ((char)d).ToString ());
				return d;
			}

			get_char ();
			surrogate = 0;
			return v;
		}

		int EscapeUnicode (int ch, out int surrogate)
		{
			bool error;
			if (ch == 'U') {
				ch = getHex (8, out surrogate, out error);
			} else {
				ch = getHex (4, out surrogate, out error);
			}

			if (error)
				Report.Error (1009, Location, "Unrecognized escape sequence");

			return ch;
		}

		int get_char ()
		{
			int x;
			if (putback_char != -1) {
				x = putback_char;
				putback_char = -1;
			} else {
				x = reader.Read ();
			}
			
			if (x <= 13) {
				if (x == '\r') {
					if (peek_char () == '\n') {
						putback_char = -1;
					}

					x = '\n';
					advance_line ();
				} else if (x == '\n') {
					advance_line ();
				} else {
					col++;
				}
			} else if (x >= UnicodeLS && x <= UnicodePS) {
				advance_line ();
			} else {
				col++;
			}

			return x;
		}

		void advance_line ()
		{
			line++;
			ref_line++;
			previous_col = col;
			col = 0;
		}

		int peek_char ()
		{
			if (putback_char == -1)
				putback_char = reader.Read ();
			return putback_char;
		}

		int peek_char2 ()
		{
			if (putback_char != -1)
				return putback_char;
			return reader.Peek ();
		}
		
		public void putback (int c)
		{
			if (putback_char != -1) {
				throw new InternalErrorException (string.Format ("Secondary putback [{0}] putting back [{1}] is not allowed", (char)putback_char, (char) c), Location);
			}

			if (c == '\n' || col == 0 || (c >= UnicodeLS && c <= UnicodePS)) {
				// It won't happen though.
				line--;
				ref_line--;
				col = previous_col;
			}
			else
				col--;
			putback_char = c;
		}

		public bool advance ()
		{
			return peek_char () != -1 || CompleteOnEOF;
		}

		public Object Value {
			get {
				return val;
			}
		}

		public Object value ()
		{
			return val;
		}

		public int token ()
		{
			prev_token = current_token;
			prev_token_line = current_token_line;
			
			current_token = xtoken (true);
			current_token_line = line;

			return current_token;
		}

		public void token_putback (int token)
		{
			if (putback_token != -1)
				throw new Exception("Can't put back token twice.'");
			putback_token = token;
		}

		int TokenizePreprocessorIdentifier (out int c)
		{
			// skip over white space
			do {
				c = get_char ();
			} while (c == ' ' || c == '\t');


			int pos = 0;
			while (c != -1 && c >= 'a' && c <= 'z') {
				id_builder[pos++] = (char) c;
				c = get_char ();
				if (c == '\\') {
					int peek = peek_char ();
					if (peek == 'U' || peek == 'u') {
						int surrogate;
						c = EscapeUnicode (c, out surrogate);
						if (surrogate != 0) {
							if (is_identifier_part_character ((char) c)) {
								id_builder[pos++] = (char) c;
							}
							c = surrogate;
						}
					}
				}
			}

			return pos;
		}

		PreprocessorDirective get_cmd_arg (out string arg)
		{
			int c;		

			tokens_seen = false;
			arg = "";

			var cmd = GetPreprocessorDirective (id_builder, TokenizePreprocessorIdentifier (out c));

			if ((cmd & PreprocessorDirective.CustomArgumentsParsing) != 0)
				return cmd;

			// skip over white space
			while (c == ' ' || c == '\t')
				c = get_char ();

			int has_identifier_argument = (int)(cmd & PreprocessorDirective.RequiresArgument);
			int pos = 0;

			while (c != -1 && c != '\n' && c != UnicodeLS && c != UnicodePS) {
				if (c == '\\' && has_identifier_argument >= 0) {
					if (has_identifier_argument != 0) {
						has_identifier_argument = 1;

						int peek = peek_char ();
						if (peek == 'U' || peek == 'u') {
							int surrogate;
							c = EscapeUnicode (c, out surrogate);
							if (surrogate != 0) {
								if (is_identifier_part_character ((char) c)) {
									if (pos == value_builder.Length)
										Array.Resize (ref value_builder, pos * 2);

									value_builder[pos++] = (char) c;
								}
								c = surrogate;
							}
						}
					} else {
						has_identifier_argument = -1;
					}
				} else if (c == '/' && peek_char () == '/') {
					//
					// Eat single-line comments
					//
					get_char ();
					ReadToEndOfLine ();
					break;
				}

				if (pos == value_builder.Length)
					Array.Resize (ref value_builder, pos * 2);

				value_builder[pos++] = (char) c;
				c = get_char ();
			}

			if (pos != 0) {
				if (pos > MaxIdentifierLength)
					arg = new string (value_builder, 0, pos);
				else
					arg = InternIdentifier (value_builder, pos);

				// Eat any trailing whitespaces
				arg = arg.Trim (simple_whitespaces);
			}

			return cmd;
		}

		//
		// Handles the #line directive
		//
		bool PreProcessLine ()
		{
			Location loc = Location;

			int c;

			int length = TokenizePreprocessorIdentifier (out c);
			if (length == line_default.Length) {
				if (!IsTokenIdentifierEqual (line_default))
					return false;

				current_source = source_file.SourceFile;
				if (!hidden_block_start.IsNull) {
					current_source.RegisterHiddenScope (hidden_block_start, loc);
					hidden_block_start = Location.Null;
				}

				ref_line = line;
				return true;
			}

			if (length == line_hidden.Length) {
				if (!IsTokenIdentifierEqual (line_hidden))
					return false;

				if (hidden_block_start.IsNull)
					hidden_block_start = loc;

				return true;
			}

			if (length != 0 || c < '0' || c > '9') {
				//
				// Eat any remaining characters to continue parsing on next line
				//
				ReadToEndOfLine ();
				return false;
			}

			int new_line = TokenizeNumber (c);
			if (new_line < 1) {
				//
				// Eat any remaining characters to continue parsing on next line
				//
				ReadToEndOfLine ();
				return new_line != 0;
			}

			c = get_char ();
			if (c == ' ') {
				// skip over white space
				do {
					c = get_char ();
				} while (c == ' ' || c == '\t');
			} else if (c == '"') {
				c = 0;
			}

			if (c != '\n' && c != '/' && c != '"' && c != UnicodeLS && c != UnicodePS) {
				//
				// Eat any remaining characters to continue parsing on next line
				//
				ReadToEndOfLine ();

				Report.Error (1578, loc, "Filename, single-line comment or end-of-line expected");
				return true;
			}

			string new_file_name = null;
			if (c == '"') {
				new_file_name = TokenizeFileName (ref c);

				// skip over white space
				while (c == ' ' || c == '\t') {
					c = get_char ();
				}
			}

			if (c == '\n' || c == UnicodeLS || c == UnicodePS) {

			} else if (c == '/') {
				ReadSingleLineComment ();
			} else {
				//
				// Eat any remaining characters to continue parsing on next line
				//
				ReadToEndOfLine ();

				Error_EndLineExpected ();
				return true;
			}

			if (new_file_name != null) {
				current_source = context.LookupFile (source_file, new_file_name);
				source_file.AddIncludeFile (current_source);
			}

			if (!hidden_block_start.IsNull) {
				current_source.RegisterHiddenScope (hidden_block_start, loc);
				hidden_block_start = Location.Null;
			}

			ref_line = new_line;
			return true;
		}

		//
		// Handles #define and #undef
		//
		void PreProcessDefinition (bool is_define, string ident, bool caller_is_taking)
		{
			if (ident.Length == 0 || ident == "true" || ident == "false"){
				Report.Error (1001, Location, "Missing identifier to pre-processor directive");
				return;
			}

			if (ident.IndexOfAny (simple_whitespaces) != -1){
				Error_EndLineExpected ();
				return;
			}

			if (!is_identifier_start_character (ident [0]))
				Report.Error (1001, Location, "Identifier expected: {0}", ident);
			
			foreach (char c in ident.Substring (1)){
				if (!is_identifier_part_character (c)){
					Report.Error (1001, Location, "Identifier expected: {0}",  ident);
					return;
				}
			}

			if (!caller_is_taking)
				return;

			if (is_define) {
				//
				// #define ident
				//
				if (context.Settings.IsConditionalSymbolDefined (ident))
					return;

				source_file.AddDefine (ident);
			} else {
				//
				// #undef ident
				//
				source_file.AddUndefine (ident);
			}
		}

		byte read_hex (out bool error)
		{
			int total;
			int c = get_char ();

			if ((c >= '0') && (c <= '9'))
				total = (int) c - (int) '0';
			else if ((c >= 'A') && (c <= 'F'))
				total = (int) c - (int) 'A' + 10;
			else if ((c >= 'a') && (c <= 'f'))
				total = (int) c - (int) 'a' + 10;
			else {
				error = true;
				return 0;
			}

			total *= 16;
			c = get_char ();

			if ((c >= '0') && (c <= '9'))
				total += (int) c - (int) '0';
			else if ((c >= 'A') && (c <= 'F'))
				total += (int) c - (int) 'A' + 10;
			else if ((c >= 'a') && (c <= 'f'))
				total += (int) c - (int) 'a' + 10;
			else {
				error = true;
				return 0;
			}

			error = false;
			return (byte) total;
		}

		//
		// Parses #pragma checksum
		//
		bool ParsePragmaChecksum ()
		{
			//
			// The syntax is ` "foo.txt" "{guid}" "hash"'
			//
			// guid is predefined hash algorithm guid {406ea660-64cf-4c82-b6f0-42d48172a799} for md5
			//
			int c = get_char ();

			if (c != '"')
				return false;

			string file_name = TokenizeFileName (ref c);

			// TODO: Any white-spaces count
			if (c != ' ')
				return false;

			SourceFile file = context.LookupFile (source_file, file_name);

			if (get_char () != '"' || get_char () != '{')
				return false;

			bool error;
			byte[] guid_bytes = new byte [16];
			int i = 0;

			for (; i < 4; i++) {
				guid_bytes [i] = read_hex (out error);
				if (error)
					return false;
			}

			if (get_char () != '-')
				return false;

			for (; i < 10; i++) {
				guid_bytes [i] = read_hex (out error);
				if (error)
					return false;

				guid_bytes [i++] = read_hex (out error);
				if (error)
					return false;

				if (get_char () != '-')
					return false;
			}

			for (; i < 16; i++) {
				guid_bytes [i] = read_hex (out error);
				if (error)
					return false;
			}

			if (get_char () != '}' || get_char () != '"')
				return false;

			// TODO: Any white-spaces count
			c = get_char ();
			if (c != ' ')
				return false;

			if (get_char () != '"')
				return false;

			// Any length of checksum
			List<byte> checksum_bytes = new List<byte> (16);

			var checksum_location = Location;
			c = peek_char ();
			while (c != '"' && c != -1) {
				checksum_bytes.Add (read_hex (out error));
				if (error)
					return false;

				c = peek_char ();
			}

			if (c == '/') {
				ReadSingleLineComment ();
			} else if (get_char () != '"') {
				return false;
			}

			if (context.Settings.GenerateDebugInfo) {
				var chsum = checksum_bytes.ToArray ();

				if (file.HasChecksum) {
					if (!ArrayComparer.IsEqual (file.Checksum, chsum)) {
						// TODO: Report.SymbolRelatedToPreviousError
						Report.Warning (1697, 1, checksum_location, "Different checksum values specified for file `{0}'", file.Name);
					}
				}

				file.SetChecksum (guid_bytes, chsum);
			current_source.AutoGenerated = true;
			}

			return true;
		}

		bool IsTokenIdentifierEqual (char[] identifier)
		{
			for (int i = 0; i < identifier.Length; ++i) {
				if (identifier[i] != id_builder[i])
					return false;
			}

			return true;
		}

		int TokenizeNumber (int value)
		{
			number_pos = 0;

			decimal_digits (value);
			uint ui = (uint) (number_builder[0] - '0');

			try {
				for (int i = 1; i < number_pos; i++) {
					ui = checked ((ui * 10) + ((uint) (number_builder[i] - '0')));
				}

				return (int) ui;
			} catch (OverflowException) {
				Error_NumericConstantTooLong ();
				return -1;
			}
		}

		string TokenizeFileName (ref int c)
		{
			var string_builder = new StringBuilder ();
			while (c != -1 && c != '\n' && c != UnicodeLS && c != UnicodePS) {
				c = get_char ();
				if (c == '"') {
					c = get_char ();
					break;
				}

				string_builder.Append ((char) c);
			}

			if (string_builder.Length == 0) {
				Report.Warning (1709, 1, Location, "Filename specified for preprocessor directive is empty");
			}

		
			return string_builder.ToString ();
		}

		int TokenizePragmaNumber (ref int c)
		{
			number_pos = 0;

			int number;

			if (c >= '0' && c <= '9') {
				number = TokenizeNumber (c);

				c = get_char ();

				// skip over white space
				while (c == ' ' || c == '\t')
					c = get_char ();

				if (c == ',') {
					c = get_char ();
				}

				// skip over white space
				while (c == ' ' || c == '\t')
					c = get_char ();
			} else {
				number = -1;
				if (c == '/') {
					ReadSingleLineComment ();
				} else {
					Report.Warning (1692, 1, Location, "Invalid number");

					// Read everything till the end of the line or file
					ReadToEndOfLine ();
				}
			}

			return number;
		}

		void ReadToEndOfLine ()
		{
			int c;
			do {
				c = get_char ();
			} while (c != -1 && c != '\n' && c != UnicodeLS && c != UnicodePS);
		}

		void ReadSingleLineComment ()
		{
			if (peek_char () != '/')
				Report.Warning (1696, 1, Location, "Single-line comment or end-of-line expected");

			// Read everything till the end of the line or file
			ReadToEndOfLine ();
		}

		/// <summary>
		/// Handles #pragma directive
		/// </summary>
		void ParsePragmaDirective ()
		{
			int c;
			int length = TokenizePreprocessorIdentifier (out c);
			if (length == pragma_warning.Length && IsTokenIdentifierEqual (pragma_warning)) {
				length = TokenizePreprocessorIdentifier (out c);

				//
				// #pragma warning disable
				// #pragma warning restore
				//
				if (length == pragma_warning_disable.Length) {
					bool disable = IsTokenIdentifierEqual (pragma_warning_disable);
					if (disable || IsTokenIdentifierEqual (pragma_warning_restore)) {
						// skip over white space
						while (c == ' ' || c == '\t')
							c = get_char ();

						var loc = Location;

						if (c == '\n' || c == '/' || c == UnicodeLS || c == UnicodePS) {
							if (c == '/')
								ReadSingleLineComment ();

							//
							// Disable/Restore all warnings
							//
							if (disable) {
								Report.RegisterWarningRegion (loc).WarningDisable (loc.Row);
							} else {
								Report.RegisterWarningRegion (loc).WarningEnable (loc.Row);
							}
						} else {
							//
							// Disable/Restore a warning or group of warnings
							//
							int code;
							do {
								code = TokenizePragmaNumber (ref c);
								if (code > 0) {
									if (disable) {
										Report.RegisterWarningRegion (loc).WarningDisable (loc, code, context.Report);
									} else {
										Report.RegisterWarningRegion (loc).WarningEnable (loc, code, context);
									}
								}
							} while (code >= 0 && c != '\n' && c != -1 && c != UnicodeLS && c != UnicodePS);
						}

						return;
					}
				}

				Report.Warning (1634, 1, Location, "Expected disable or restore");

				// Eat any remaining characters on the line
				ReadToEndOfLine ();

				return;
			}

			//
			// #pragma checksum
			//
			if (length == pragma_checksum.Length && IsTokenIdentifierEqual (pragma_checksum)) {
				if (c != ' ' || !ParsePragmaChecksum ()) {
					Report.Warning (1695, 1, Location,
						"Invalid #pragma checksum syntax. Expected \"filename\" \"{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}\" \"XXXX...\"");
				}

				return;
			}

			Report.Warning (1633, 1, Location, "Unrecognized #pragma directive");
		}

		bool eval_val (string s)
		{
			if (s == "true")
				return true;
			if (s == "false")
				return false;

			return source_file.IsConditionalDefined (s);
		}

		bool pp_primary (ref string s)
		{
			s = s.Trim ();
			int len = s.Length;

			if (len > 0){
				char c = s [0];
				
				if (c == '('){
					s = s.Substring (1);
					bool val = pp_expr (ref s, false);
					if (s.Length > 0 && s [0] == ')'){
						s = s.Substring (1);
						return val;
					}
					Error_InvalidDirective ();
					return false;
				}
				
				if (is_identifier_start_character (c)){
					int j = 1;

					while (j < len){
						c = s [j];
						
						if (is_identifier_part_character (c)){
							j++;
							continue;
						}
						bool v = eval_val (s.Substring (0, j));
						s = s.Substring (j);
						return v;
					}
					bool vv = eval_val (s);
					s = "";
					return vv;
				}
			}
			Error_InvalidDirective ();
			return false;
		}
		
		bool pp_unary (ref string s)
		{
			s = s.Trim ();
			int len = s.Length;

			if (len > 0){
				if (s [0] == '!'){
					if (len > 1 && s [1] == '='){
						Error_InvalidDirective ();
						return false;
					}
					s = s.Substring (1);
					return ! pp_primary (ref s);
				} else
					return pp_primary (ref s);
			} else {
				Error_InvalidDirective ();
				return false;
			}
		}
		
		bool pp_eq (ref string s)
		{
			bool va = pp_unary (ref s);

			s = s.Trim ();
			int len = s.Length;
			if (len > 0){
				if (s [0] == '='){
					if (len > 2 && s [1] == '='){
						s = s.Substring (2);
						return va == pp_unary (ref s);
					} else {
						Error_InvalidDirective ();
						return false;
					}
				} else if (s [0] == '!' && len > 1 && s [1] == '='){
					s = s.Substring (2);

					return va != pp_unary (ref s);

				} 
			}

			return va;
				
		}
		
		bool pp_and (ref string s)
		{
			bool va = pp_eq (ref s);

			s = s.Trim ();
			int len = s.Length;
			if (len > 0){
				if (s [0] == '&'){
					if (len > 2 && s [1] == '&'){
						s = s.Substring (2);
						return (va & pp_and (ref s));
					} else {
						Error_InvalidDirective ();
						return false;
					}
				} 
			}
			return va;
		}
		
		//
		// Evaluates an expression for `#if' or `#elif'
		//
		bool pp_expr (ref string s, bool isTerm)
		{
			bool va = pp_and (ref s);
			s = s.Trim ();
			int len = s.Length;
			if (len > 0){
				char c = s [0];
				
				if (c == '|'){
					if (len > 2 && s [1] == '|'){
						s = s.Substring (2);
						return va | pp_expr (ref s, isTerm);
					} else {
						Error_InvalidDirective ();
						return false;
					}
				}
				if (isTerm) {
					Error_EndLineExpected ();
					return false;
				}
			}
			
			return va;
		}

		bool eval (string s)
		{
			bool v = pp_expr (ref s, true);
			s = s.Trim ();
			if (s.Length != 0){
				return false;
			}

			return v;
		}

		void Error_NumericConstantTooLong ()
		{
			Report.Error (1021, Location, "Integral constant is too large");			
		}
		
		void Error_InvalidDirective ()
		{
			Report.Error (1517, Location, "Invalid preprocessor directive");
		}

		void Error_UnexpectedDirective (string extra)
		{
			Report.Error (
				1028, Location,
				"Unexpected processor directive ({0})", extra);
		}

		void Error_TokensSeen ()
		{
			Report.Error (1032, Location,
				"Cannot define or undefine preprocessor symbols after first token in file");
		}

		void Eror_WrongPreprocessorLocation ()
		{
			Report.Error (1040, Location,
				"Preprocessor directives must appear as the first non-whitespace character on a line");
		}

		void Error_EndLineExpected ()
		{
			Report.Error (1025, Location, "Single-line comment or end-of-line expected");
		}

		//
		// Raises a warning when tokenizer found documentation comment
		// on unexpected place
		//
		void WarningMisplacedComment (Location loc)
		{
			if (doc_state != XmlCommentState.Error) {
				doc_state = XmlCommentState.Error;
				Report.Warning (1587, 2, loc, "XML comment is not placed on a valid language element");
			}
		}
		
		//
		// if true, then the code continues processing the code
		// if false, the code stays in a loop until another directive is
		// reached.
		// When caller_is_taking is false we ignore all directives except the ones
		// which can help us to identify where the #if block ends
		bool ParsePreprocessingDirective (bool caller_is_taking)
		{
			string arg;
			bool region_directive = false;

			var directive = get_cmd_arg (out arg);

			//
			// The first group of pre-processing instructions is always processed
			//
			switch (directive) {
			case PreprocessorDirective.Region:
				region_directive = true;
				arg = "true";
				goto case PreprocessorDirective.If;

			case PreprocessorDirective.Endregion:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #region for this #endregion");
					return true;
				}
				int pop = ifstack.Pop ();
					
				if ((pop & REGION) == 0)
					Report.Error (1027, Location, "Expected `#endif' directive");
					
				return caller_is_taking;
				
			case PreprocessorDirective.If:
				if (ifstack == null)
					ifstack = new Stack<int> (2);

				int flags = region_directive ? REGION : 0;
				if (ifstack.Count == 0){
					flags |= PARENT_TAKING;
				} else {
					int state = ifstack.Peek ();
					if ((state & TAKING) != 0) {
						flags |= PARENT_TAKING;
					}
				}

				if (eval (arg) && caller_is_taking) {
					ifstack.Push (flags | TAKING);
					return true;
				}
				ifstack.Push (flags);
				return false;

			case PreprocessorDirective.Endif:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #if for this #endif");
					return true;
				} else {
					pop = ifstack.Pop ();
					
					if ((pop & REGION) != 0)
						Report.Error (1038, Location, "#endregion directive expected");
					
					if (arg.Length != 0) {
						Error_EndLineExpected ();
					}
					
					if (ifstack.Count == 0)
						return true;

					int state = ifstack.Peek ();
					return (state & TAKING) != 0;
				}

			case PreprocessorDirective.Elif:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #if for this #elif");
					return true;
				} else {
					int state = ifstack.Pop ();

					if ((state & REGION) != 0) {
						Report.Error (1038, Location, "#endregion directive expected");
						return true;
					}

					if ((state & ELSE_SEEN) != 0){
						Error_UnexpectedDirective ("#elif not valid after #else");
						return true;
					}

					if ((state & TAKING) != 0) {
						ifstack.Push (0);
						return false;
					}

					if (eval (arg) && ((state & PARENT_TAKING) != 0)){
						ifstack.Push (state | TAKING);
						return true;
					}

					ifstack.Push (state);
					return false;
				}

			case PreprocessorDirective.Else:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #if for this #else");
					return true;
				} else {
					int state = ifstack.Peek ();

					if ((state & REGION) != 0) {
						Report.Error (1038, Location, "#endregion directive expected");
						return true;
					}

					if ((state & ELSE_SEEN) != 0){
						Error_UnexpectedDirective ("#else within #else");
						return true;
					}

					ifstack.Pop ();

					if (arg.Length != 0) {
						Error_EndLineExpected ();
						return true;
					}

					bool ret = false;
					if ((state & PARENT_TAKING) != 0) {
						ret = (state & TAKING) == 0;
					
						if (ret)
							state |= TAKING;
						else
							state &= ~TAKING;
					}
	
					ifstack.Push (state | ELSE_SEEN);
					
					return ret;
				}
			case PreprocessorDirective.Define:
				if (any_token_seen){
					Error_TokensSeen ();
					return caller_is_taking;
				}
				PreProcessDefinition (true, arg, caller_is_taking);
				return caller_is_taking;

			case PreprocessorDirective.Undef:
				if (any_token_seen){
					Error_TokensSeen ();
					return caller_is_taking;
				}
				PreProcessDefinition (false, arg, caller_is_taking);
				return caller_is_taking;

			case PreprocessorDirective.Invalid:
				Report.Error (1024, Location, "Wrong preprocessor directive");
				return true;
			}

			//
			// These are only processed if we are in a `taking' block
			//
			if (!caller_is_taking)
				return false;
					
			switch (directive){
			case PreprocessorDirective.Error:
				Report.Error (1029, Location, "#error: '{0}'", arg);
				return true;

			case PreprocessorDirective.Warning:
				Report.Warning (1030, 1, Location, "#warning: `{0}'", arg);
				return true;

			case PreprocessorDirective.Pragma:
				if (context.Settings.Version == LanguageVersion.ISO_1) {
					Report.FeatureIsNotAvailable (context, Location, "#pragma");
				}

				ParsePragmaDirective ();
				return true;

			case PreprocessorDirective.Line:
				Location loc = Location;
				if (!PreProcessLine ())
					Report.Error (1576, loc, "The line number specified for #line directive is missing or invalid");

				return caller_is_taking;
			}

			throw new NotImplementedException (directive.ToString ());
		}

		private int consume_string (bool quoted, char quoteChar = '"')
		{
			int c;
			int pos = 0;
			Location start_location = Location;
			if (quoted)
				start_location = start_location - 1;

#if FULL_AST
			int reader_pos = reader.Position;
#endif

			while (true) {
				c = get_char ();
				if (c == quoteChar) {
					if (quoted && peek_char () == quoteChar) {
						if (pos == value_builder.Length)
							Array.Resize (ref value_builder, pos * 2);

						value_builder[pos++] = (char) c;
						get_char ();
						continue;
					}

					string s;
					if (pos == 0)
						s = string.Empty;
					else if (pos <= 4)
						s = InternIdentifier (value_builder, pos);
					else
						s = new string (value_builder, 0, pos);

					ILiteralConstant res = new StringLiteral (context.BuiltinTypes, s, start_location);
					val = res;
#if FULL_AST
					res.ParsedValue = quoted ?
						reader.ReadChars (reader_pos - 2, reader.Position - 1) :
						reader.ReadChars (reader_pos - 1, reader.Position);
#endif

					return Token.LITERAL;
				}

				if (c == '\n' || c == UnicodeLS || c == UnicodePS) {
					if (!quoted) {
						Report.Error (1010, Location, "Newline in constant");

						advance_line ();

						// Don't add \r to string literal
						if (pos > 1 && value_builder [pos - 1] == '\r')
							--pos;

						val = new StringLiteral (context.BuiltinTypes, new string (value_builder, 0, pos), start_location);
						return Token.LITERAL;
					}

					advance_line ();
				} else if (c == '\\' && !quoted) {
					++col;
					int surrogate;
					c = escape (c, out surrogate);
					if (c == -1)
						return Token.ERROR;
					if (surrogate != 0) {
						if (pos == value_builder.Length)
							Array.Resize (ref value_builder, pos * 2);

						value_builder[pos++] = (char) c;
						c = surrogate;
					}
				} else if (c == -1) {
					Report.Error (1039, Location, "Unterminated string literal");
					return Token.EOF;
				} else {
					++col;
				}

				if (pos == value_builder.Length)
					Array.Resize (ref value_builder, pos * 2);

				value_builder[pos++] = (char) c;
			}
		}

		private int consume_regex ()
		{
			int c;
			int pos = 0;
			Location start_location = Location;

#if FULL_AST
			int reader_pos = reader.Position;
#endif

			StringBuilder opt_builder = null;

			while (true) {
				c = get_char ();
				if (c == '\\') {
					if (pos == value_builder.Length)
						Array.Resize (ref value_builder, pos * 2);
					value_builder[pos++] = (char) c;
					c = get_char ();
					// c will be added automatically at the end of this block
				} else if (c == '/') {

					c = peek_char();
					while (c == 'g' || c == 'i' || c == 'm' || c == 's' || c == 'x') {
						if (opt_builder == null)
							opt_builder = new StringBuilder();
						opt_builder.Append((char) get_char ());
						c = peek_char ();
					}

					string s;
					if (pos == 0)
						s = string.Empty;
					else
						s = new string (value_builder, 0, pos);

					ILiteralConstant res = new RegexLiteral (context.BuiltinTypes, s, 
					                                         opt_builder != null ? opt_builder.ToString() : null, 
					                                         start_location);
					val = res;
#if FULL_AST
					res.ParsedValue = quoted ?
						reader.ReadChars (reader_pos - 2, reader.Position - 1) :
						reader.ReadChars (reader_pos - 1, reader.Position);
#endif

					return Token.LITERAL;
				}

				if (c == '\n') {
					Report.Error (7027, Location, "Newline in regex constant");
					val = new StringLiteral (context.BuiltinTypes, new string (value_builder, 0, pos), start_location);
					return Token.LITERAL;
				} /* else if (c == '\\') {
					c = get_char();
					if (c != -1) {
						if (pos == value_builder.Length)
							Array.Resize (ref value_builder, pos * 2);
						value_builder[pos++] = (char) c;
					}
				} */

				if (c == -1) {
					Report.Error (7028, Location, "Unterminated regex literal");
					return Token.EOF;
				}

				if (pos == value_builder.Length)
					Array.Resize (ref value_builder, pos * 2);

				value_builder[pos++] = (char) c;
			}
		}

		private int consume_xml ()
		{
			int c;
			int pos = 0;
			Location start_location = Location;
			
#if FULL_AST
			int reader_pos = reader.Position;
#endif
			
			if (pos == value_builder.Length)
				Array.Resize (ref value_builder, pos * 2);
			value_builder[pos++] = (char) '<';

			while (true) {

				c = get_char ();
				if (c == '>') {

					if (pos == value_builder.Length)
						Array.Resize (ref value_builder, pos * 2);
					value_builder[pos++] = (char) c;

					c = peek_char();
					while (c == ' ' || c == '\t') {
						c = get_char ();
						if (pos == value_builder.Length)
							Array.Resize (ref value_builder, pos * 2);
						value_builder[pos++] = (char) c;
					}

					// TODO: This is a pretty ghetto way to identify the end of the xml literal.  Probably will
					// work most of the time, but is not a general solution.  FIXME
					if (c == ';' || c == '.' || c == ',' || c == ')' || c == '}' || c == ']') {

						string s;
						if (pos == 0)
							s = string.Empty;
						else
							s = new string (value_builder, 0, pos);
						
						ILiteralConstant res = new XmlLiteral (context.BuiltinTypes, s, start_location);
						val = res;
#if FULL_AST
						res.ParsedValue = quoted ?
							reader.ReadChars (reader_pos - 2, reader.Position - 1) :
								reader.ReadChars (reader_pos - 1, reader.Position);
#endif
						
						return Token.LITERAL;
					}
				}
				
				if (c == -1) {
					Report.Error (7029, Location, "Unterminated xml literal");
					return Token.EOF;
				}
				
				if (pos == value_builder.Length)
					Array.Resize (ref value_builder, pos * 2);
				value_builder[pos++] = (char) c;
			}
		}

		private int consume_identifier (bool parse_token, int s)
		{
			int res = consume_identifier (parse_token, s, false);

			if (doc_state == XmlCommentState.Allowed)
				doc_state = XmlCommentState.NotAllowed;

			return res;
		}

		int consume_identifier (bool parse_token, int c, bool quoted) 
		{
			//
			// This method is very performance sensitive. It accounts
			// for approximately 25% of all parser time
			//

			int pos = 0;
			int column = col;
			if (quoted)
				--column;

			if (c == '\\') {
				int surrogate;
				c = escape (c, out surrogate);
				if (surrogate != 0) {
					id_builder [pos++] = (char) c;
					c = surrogate;
				}
			}

			id_builder [pos++] = (char) c;

			bool is_config_ident = false;

			try {
				while (true) {
					c = reader.Read ();

					if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (c >= '0' && c <= '9') || c == '$') {
						id_builder [pos++] = (char) c;
						continue;
					}

					if (parsing_block == 0 && c == ':' && !is_config_ident) {
						var colonPos = reader.Position;
						c = reader.Read ();
						if (c == ':') { 
							c = reader.Read ();
							if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '$') {
								is_config_ident = true;
								id_builder [pos++] = ':';
								id_builder [pos++] = ':';
								id_builder [pos++] = (char) c;
								continue;
							} 
						}
						if (!is_config_ident) {
							reader.Position = colonPos;
							c = ':';
						}
					}

					if (c < 0x80) {
						if (c == '\\') {
							int surrogate;
							c = escape (c, out surrogate);
							if (is_identifier_part_character ((char) c))
								id_builder[pos++] = (char) c;

							if (surrogate != 0) {
								c = surrogate;
							}

							continue;
						}
					} else if (Char.IsLetter ((char) c) || Char.GetUnicodeCategory ((char) c) == UnicodeCategory.ConnectorPunctuation) {
						id_builder [pos++] = (char) c;
						continue;
					}

					putback_char = c;
					break;
				}
			} catch (IndexOutOfRangeException) {
				Report.Error (645, Location, "Identifier too long (limit is 512 chars)");
				--pos;
				col += pos;
			}

			col += pos - 1;

			//
			// Optimization: avoids doing the keyword lookup
			// on uppercase letters
			//
			if (!quoted && !is_config_ident && id_builder [0] >= '_') {
				int keyword = GetKeyword (id_builder, pos);
				if (keyword != -1) {
					val = ltb.Create (keyword == Token.AWAIT ? "await" : null, current_source, ref_line, column);
					if (keyword == Token.ELSE && do_auto_semi_insertion(parse_token, line, -1, keyword)) 
						return Token.SEMICOLON;
					return keyword;
				}
			}

			string s = InternIdentifier (id_builder, pos);

			val = ltb.Create (s, current_source, ref_line, column);
			if (quoted && parsing_attribute_section)
				AddEscapedIdentifier (((LocatedToken) val).Location);

			return is_config_ident ? Token.IDENTIFIER_CONFIG : 
				((parsing_modifiers && !parsing_attribute_section) ? Token.IDENTIFIER_MODIFIER : Token.IDENTIFIER);
		}

		string InternIdentifier (char[] charBuffer, int length)
		{
			//
			// Keep identifiers in an array of hashtables to avoid needless
			// allocations
			//
			var identifiers_group = identifiers[length];
			string s;
			if (identifiers_group != null) {
				if (identifiers_group.TryGetValue (charBuffer, out s)) {
					return s;
				}
			} else {
				// TODO: this should be number of files dependant
				// corlib compilation peaks at 1000 and System.Core at 150
				int capacity = length > 20 ? 10 : 100;
				identifiers_group = new Dictionary<char[], string> (capacity, new IdentifiersComparer (length));
				identifiers[length] = identifiers_group;
			}

			char[] chars = new char[length];
			Array.Copy (charBuffer, chars, length);

			s = new string (charBuffer, 0, length);
			identifiers_group.Add (chars, s);
			return s;
		}
		
		public int xtoken (bool parse_token = false)
		{
			int d, c, next;

			// Allow next token to be pushed back if we insert semicolons
			if (putback_token != -1) {
				next = putback_token;
				putback_token = -1;
				return next;
			}

			// Decrement parse regex counter (allows regex literals to follow 1 token after 
			// symbols '=', ':', '(', '[', and ',')
			if (parse_regex_xml > 0)
				parse_regex_xml--;

			// Decrement parse colon counter (allows us to disambiguate ident:*=value from *= operator)
			if (parse_colon > 0)
				parse_colon--;

			// Decrement allow auto semi counter (allows us to allow semicolon insertion only after next x symbols)
			if (allow_auto_semi_after > 0)
				allow_auto_semi_after--;

			// Eat all tokens until we get to final end brace
			if (eat_block) {
				eat_block = false;
				eat_block_braces = 1;
				do {
					next = xtoken (parse_token);
					if (next == Token.OPEN_BRACE || next == Token.OPEN_BRACE_INIT) {
						eat_block_braces++;
					} else if (next == Token.CLOSE_BRACE) {
						eat_block_braces--;
					}
				} while (eat_block_braces > 0 && next != Token.EOF);
				return next;
			}

			// Whether we have seen comments on the current line
			bool comments_seen = false;
			while ((c = get_char ()) != -1) {
				switch (c) {
				case '\t':
					col = ((col - 1 + tab_size) / tab_size) * tab_size;
					continue;

				case ' ':
				case '\f':
				case '\v':
				case 0xa0:
				case 0:
				case 0xFEFF:	// Ignore BOM anywhere in the file
					continue;

/*				This is required for compatibility with .NET
				case 0xEF:
					if (peek_char () == 0xBB) {
						PushPosition ();
						get_char ();
						if (get_char () == 0xBF)
							continue;
						PopPosition ();
					}
					break;
*/
				case '\\':
					tokens_seen = true;
					return consume_identifier (parse_token, c);

				case '{':
					val = ltb.Create (current_source, ref_line, col);
					if (current_token == Token.OPEN_PARENS || 
					  current_token == Token.ASSIGN ||
					  current_token == Token.COMMA ||
					  current_token == Token.COLON ||
					  current_token == Token.OPEN_BRACKET ||
					  current_token == Token.OPEN_BRACKET_EXPR ||
					  current_token == Token.RETURN ||
					  current_token == Token.OP_OR ||
					  current_token == Token.LOGICAL_OR_ASSIGN ||
					  current_token == Token.INTERR ||
					  current_token == Token.INTERR_NULLABLE) {
						bool isInit = true;
						PushPosition();
						this.AutoSemiInsertion = false;
						next = token ();
						if (next != Token.CLOSE_BRACE) {
							if (next != Token.IDENTIFIER && next != Token.LITERAL) {
								isInit = false;
							} else {
								next = token ();
								if (next != Token.COLON) {
									isInit = false;
								}
							}
						}
						PopPosition();
						if (isInit) 
							return Token.OPEN_BRACE_INIT;
					}
					return Token.OPEN_BRACE;
				case '}':
					if (do_auto_semi_insertion (parse_token, line, c, -1))
						return Token.SEMICOLON;
					val = ltb.Create (current_source, ref_line, col);
					return Token.CLOSE_BRACE;
				case '[':
					// To block doccomment inside attribute declaration.
					if (doc_state == XmlCommentState.Allowed)
						doc_state = XmlCommentState.NotAllowed;

					val = ltb.Create (current_source, ref_line, col);

					parse_regex_xml = 2;  // regex literals may be included in array initializers.

					if (parsing_block == 0)
						return Token.OPEN_BRACKET;

					next = peek_char ();
					switch (next) {
					case ']':
					case ',':
						return Token.OPEN_BRACKET;

					case ' ':
					case '\f':
					case '\v':
					case '\r':
					case '\n':
					case UnicodeLS:
					case UnicodePS:
					case '/':
						next = peek_token ();
						if (next == Token.COMMA || next == Token.CLOSE_BRACKET)
							return Token.OPEN_BRACKET;

						return Token.OPEN_BRACKET_EXPR;
					default:
						return Token.OPEN_BRACKET_EXPR;
					}
				case ']':
					ltb.CreateOptional (current_source, ref_line, col, ref val);
					return Token.CLOSE_BRACKET;
				case '(':
					val = ltb.Create (current_source, ref_line, col);
					parse_regex_xml = 2; // regex literals may follow open parens (method param, expressions).
					//
					// An expression versions of parens can appear in block context only
					//
					if (parsing_block != 0) {
						
						//
						// Optmize most common case where we know that parens
						// is not special
						//
						switch (current_token) {
						case Token.IDENTIFIER:
						case Token.IF:
						case Token.FOR:
						case Token.FOR_EACH:
						case Token.TYPEOF:
						case Token.WHILE:
						case Token.USING:
						case Token.DEFAULT:
						case Token.DELEGATE:
						case Token.OP_GENERICS_GT:
							return Token.OPEN_PARENS;
						}

						// Optimize using peek
						int xx = peek_char ();
						switch (xx) {
						case '(':
						case '\'':
						case '"':
						case '0':
						case '1':
							return Token.OPEN_PARENS;
						}

						PushPosition ();
						d = TokenizeOpenParens ();
						PopPosition ();
						return d;
					}

					return Token.OPEN_PARENS;
				case ')':
//					d = peek_char ();
//					if (d == '.') {
//						get_char ();
//						return Token.CLOSE_PARENS_DOT;
//					}
					ltb.CreateOptional (current_source, ref_line, col, ref val);
					return Token.CLOSE_PARENS;
				case ',':
					ltb.CreateOptional (current_source, ref_line, col, ref val);
					parse_regex_xml = 2; // Regex literals may follow commas, (method param, initializer element)
					return Token.COMMA;
				case ';':
					ltb.CreateOptional (current_source, ref_line, col, ref val);
					return Token.SEMICOLON;
				case '~':
					val = ltb.Create (current_source, ref_line, col);
					return Token.TILDE;
				case '?':
					val = ltb.Create (current_source, ref_line, col);
					return TokenizePossibleNullableType ();

				case '<':
					val = ltb.Create (current_source, ref_line, col);
					d = peek_char ();

					if (d == '=') {
						get_char ();
						return Token.OP_LE;
					}

					if (d == '<') {
						get_char ();
						d = peek_char ();

						if (d == '=') {
							get_char ();
							return Token.OP_SHIFT_LEFT_ASSIGN;
						}
						return Token.OP_SHIFT_LEFT;
					}

					if (parse_regex_xml > 0 && char.IsLetter ((char)d)) {
						return consume_xml();
					}

					return Token.OP_LT;

				case '>':
					val = ltb.Create (current_source, ref_line, col);
					d = peek_char ();

					if (parsing_generic_less_than > 1 || (parsing_generic_less_than == 1 && d != '>')) {
						parsing_generic_less_than--;
						return Token.OP_GENERICS_GT;
					}

					if (d == '=') {
						get_char ();
						return Token.OP_GE;
					}

					if (d == '>') {
						get_char ();
						d = peek_char ();

						if (d == '=') {
							get_char ();
							return Token.OP_SHIFT_RIGHT_ASSIGN;
						}

						if (d == '>') {
							get_char ();
							d = peek_char ();

							if (d == '=') {
								get_char ();
								return Token.OP_USHIFT_RIGHT_ASSIGN;
							}

							return Token.OP_USHIFT_RIGHT;
						}
						return Token.OP_SHIFT_RIGHT;
					}

					return Token.OP_GT;

				case '+':
					val = ltb.Create (current_source, ref_line, col);
					d = peek_char ();
					if (d == '+') {
						d = Token.OP_INC;
					} else if (d == '=') {
						d = Token.OP_ADD_ASSIGN;
					} else {
						return Token.PLUS;
					}
					get_char ();
					return d;

				case '-':
					val = ltb.Create (current_source, ref_line, col);
					d = peek_char ();
					if (d == '-') {
						d = Token.OP_DEC;
					} else if (d == '=')
						d = Token.OP_SUB_ASSIGN;
					else if (d == '>')
						d = Token.OP_PTR;
					else {
						return Token.MINUS;
					}
					get_char ();
					return d;

				case '!':
					val = ltb.Create (current_source, ref_line, col);
					if (peek_char () == '=') {
						get_char ();
						if (peek_char () == '=') {
							get_char ();
							return Token.OP_STRICT_NE;
						}
						return Token.OP_NE;
					}
					return Token.BANG;

				case '=':
					val = ltb.Create (current_source, ref_line, col);
					d = peek_char ();
					if (d == '=') {
						parse_regex_xml = 2; // Regex literals may follow equality test operators.
						get_char ();
						d = peek_char ();
						if (d == '=') {
							get_char ();
							return Token.OP_STRICT_EQ;
						}
						return Token.OP_EQ;
					}
					if (d == '>' && parsing_playscript) {
						get_char ();
						return Token.ARROW;
					}

					parse_regex_xml = 2; // Regex literals may follow assignment op '='
					return Token.ASSIGN;

				case '&':
					val = ltb.Create (current_source, ref_line, col);
					d = peek_char ();
					if (d == '&') {
						get_char ();
						d = peek_char ();
						if (d == '=') {
							get_char ();
							return Token.LOGICAL_AND_ASSIGN;
						}
						return Token.OP_AND;
					}
					if (d == '=') {
						get_char ();
						return Token.OP_AND_ASSIGN;
					}
					return Token.BITWISE_AND;

				case '|':
					val = ltb.Create (current_source, ref_line, col);
					d = peek_char ();
					if (d == '|') {
						get_char ();
						d = peek_char ();
						if (d == '=') {
							get_char ();
							return Token.LOGICAL_OR_ASSIGN;
						}
						return Token.OP_OR;
					}
					if (d == '=') {
						get_char ();
						return Token.OP_OR_ASSIGN;
					}
					return Token.BITWISE_OR;

				case '*':
					val = ltb.Create (current_source, ref_line, col);
					if (peek_char () == '=' && parse_colon == 0) {
						get_char ();
						return Token.OP_MULT_ASSIGN;
					}
					return Token.STAR;

				case '/':
					d = peek_char ();
					if (d == '=') {
						val = ltb.Create (current_source, ref_line, col);
						get_char ();
						return Token.OP_DIV_ASSIGN;
					}

					// Handle double-slash comments.
					if (d == '/') {
						get_char ();
						if (doc_processing) {
							if (peek_char () == '/') {
								get_char ();
								// Don't allow ////.
								if ((d = peek_char ()) != '/') {
									if (doc_state == XmlCommentState.Allowed)
										handle_one_line_xml_comment ();
									else if (doc_state == XmlCommentState.NotAllowed)
										WarningMisplacedComment (Location - 3);
								}
							} else {
								if (xml_comment_buffer.Length > 0)
									doc_state = XmlCommentState.NotAllowed;
							}
						}

						while ((d = get_char ()) != -1 && d != '\n' && d != UnicodeLS && d != UnicodePS);

						if (d == '\n' || d == UnicodeLS || d == UnicodePS)
							putback (d);

						any_token_seen |= tokens_seen;
						tokens_seen = false;
						comments_seen = false;
						continue;
					} else if (d == '*') {
						get_char ();
						// Handle /*@asx conditional comment
						if (peek_char () == '@') {
							PushPosition();
							get_char ();
							if (peek_char() == 'a') {
								get_char ();
								if (peek_char () == 's') {
									get_char ();
									if (peek_char () == 'x') {
										get_char ();
										DiscardPosition();
										continue;
									}
								}
							}
							PopPosition();
						}
						bool docAppend = false;
						if (doc_processing && peek_char () == '*') {
							get_char ();
							// But when it is /**/, just do nothing.
							if (peek_char () == '/') {
								get_char ();
								continue;
							}
							if (doc_state == XmlCommentState.Allowed)
								docAppend = true;
							else if (doc_state == XmlCommentState.NotAllowed) {
								WarningMisplacedComment (Location - 2);
							}
						}

						int current_comment_start = 0;
						if (docAppend) {
							current_comment_start = xml_comment_buffer.Length;
							xml_comment_buffer.Append (Environment.NewLine);
						}

						while ((d = get_char ()) != -1) {
							if (d == '*' && peek_char () == '/') {
								get_char ();
								comments_seen = true;
								break;
							}
							if (docAppend)
								xml_comment_buffer.Append ((char)d);
							
							if (d == '\n' || d == UnicodeLS || d == UnicodePS){
								any_token_seen |= tokens_seen;
								tokens_seen = false;
								// 
								// Reset 'comments_seen' just to be consistent.
								// It doesn't matter either way, here.
								//
								comments_seen = false;
							}
						}
						if (!comments_seen)
							Report.Error (1035, Location, "End-of-file found, '*/' expected");

						if (docAppend)
							update_formatted_doc_comment (current_comment_start);
						continue;
					} else if (parse_regex_xml > 0) {
						// A regex literal may follow an '=', '==', '===' '(' ',' ':' or '['. 
						return consume_regex();
					}
					val = ltb.Create (current_source, ref_line, col);
					return Token.DIV;

				case '%':
					val = ltb.Create (current_source, ref_line, col);
					if (peek_char () == '=') {
						get_char ();
						return Token.OP_MOD_ASSIGN;
					}
					return Token.PERCENT;

				case '^':
					val = ltb.Create (current_source, ref_line, col);
					if (peek_char () == '=') {
						get_char ();
						return Token.OP_XOR_ASSIGN;
					}
					return Token.CARRET;

				case ':':
					val = ltb.Create (current_source, ref_line, col);
					if (peek_char () == ':') {
						get_char ();
						return Token.DOUBLE_COLON;
					}
					parse_regex_xml = 2;  // Regex literals may follow colons in object initializers.
					parse_colon = 2;  // Don't parse *= after a colon 
					return Token.COLON;

				case '0': case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8': case '9':
					tokens_seen = true;
					return is_number (c);

				case '\n': // white space
				case UnicodeLS:
				case UnicodePS:
					if (do_auto_semi_insertion (parse_token, line - 1, c, -1))
						return Token.SEMICOLON;
					any_token_seen |= tokens_seen;
					tokens_seen = false;
					comments_seen = false;
					continue;

				case '.':
					tokens_seen = true;
					d = peek_char ();
					if (d >= '0' && d <= '9') 
						return is_number (c);

					if (d == '@') {
						get_char ();
						return Token.DOT_AT;
					}

					if (d == '*') {
						get_char ();
						return Token.DOT_STAR;
					}

//					if (d == '(') {
//						get_char ();
//						return Token.DOT_OPEN_PARENS;
//					}

					if (d == '.') {
						get_char ();
						d = peek_char ();
						if (d == '.') {
							get_char ();
							return Token.DOTDOTDOT;
						}
						if (d == '@') {
							get_char ();
							return Token.DOTDOT_AT;
						}
						if (d == '*') {
							get_char ();
							return Token.DOTDOT_STAR;
						}
						return Token.DOTDOT;
					}

					ltb.CreateOptional (current_source, ref_line, col, ref val);
					if (d != '<') {
						return Token.DOT;
					}
					get_char ();
					parsing_generic_less_than++;
					int dim;
					PushPosition ();
					if (parse_generic_dimension (out dim)) {
						val = dim;
						DiscardPosition ();
						return Token.GENERIC_DIMENSION;
					}
					PopPosition ();
					return Token.OP_GENERICS_LT;
				
				case '#':
					if (tokens_seen || comments_seen) {
						Eror_WrongPreprocessorLocation ();
						return Token.ERROR;
					}
					
					if (ParsePreprocessingDirective (true))
						continue;

					bool directive_expected = false;
					while ((c = get_char ()) != -1) {
						if (col == 1) {
							directive_expected = true;
						} else if (!directive_expected) {
							// TODO: Implement comment support for disabled code and uncomment this code
//							if (c == '#') {
//								Eror_WrongPreprocessorLocation ();
//								return Token.ERROR;
//							}
							continue;
						}

						if (c == ' ' || c == '\t' || c == '\n' || c == '\f' || c == '\v' || c == UnicodeLS || c == UnicodePS)
							continue;

						if (c == '#') {
							if (ParsePreprocessingDirective (false))
								break;
						}
						directive_expected = false;
					}

					if (c != -1) {
						tokens_seen = false;
						continue;
					}

					return Token.EOF;
				
				case '"':
					return consume_string (false, '"');
				case '\'':
					return consume_string (false, '\'');

//				case '\'':
//					return TokenizeBackslash ();
				
				case '@':
					if (!parsing_playscript)
						return Token.OP_AT;

					c = get_char ();
					if (c == '"') {
						tokens_seen = true;
						return consume_string (true);
					}

					// Handle end @asx*/ conditional comment
					PushPosition();
					if (c == 'a') {
						if (peek_char () == 's') {
							get_char ();
							if (peek_char () == 'x') {
								get_char ();
								if (peek_char () == '*') {
									get_char ();
									if (peek_char () == '/') {
										get_char ();
										DiscardPosition();
										continue;
									}
								}
							}
						}
					}
					PopPosition();

					if (is_identifier_start_character (c)){
						return consume_identifier (parse_token, c, true);
					}

					Report.Error (1646, Location, "Keyword, identifier, or string expected after verbatim specifier: @");
					return Token.ERROR;

				case EvalStatementParserCharacter:
					return Token.EVAL_STATEMENT_PARSER;
				case EvalCompilationUnitParserCharacter:
					return Token.EVAL_COMPILATION_UNIT_PARSER;
				case EvalUsingDeclarationsParserCharacter:
					return Token.EVAL_USING_DECLARATIONS_UNIT_PARSER;
				case DocumentationXref:
					return Token.DOC_SEE;
				}

				if (is_identifier_start_character (c)) {
					tokens_seen = true;
					return consume_identifier (parse_token, c);
				}

				if (char.IsWhiteSpace ((char) c))
					continue;

				Report.Error (1056, Location, "Unexpected character `{0}'", ((char) c).ToString ());
			}

			if (CompleteOnEOF){
				if (generated)
					return Token.COMPLETE_COMPLETION;
				
				generated = true;
				return Token.GENERATE_COMPLETION;
			}
			

			return Token.EOF;
		}

//		int TokenizeBackslash ()
//		{
//#if FULL_AST
//			int read_start = reader.Position;
//#endif
//			Location start_location = Location;
//			int c = get_char ();
//			tokens_seen = true;
//			if (c == '\'') {
//				val = new CharLiteral (context.BuiltinTypes, (char) c, start_location);
//				Report.Error (1011, start_location, "Empty character literal");
//				return Token.LITERAL;
//			}
//
//			if (c == '\n' || c == UnicodeLS || c == UnicodePS) {
//				Report.Error (1010, start_location, "Newline in constant");
//				return Token.ERROR;
//			}
//
//			int d;
//			c = escape (c, out d);
//			if (c == -1)
//				return Token.ERROR;
//			if (d != 0)
//				throw new NotImplementedException ();
//
//			ILiteralConstant res = new CharLiteral (context.BuiltinTypes, (char) c, start_location);
//			val = res;
//			c = get_char ();
//
//			if (c != '\'') {
//				Report.Error (1012, start_location, "Too many characters in character literal");
//
//				// Try to recover, read until newline or next "'"
//				while ((c = get_char ()) != -1) {
//					if (c == '\n' || c == '\'')
//						break;
//				}
//			}
//
//#if FULL_AST
//			res.ParsedValue = reader.ReadChars (read_start - 1, reader.Position);
//#endif
//
//			return Token.LITERAL;
//		}


		//
		// Handles one line xml comment
		//
		private void handle_one_line_xml_comment ()
		{
			int c;
			while ((c = peek_char ()) == ' ')
				get_char (); // skip heading whitespaces.
			while ((c = peek_char ()) != -1 && c != '\n' && c != '\r') {
				xml_comment_buffer.Append ((char) get_char ());
			}
			if (c == '\r' || c == '\n')
				xml_comment_buffer.Append (Environment.NewLine);
		}

		//
		// Remove heading "*" in Javadoc-like xml documentation.
		//
		private void update_formatted_doc_comment (int current_comment_start)
		{
			int length = xml_comment_buffer.Length - current_comment_start;
			string [] lines = xml_comment_buffer.ToString (
				current_comment_start,
				length).Replace ("\r", "").Split ('\n');
			
			// The first line starts with /**, thus it is not target
			// for the format check.
			for (int i = 1; i < lines.Length; i++) {
				string s = lines [i];
				int idx = s.IndexOf ('*');
				string head = null;
				if (idx < 0) {
					if (i < lines.Length - 1)
						return;
					head = s;
				} else
					head = s.Substring (0, idx);
				foreach (char c in head)
					if (c != ' ')
						return;
				lines [i] = s.Substring (idx + 1);
			}
			xml_comment_buffer.Remove (current_comment_start, length);
			xml_comment_buffer.Insert (current_comment_start, String.Join (Environment.NewLine, lines));
		}

		//
		// Checks if there was incorrect doc comments and raise
		// warnings.
		//
		public void check_incorrect_doc_comment ()
		{
			if (xml_comment_buffer.Length > 0)
				WarningMisplacedComment (Location);
		}

		//
		// Consumes the saved xml comment lines (if any)
		// as for current target member or type.
		//
		public string consume_doc_comment ()
		{
			if (xml_comment_buffer.Length > 0) {
				string ret = xml_comment_buffer.ToString ();
				reset_doc_comment ();
				return ret;
			}
			return null;
		}

		Report Report {
			get { return context.Report; }
		}

		void reset_doc_comment ()
		{
			xml_comment_buffer.Length = 0;
		}

		bool do_auto_semi_insertion (bool parse_token, int line, int c, int t)
		{
			bool insert_semi = false;
			if (parse_token && prev_token_line == line && prev_token != Token.SEMICOLON && !parsing_playscript && allow_auto_semi && 
			    allow_auto_semi_after == 0 && allowed_auto_semi_tokens[prev_token]) {
				PushPosition ();
				int next = xtoken ();
				PopPosition ();
				if (!disallowed_next_auto_semi_tokens[next]) {
					if (c != -1)
						putback (c);
					else
						token_putback (t);
					warn_semi_inserted (Location);
					insert_semi = true;
				}
			}
			if (parse_token && has_temp_auto_semi_after_tokens)
				clear_temp_auto_semi_tokens ();
			return insert_semi;
		}

		void warn_semi_inserted (Location loc) 
		{
			Report.Warning (7093, 4, loc, "Semicolon automatically inserted on unterminated line.");
		}

		void clear_temp_auto_semi_tokens ()
		{
			var len = temp_auto_semi_after_tokens.Count;
			for (var i = 0; i < len; i++) {
				int token = temp_auto_semi_after_tokens[i];
				allowed_auto_semi_tokens.Set (token, false);
			}
			has_temp_auto_semi_after_tokens = false;
		}

		public void cleanup ()
		{
			if (ifstack != null && ifstack.Count >= 1) {
				int state = ifstack.Pop ();
				if ((state & REGION) != 0)
					Report.Error (1038, Location, "#endregion directive expected");
				else 
					Report.Error (1027, Location, "Expected `#endif' directive");
			}
		}
	}

	//
	// Indicates whether it accepts XML documentation or not.
	//
	public enum XmlCommentState {
		// comment is allowed in this state.
		Allowed,
		// comment is not allowed in this state.
		NotAllowed,
		// once comments appeared when it is NotAllowed, then the
		// state is changed to it, until the state is changed to
		// .Allowed.
		Error
	}
}

