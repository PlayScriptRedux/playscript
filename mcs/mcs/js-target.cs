using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.CSharp.JavaScript
{
	public class JsEmitContext {

		public ModuleContainer Module;
		public JsEmitBuffer Buf;

		private List<JsEmitBuffer> _stack = new List<JsEmitBuffer>();
		private Dictionary<string, JsEmitBuffer> _stash = new Dictionary<string, JsEmitBuffer>();

		private HashSet<string> _definedNamespaces = new HashSet<string>();

		public JsEmitContext(ModuleContainer module)
		{
			Module = module;
			Buf = new JsEmitBuffer();
		}

		public CompilerContext Compiler {
			get { return Module.Compiler; }
		}

		public Report Report {
			get { return Module.Compiler.Report; }
		}

		public bool CheckCanEmit(Location loc) {
			if (loc.SourceFile != null && 
			    (loc.SourceFile.FileType != SourceFileType.ActionScript || loc.SourceFile.AsExtended == true)) {
				this.Report.Error (7071, loc,  "JavaScript code generation for C# or ASX types not supported.");
				return false;
			}
			return true;
		}

		public void Push()
		{
			var oldBuf = Buf;
			_stack.Add (Buf);
			Buf = new JsEmitBuffer();
			Buf.IndentLevel = oldBuf.IndentLevel; 
		}

		public void Pop()
		{
			Buf = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
		}

		public void Stash(string id)
		{
			var oldBuf = Buf;
			_stash[id] = Buf;
			Buf = new JsEmitBuffer();
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
		
		public string
			MakeJsNamespaceName(string ns)
		{
			return ns.Replace ('.', '$');
		}

	}

	public class JsEmitBuffer {

		public TextWriter Stream;
		public string CurIndent;

		private int _indentLevel;

		private static string[] _indents = 
		  { "", "  ", "    ", "      ", "        ", "          ", "            ", "              ", "                " };

		public JsEmitBuffer() 
		{
			Stream = new System.IO.StringWriter();
			IndentLevel = 0;
			CurIndent = "";
		}

		public int IndentLevel {
			set {
				if (_indentLevel != value) {
					if (value < _indents.Length - 1)
						CurIndent = _indents [value];
					else
						CurIndent = "                                                                                             ".Substring (0, _indentLevel * 2);
					_indentLevel = value;
				}
			}
			get {
				return _indentLevel;
			}
		}

		public void Indent()
		{
			IndentLevel++;
		}

		public void Unindent()
		{
			if (IndentLevel > 0)
				IndentLevel--;
		}

		public void Write(string s)
		{
			Stream.Write (s.Replace ("\t", CurIndent));
		}


	}


}

