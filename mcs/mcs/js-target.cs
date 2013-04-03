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
using System.IO;
using System.Text;

namespace Mono.CSharp.JavaScript
{
	public class JsEmitContext {

		public ModuleContainer Module;
		public JsEmitBuffer Buf;

		bool _forceExpression;
		List<bool> _forceExprStack = new List<bool>(128);

		private List<JsEmitBuffer> _stack = new List<JsEmitBuffer>();
		private Dictionary<string, JsEmitBuffer> _stash = new Dictionary<string, JsEmitBuffer>();

		private HashSet<string> _definedNamespaces = new HashSet<string>();

		public JsEmitContext(ModuleContainer module)
		{
			Module = module;
			Buf = new JsEmitBuffer();
			Buf.EmitContext = this;
		}

		public CompilerContext Compiler {
			get { return Module.Compiler; }
		}

		public Report Report {
			get { return Module.Compiler.Report; }
		}

		public bool CheckCanEmit(Location loc) {
//			if (loc.SourceFile != null && 
//			    (loc.SourceFile.FileType != SourceFileType.PlayScript || loc.SourceFile.PsExtended == true)) {
//				this.Report.Error (7071, loc,  "JavaScript code generation for C# or ASX types not supported.");
//				return false;
//			}
			return true;
		}

		// Forces statements to be emitted as expressions (no preceeding indent, no following ;\n)
		public bool ForceExpr {
			get { return _forceExpression; }
		}

		public void PushForceExpr (bool force) 
		{
			_forceExprStack.Add (_forceExpression);
			_forceExpression = force;
		}

		public void PopForceExpr ()
		{
			if (_forceExprStack.Count > 0) {
				_forceExpression = _forceExprStack [_forceExprStack.Count - 1];
				_forceExprStack.RemoveAt (_forceExprStack.Count - 1);
			}
		}

		public void Push()
		{
			var oldBuf = Buf;
			_stack.Add (Buf);
			Buf = new JsEmitBuffer();
			Buf.EmitContext = this;
			Buf.IndentLevel = oldBuf.IndentLevel; 
		}

		public JsEmitBuffer Pop()
		{
			var ret = Buf;
			Buf = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
			return ret;
		}

		public void Stash(string id)
		{
			var oldBuf = Buf;
			_stash[id] = Buf;
			Buf = new JsEmitBuffer();
			Buf.EmitContext = this;
			Buf.IndentLevel = oldBuf.IndentLevel; 
		}

		public void Restore(string id)
		{
			Buf = _stash[id];
		}

		public bool IsNamespaceDefined(string ns)
		{
			return _definedNamespaces.Contains(ns);
		}
		
		public void MarkNamespaceDefined(string ns)
		{
			_definedNamespaces.Add(ns);
		}
		
		public string MakeJsNamespaceName(string ns)
		{
			return ns.Replace ('.', '$');
		}

		public string MakeJsTypeName(string t)
		{
			return t.Replace ("<", "$").Replace (">", "$");
		}

		public string MakeJsFullTypeName(TypeSpec t)
		{
			return t.MemberDefinition.Namespace + "." + MakeJsTypeName(t.Name);
		}

		public int GetOperPrecendence(Expression e)
		{
			if (e is As || e is AsIn || e is Is) {
				return 6;
			}

			if (e is TypeOf || e is AsDelete) {
				return 2;
			}

			if (e is Conditional) {
				return 13;
			}

			if (e is Assign) {
				return 14;
			}

			if (e is UnaryMutator) {
				var um = e as UnaryMutator;
				if (um.UnaryMutatorMode == UnaryMutator.Mode.PreIncrement ||
				    um.UnaryMutatorMode == UnaryMutator.Mode.PreDecrement)
					return 2;
				else
					return 1;
			}

			if (e is Binary) {
				var op = (e as Binary).Oper;
				switch (op) {
				// Standard C# binary operators
				case Binary.Operator.Addition:		return 4;
				case Binary.Operator.Subtraction:	return 4;
				case Binary.Operator.Multiply:		return 3;
				case Binary.Operator.Division:		return 3;
				case Binary.Operator.Modulus:		return 3;
				case Binary.Operator.BitwiseAnd:	return 8;
				case Binary.Operator.BitwiseOr:		return 10;
				case Binary.Operator.ExclusiveOr:	return 9;
				case Binary.Operator.LogicalAnd:	return 11;
				case Binary.Operator.LogicalOr:		return 12;
				case Binary.Operator.LeftShift:		return 5;
				case Binary.Operator.RightShift:	return 5;
				case Binary.Operator.Equality:		return 7;
				case Binary.Operator.Inequality:	return 7;
				case Binary.Operator.GreaterThan:	return 6;
				case Binary.Operator.LessThan:		return 6;
				case Binary.Operator.GreaterThanOrEqual: return 6;
				case Binary.Operator.LessThanOrEqual: return 6;

				// PlayScript binary operators
				case Binary.Operator.AsURightShift: return 5;
				case Binary.Operator.AsRefEquality: return 7;
				}
			}

			if (e is Unary) {
				var op = (e as Unary).Oper;
				switch (op) {
					// Unary operators
				case Unary.Operator.LogicalNot:		return 2;		
				case Unary.Operator.OnesComplement:	return 2;
				case Unary.Operator.UnaryPlus:		return 2;
				case Unary.Operator.UnaryNegation:	return 2;
				case Unary.Operator.AddressOf:		return 2;
				}
			}

			return 0;
		}

		public bool NeedParens(Expression parent, Expression child) {
			return GetOperPrecendence(child) > GetOperPrecendence(parent);
		}

	}

	public class JsEmitBuffer {

		private const int MAX_INDENT = 40;

		public TextWriter Stream;
		public string CurIndent;
		public JsEmitContext EmitContext;
		public int Line;
		public int Col;

		private int _indentLevel;
		private Location _curLoc;

		private static string _indentStr = 
			"                                                                                                                                 ";

		private static char[] buf = new char[4096];

		private struct MapSeg {

			public int Line;
			public int Col;
			public int SrcFile;
			public int SrcLine;
			public int SrcCol;

			public MapSeg(int line, int col, int srcFile, int srcLine, int srcCol)
			{
				Line = line;
				Col = col;
				SrcFile = srcFile;
				SrcLine = srcLine;
				SrcCol = srcCol;
			}
		}

		private List<MapSeg> _srcMap = new List<MapSeg>();

		public JsEmitBuffer() 
		{
			Stream = new System.IO.StringWriter();
			IndentLevel = 0;
			CurIndent = "";
		}

		public int IndentLevel {
			set {
				_indentLevel = value;
			}
			get {
				return _indentLevel;
			}
		}

		public void Indent()
		{
			_indentLevel++;
		}

		public void Unindent()
		{
			if (_indentLevel > 0)
				_indentLevel--;
		}

		private void AddMapSeg(Location loc)
		{
			_srcMap.Add (new MapSeg(Line, Col, loc.File, loc.Row, loc.Column));
		}

		private void SetLoc(Location loc) 
		{
			if (_curLoc.File != loc.File || _curLoc.Row != loc.Row || _curLoc.Column != loc.Column) {
				AddMapSeg (loc);
				_curLoc = loc;
			}
		}

		private static string[] _strs = new string[4];

		private string ProcessString(string s1, string s2, string s3, string s4)
		{
			_strs[0] = s1;
			_strs[1] = s2;
			_strs[2] = s3;
			_strs[3] = s4;

			bool force_expr = EmitContext.ForceExpr;
			bool modified = false;

			int si = 0;
			string str = _strs[0];
			var len = str.Length;
			int s = 0;
			int d = 0;

			while (str != null) {

				if (s < len) {

					var ch = str[s];

					if (force_expr) {
						if (ch == '\t') {
							s++;
							modified = true;
							continue;
						} else if (ch == ';' && s < len - 1 && str[s + 1] == '\n') {
							s += 2;
							modified = true;
							continue;
						}
					} else {
						if (ch == '\t') {
							s++;
							var ilen = (_indentLevel < MAX_INDENT) ? (_indentLevel << 2) : (MAX_INDENT << 1);
							var i = 0;
							while (i < ilen) {
								buf[d++] = _indentStr[i++];
							}
							modified = true;
							continue;
						}
					}

					if (ch == '\n') {
						Line++;
						Col = 0;
					} else {
						Col++;
					}

					buf[d++] = str[s++];

				} else {

					si++;
					str = si < 4 ? _strs[si] : null;
					if (str == null) {
						break;
					}
					s = 0;
					len = str.Length;
					modified = true;

				}
			}

			if (modified) {
				return new string(buf, 0, d);
			} else {
				return s1;
			}
		}

		public string EscapeString (string s) {
			int len = s.Length;
			bool modified = false;
			int d = 0;
			for (var i = 0; i < len; i++) {
				var ch = s[i];
				if (ch < ' ' || ch > '~' || ch == '\\' || ch == '\'' || ch == '\"') {
					buf[d++] = '\\';
					modified = true;
					if (ch == '\r')
						buf[d++] = 'r';
					else if (ch == '\n')
						buf[d++] = 'n';
					else if (ch == 't')
						buf[d++] = 't';
					else if (ch == '\\')
						buf[d++] = '\\';
					else if (ch == '\'')
						buf[d++] = '\'';
					else if (ch == '\"')
						buf[d++] = '\"';
					else {
						buf[d++] = 'x';
						if ((ch & 0xff) > 0x9f)
							buf[d++] = (char)('a' + ((ch & 0xff) >> 4) - 0xa);
						else
							buf[d++] = (char)('0' + ((ch & 0xff) >> 4));
						if ((ch & 0xf) > 0x9)
							buf[d++] = (char)('a' + (ch & 0xf) - 0xa);
						else
							buf[d++] = (char)('0' + (ch & 0xf));
					}
				} else {
					buf[d++] = ch;
				}
			}
			if (modified) {
				return new String(buf, 0, d);
			} else {
				return s;
			}
		}

		public void Write (string s1)
		{
			Stream.Write (ProcessString (s1, null, null, null));
		}
		
		public void Write (string s1, string s2)
		{
			Stream.Write (ProcessString (s1, s2, null, null));
		}
		
		public void Write (string s1, string s2, string s3)
		{
			Stream.Write (ProcessString (s1, s2, s3, null));
		}
		
		public void Write (string s1, string s2, string s3, string s4)
		{
			Stream.Write (ProcessString (s1, s2, s3, s4));
		}
		
		public void Write (string s1, string s2, string s3, string s4, string s5)
		{
			Stream.Write (ProcessString (s1, s2, s3, s4));
			Stream.Write (ProcessString (s5, null, null, null));
		}
		
		public void Write (string s1, string s2, string s3, string s4, string s5, string s6)
		{
			Stream.Write (ProcessString (s1, s2, s3, s4));
			Stream.Write (ProcessString (s5, s6, null, null));
		}
		
		public void Write (string s1, string s2, string s3, string s4, string s5, string s6, string s7)
		{
			Stream.Write (ProcessString (s1, s2, s3, s4));
			Stream.Write (ProcessString (s5, s6, s7, null));
		}
		
		public void Write (string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8)
		{
			Stream.Write (ProcessString (s1, s2, s3, s4)); 
			Stream.Write ( ProcessString (s5, s6, s7, s8));
		}

		public void Write (string s1, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, null, null, null));
		}

		public void Write (string s1, string s2, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, s2, null, null));
		}

		public void Write (string s1, string s2, string s3, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, s2, s3, null));
		}

		public void Write (string s1, string s2, string s3, string s4, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, s2, s3, s4));
		}

		public void Write (string s1, string s2, string s3, string s4, string s5, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, s2, s3, s4)); 
			Stream.Write (ProcessString (s5, null, null, null));
		}

		public void Write (string s1, string s2, string s3, string s4, string s5, string s6, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, s2, s3, s4)); 
			Stream.Write (ProcessString (s5, s6, null, null));
		}

		public void Write (string s1, string s2, string s3, string s4, string s5, string s6, string s7, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, s2, s3, s4));
			Stream.Write (ProcessString (s5, s6, s7, null));
		}

		public void Write (string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, Location loc)
		{
			SetLoc (loc);
			Stream.Write (ProcessString (s1, s2, s3, s4)); 
			Stream.Write (ProcessString (s5, s6, s7, s8));
		}

		public void WriteBlockStatement (Statement s)
		{
			if (s is Block) {
				((Block)s).EmitBlockJs (EmitContext, false);
			} else {
				Write ("{\n");
				Indent ();
				s.EmitJs (EmitContext);
				Unindent ();
				Write ("\t}");
			}
		}

	}


}

