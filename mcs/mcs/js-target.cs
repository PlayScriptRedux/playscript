using System;
using System.Collections.Generic;
using System.IO;

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
			if (loc.SourceFile != null && 
			    (loc.SourceFile.FileType != SourceFileType.ActionScript || loc.SourceFile.AsExtended == true)) {
				this.Report.Error (7071, loc,  "JavaScript code generation for C# or ASX types not supported.");
				return false;
			}
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

		public int GetOperPrecendence(Expression e)
		{
			if (e is As || e is AsIn || e is Is) {
				return 6;
			}

			if (e is TypeOf || e is AsDelete) {
				return 2;
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

				// ActionScript binary operators
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

		public TextWriter Stream;
		public string CurIndent;
		public JsEmitContext EmitContext;

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

		public void Write (string s)
		{
			if (EmitContext.ForceExpr) {
				Stream.Write (s.Replace ("\t", "").
				                Replace (";\n", ""));
			} else {
				Stream.Write (s.Replace ("\t", CurIndent));
			}
		}


	}


}

