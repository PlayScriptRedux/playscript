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
using Mono.CSharp.Cpp;

namespace Mono.CSharp 
{

	public partial class AssemblyDefinition
	{
		private CppEmitContext cec;

		public void EmitCpp ()
		{
			cec = new CppEmitContext (module);
			
			cec.Buf.Write ("// Module: ", this.Name, ".cpp\n");
			
			cec.Pass = CppPasses.PREDEF;
			module.EmitContainerCpp (cec);

			cec.Pass = CppPasses.CLASSDEF;
			module.EmitContainerCpp (cec);
			
			cec.Pass = CppPasses.METHODS;
			module.EmitContainerCpp (cec);
		}

		public void SaveCpp ()
		{
			var s = cec.Buf.Stream.ToString ();
//			System.Console.WriteLine (s);
			System.IO.File.WriteAllText (file_name, s);
		}
	}

	public partial class TypeContainer
	{
		public virtual void EmitContainerCpp (CppEmitContext cec)
		{
			if (containers != null) {
				for (int i = 0; i < containers.Count; ++i)
					containers[i].EmitContainerCpp (cec);
			}
		}
	}

	public partial class NamespaceContainer
	{
		public override void EmitContainerCpp (CppEmitContext cec)
		{
			VerifyClsCompliance ();
			
			var ns = cec.MakeCppNamespaceName (this.NS.Name);
			var ns_names = this.NS.Name.Split (new char[] { '.' });
			bool is_global_ns = String.IsNullOrEmpty (ns);
			
			if (!is_global_ns && ns != cec.PrevNamespace) {
				if (cec.PrevNamespaceNames != null) {
					foreach (var n in cec.PrevNamespaceNames) {
						cec.Buf.Unindent ();
						cec.Buf.Write ("\t}\n");
					}
				}
				foreach (var n in ns_names) {
					cec.Buf.Write ("\tnamespace ", n, " {\n");
					cec.Buf.Indent();
				}
				cec.MarkNamespaceDefined (NS.Name);
				cec.PrevNamespace = ns;
				cec.PrevNamespaceNames = ns_names;
			}
			
			base.EmitContainerCpp (cec);
		}
	}

	public partial class ModuleContainer
	{
		public override void EmitContainerCpp (CppEmitContext cec)
		{
			if (OptAttributes != null)
				OptAttributes.EmitCpp (cec);
			
			if (cec.Pass == CppPasses.PREDEF) {
				foreach (var tc in containers) {
					tc.PrepareEmit ();
				}
			}
			
			cec.PrevNamespace = null;
			cec.PrevNamespaceNames = null;
			
			base.EmitContainerCpp (cec);
			
			// Close last unclosed namespace..
			if (cec.PrevNamespaceNames != null) {
				foreach (var n in cec.PrevNamespaceNames) {
					cec.Buf.Unindent ();
					cec.Buf.Write ("\t}\n");
				}
			}
			
			cec.PrevNamespace = null;
			cec.PrevNamespaceNames = null;
			
			if (cec.Pass == CppPasses.METHODS) {
				if (Compiler.Report.Errors == 0 && !Compiler.Settings.WriteMetadataOnly)
					VerifyMembers ();
			}
			
			if (anonymous_types != null) {
				foreach (var atypes in anonymous_types)
					foreach (var at in atypes.Value)
						at.EmitContainerCpp (cec);
			}
		}
	}

	public partial class TypeDefinition
	{
		public override void EmitContainerCpp (CppEmitContext cec)
		{
			if ((caching_flags & Flags.CloseTypeCreated) != 0)
				return;
			
			EmitCpp (cec);
		}


		public override void EmitCpp (CppEmitContext cec)
		{
			if (cec.Pass == CppPasses.PREDEF) {
				ValidateEmit ();
			}
			
			base.EmitCpp (cec);
			
			if (cec.Pass == CppPasses.PREDEF) {
				return;
			}
			
			int i;
			MemberCore m;
			HashSet<MemberCore> emitted = new HashSet<MemberCore> ();
			
			// Fields
			if (cec.Pass == CppPasses.CLASSDEF) {
				for (i = 0; i < members.Count; i++) {
					m = members [i];
					var f = m as Field;
					if (f != null) {
						f.EmitCpp (cec);
						emitted.Add (f);
					}
				}
			}
			
			if (cec.Pass == CppPasses.CLASSDEF || cec.Pass == CppPasses.METHODS) {
				
				// Constructors
				for (i = 0; i < members.Count; i++) {
					m = members [i];
					var c = m as Constructor;
					if (c != null && (c.ModFlags & Modifiers.STATIC) == 0) {
						c.EmitCpp (cec);
						emitted.Add (c);
					}
				}
				
				// Static constructors
				for (i = 0; i < members.Count; i++) {
					m = members [i];
					var c = m as Constructor;
					if (c != null && (c.ModFlags & Modifiers.STATIC) != 0) {
						c.EmitCpp (cec);
						emitted.Add (c);
					}
				}
				
				// Properties
				for (i = 0; i < members.Count; i++) {
					m = members [i];
					if (m is Property) {
						m.EmitCpp (cec);
						emitted.Add (m);
					}
				}
				
				// Methods
				for (i = 0; i < members.Count; i++) {
					m = members [i];
					if (m is Method) {
						m.EmitCpp (cec);
						emitted.Add (m);
					}
				}
				
				// Whatever else
				for (i = 0; i < members.Count; i++) {
					m = members [i];
					if (!emitted.Contains (m)) {
						m.EmitCpp (cec);
					}
				}
			}
			
		}
	}

	public partial class ClassOrStruct
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			if (cec.Pass == CppPasses.PREDEF) {
				
				if (!cec.CheckCanEmit (Location))
					return;
				
				if (!has_static_constructor && HasStaticFieldInitializer) {
					var c = DefineDefaultConstructor (true);
					c.Define ();
				}
				
				if (!(this.Parent is NamespaceContainer)) {
					cec.Report.Error (7175, Location, "C++ code generation for nested types not supported.");
					return;
				}
				
				Constructor constructor = null;
				
				foreach (var member in Members) {
					var c = member as Constructor;
					if (c != null) {
						if ((c.ModFlags & Modifiers.STATIC) != 0) {
							continue;
						} 
						if (constructor != null) {
							cec.Report.Error (7177, c.Location, "C++ generation not supported for overloaded constructors");
							return;
						}
						constructor = c;
					}
				}
				
			}
			
			if (cec.Pass == CppPasses.PREDEF) {
				cec.Buf.Write ("\tclass ", MemberName.Name, ";\n");
			} else if (cec.Pass == CppPasses.CLASSDEF) {
				cec.Buf.Write ("\tclass ", MemberName.Name, " {\n", Location);
				cec.Buf.Indent ();
				cec.Buf.Write ("\tpublic:\n");
			}
			
			base.EmitCpp (cec);
			
			if (cec.Pass == CppPasses.CLASSDEF) {
				cec.Buf.Unindent();
				cec.Buf.Write ("\t};\n");
			}
			
		}
	}

	public partial class Class
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			base.EmitCpp (cec);
		}
	}

	public partial class Struct
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			CheckStructCycles ();
			
			base.EmitCpp (cec);
		}
	}

	public partial class MemberCore
	{
		/// <summary>
		/// Base C++ emit method.  This is also entry point for CLS-Compliant verification.
		/// </summary>
		public virtual void EmitCpp (CppEmitContext cec)
		{
			if (!Compiler.Settings.VerifyClsCompliance)
				return;
			
			VerifyClsCompliance ();
		}
	}

	public partial class Expression
	{
		public virtual void EmitCpp (CppEmitContext cec)
		{
			cec.Report.Error (7174, this.loc, "C++ code generation for " + this.GetType ().Name + " expression not supported.");
			cec.Buf.Write ("<<" + this.GetType ().Name + " expr>>");
		}
	}

	public partial class Statement
	{
		protected virtual void DoEmitCpp (CppEmitContext cec) 
		{
			cec.Report.Error (7172, this.loc, "C++ code generation for " + this.GetType ().Name + " statement not supported.");
			cec.Buf.Write ("<<" + this.GetType ().Name + " stmnt>>");
		}
		
		public virtual void EmitCpp (CppEmitContext cec)
		{
			DoEmitCpp (cec);
		}
	}

	public partial class ExpressionStatement
	{
		public virtual void EmitStatementCpp (CppEmitContext cec)
		{
			cec.Report.Error(7172, Location, "C++ code generation for " + this.GetType ().Name + " statement not supported.");
			cec.Buf.Write ("<<" + this.GetType ().Name + " stmtexpr>>");
		}
	}

	public partial class Constant
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write (this.GetValueAsLiteral(), Location);
		}
	}

	public partial class DoubleConstant
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			double d = Value;
			if (d == System.Math.Floor (d)) {
				cec.Buf.Write (GetValue ().ToString (), ".0", Location);
			} else {
				cec.Buf.Write (GetValue ().ToString (), Location);
			}
		}
	}

	public partial class StringConstant
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			if (Value != null) {
				cec.Buf.Write ("\"", cec.Buf.EscapeString(Value), "\"", Location);
			} else {
				cec.Buf.Write ("\"\"", Location);
			}
		}
	}

	public partial class NullConstant
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write (GetValueAsLiteral(), Location);
		}
	}

	public partial class StringLiteral
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\"", cec.Buf.EscapeString(Value), "\"", loc);
		}
	}

	public partial class InterfaceMemberBase
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			CheckExternImpl ();
			
			base.EmitCpp (cec);
		}
	}

	public partial class MethodData
	{
		public void EmitCpp (TypeDefinition parent, CppEmitContext cec)
		{
			var mc = (IMemberContext) method;
			
			method.ParameterInfo.ApplyAttributes (mc, MethodBuilder);
			
			ToplevelBlock block = method.Block;
			if (block != null) {
				BlockContext bc = new BlockContext (mc, block, method.ReturnType);
				if (block.Resolve (null, bc, method)) {
					block.EmitBlockCpp (cec, false, false);
				}
			}
		}
	}

	public partial class MethodOrOperator
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			base.EmitCpp (cec);
			
			if ((this.ModFlags & Modifiers.STATIC) != 0) {
				cec.Buf.Write ("\tstatic ", Location);
			} else {
				cec.Buf.Write ("\t", Location);
			}
			
			if (cec.Pass == CppPasses.CLASSDEF) {
				cec.Buf.Write (cec.MakeCppFullTypeName (this.ReturnType), " ", this.MethodName.Name, "(");
				parameters.EmitCpp (cec);
				cec.Buf.Write (");\n");
			} else {
				cec.Buf.Write (cec.MakeCppFullTypeName (this.ReturnType), " ", cec.MakeCppTypeName (Parent.CurrentType, false), "::", this.MethodName.Name, "(");
				parameters.EmitCpp (cec);
				cec.Buf.Write (") ");
				
				if (MethodData != null)
					MethodData.EmitCpp (Parent, cec);
				
				Block = null;
				
				cec.Buf.Write ("\n");
			}
		}
	}

	public partial class Method
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			try {
				base.EmitCpp (cec);
			} catch {
				Console.WriteLine ("Internal compiler error at {0}: exception caught while emitting {1}",
				                   Location, MethodBuilder);
				throw;
			}
		}
	}

	public partial class ParametersCompiled
	{
		public void EmitCpp (CppEmitContext cec)
		{
			bool first = true;
			foreach (var p in this.FixedParameters) {
				var param = p as Parameter;
				if (param != null) {
					if (!first) {
						cec.Buf.Write (", ");
					}
					cec.Buf.Write (cec.MakeCppFullTypeName(param.Type), " ", param.Name);
					first = false;
				}
			}
		}
	}

	public partial class PropertyBase
	{
		public partial class GetMethod 
		{
			public override void EmitCpp (CppEmitContext cec)
			{
				if (cec.Pass == CppPasses.CLASSDEF) {
					cec.Buf.Write ("\t", cec.MakeCppFullTypeName (this.Property.MemberType), " get_", Property.MemberName.Name, "();\n");
				} else {
					cec.Buf.Write ("\t", cec.MakeCppFullTypeName (this.Property.MemberType), cec.MakeCppTypeName (Parent.CurrentType, false), "::", " get_", Property.MemberName.Name, "() ", Location);
					method_data.EmitCpp (Parent as TypeDefinition, cec);
					cec.Buf.Write ("\n");
					
					block = null;
				}
			}
		}
		public partial class SetMethod
		{
			public override void EmitCpp (CppEmitContext cec)
			{
				if (cec.Pass == CppPasses.CLASSDEF) {
					var parms = this.ParameterInfo;
					cec.Buf.Write ("\tvoid set_", Property.MemberName.Name, "(");
					cec.Buf.Write (cec.MakeCppFullTypeName (((Parameter)parms [0]).Type), " ", parms [0].Name);
					cec.Buf.Write (");\n");
				} else {
					var parms = this.ParameterInfo;
					cec.Buf.Write ("\tvoid set_", cec.MakeCppTypeName (Parent.CurrentType, false), "::", Property.MemberName.Name, "(");
					cec.Buf.Write (cec.MakeCppFullTypeName (((Parameter)parms [0]).Type), " ", parms [0].Name);
					cec.Buf.Write (") ", Location);
					method_data.EmitCpp (Parent as TypeDefinition, cec);
					cec.Buf.Write ("\n");
					
					block = null;
				}
			}
		}
		public override void EmitCpp (CppEmitContext cec)
		{
			if (this.Get != null) {
				this.Get.EmitCpp (cec);
			}
			
			if (this.Set != null) {
				this.Set.EmitCpp (cec);
			}
		}
	}

	public partial class FieldBase
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\t", cec.MakeCppFullTypeName(MemberType), " ", Name, Location);
			if (initializer != null) {
				ResolveContext rc = new ResolveContext (this);
				var expr = initializer.Resolve (rc);
				if (expr != null) {
					cec.Buf.Write (" = ");
					expr.EmitCpp (cec);
				}
			}
			cec.Buf.Write (";\n");
		}
	}

	public partial class Assign
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			if (Target is PropertyExpr) {
				((PropertyExpr)Target).EmitAssignCpp (cec, Source, false, false);
			} else {
				Target.EmitCpp (cec);
				cec.Buf.Write (" = ");
				Source.EmitCpp (cec);
			}
		}
	}

	public partial class Attributes
	{
		public void EmitCpp (CppEmitContext cec)
		{
		}
	}

	public partial class CompoundAssign 
	{
		public partial class TargetExpression
		{
			public override void EmitCpp (CppEmitContext cec)
			{
				child.EmitCpp (cec);
			}
		}
	}

	public partial class Binary 
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			var leftParens = cec.NeedParens(this, Left);
			var rightParens = cec.NeedParens(this, Right);
			
			if (leftParens)
				cec.Buf.Write ("(", Location);
			Left.EmitCpp (cec);
			if (leftParens)
				cec.Buf.Write (")");
			cec.Buf.Write (" " + this.OperName (oper) + " ");
			if (rightParens)
				cec.Buf.Write ("(", Location);
			Right.EmitCpp (cec);
			if (rightParens)
				cec.Buf.Write (")");
		}
	}

	public partial class Arguments
	{
		public virtual void EmitCpp (CppEmitContext cec)
		{
			bool first = true;
			foreach (Argument a in args) {
				if (!first)
					cec.Buf.Write(", ");
				a.Expr.EmitCpp (cec);
				first = false;
			}
		}
	}

	public partial class TypeCast
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("(", cec.MakeCppFullTypeName(Type), ")(", loc);
			Child.EmitCpp (cec);
			cec.Buf.Write (")");
		}
	}

	public partial class EmptyCast
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("(", cec.MakeCppFullTypeName(Type), ")(", loc);
			Child.EmitCpp (cec);
			cec.Buf.Write (")");
		}
	}

	partial class OpcodeCast
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("(", cec.MakeCppFullTypeName(Type), ")(", loc);
			Child.EmitCpp (cec);
			cec.Buf.Write (")");
		}
	}

	public partial class SimpleName
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write (Name, Location);
		}
	}

	public partial class FullNamedExpression
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write (cec.MakeCppFullTypeName(type), Location);
		}
	}

	public partial class FieldExpr
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			if (InstanceExpression != null) {
				InstanceExpression.EmitCpp (cec);
				cec.Buf.Write ("->");
			} else {
				cec.Buf.Write (DeclaringType.Name, "->", Location);
			}
			cec.Buf.Write (Name);
		}
	}

	partial class PropertyExpr
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			if (IsStatic) { 
				cec.Buf.Write (cec.MakeCppFullTypeName (best_candidate.DeclaringType, false), loc);
			} else {
				InstanceExpression.EmitCpp (cec);
			}
			cec.Buf.Write ("->", "get_", best_candidate.Name,  "()", loc);
		}
		
		public override void EmitAssignCpp (CppEmitContext cec, Expression source, bool leave_copy, bool isCompound)
		{
			if (IsStatic) { 
				cec.Buf.Write (cec.MakeCppFullTypeName (best_candidate.DeclaringType, false), loc);
			} else {
				InstanceExpression.EmitCpp (cec);
			}
			cec.Buf.Write ("->", "set_", best_candidate.Name,  "(");
			source.EmitCpp (cec);
			cec.Buf.Write (")", loc);
		}
	}

	public partial class TemporaryVariableReference
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Report.Error (7174, this.loc, "C++ code generation for " + this.GetType ().Name + " expression not supported.");
			cec.Buf.Write ("<<" + this.GetType ().Name + " expr>>");
		}
	}

	public partial class Unary
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			bool needsParen = cec.NeedParens (this, Expr);
			
			cec.Buf.Write (OperName (Oper), Location);
			if (needsParen)
				cec.Buf.Write ("(");
			Expr.EmitCpp (cec);
			if (needsParen)
				cec.Buf.Write (")");
		}
	}

	public partial class UnaryMutator
	{
		private void EmitOpCpp (CppEmitContext cec)
		{
			if (mode == Mode.PreIncrement)
				cec.Buf.Write ("++", Location);
			else if (mode == Mode.PreDecrement)
				cec.Buf.Write ("--", Location);
			
			// NOTE: TODO - Add parentheses if child op precedence is lower.
			
			Expr.EmitCpp (cec);
			
			if (mode == Mode.PostIncrement)
				cec.Buf.Write ("++");
			else if (mode == Mode.PostDecrement)
				cec.Buf.Write ("--");
		}
		
		public override void EmitStatementCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\t", Location);
			EmitOpCpp (cec);
			cec.Buf.Write (";\n");
		}

		public override void EmitCpp (CppEmitContext cec)
		{
			EmitOpCpp (cec);
		}
	}

	public partial class StringConcat
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("_root::String.concat(", loc);
			bool first = true;
			foreach (var a in arguments) {
				if (!first) {
					cec.Buf.Write (", ");
				}
				a.Expr.EmitCpp (cec);
				first = false;
			}
			cec.Buf.Write (")", loc);
		}
	}

	public partial class Conditional
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			bool test_parens = cec.NeedParens (this, expr);
			bool true_parens = cec.NeedParens (this, true_expr);
			bool false_parens = cec.NeedParens (this, false_expr);
			
			if (test_parens) {
				cec.Buf.Write ("(");
				expr.EmitCpp (cec);
				cec.Buf.Write (") ? ");
			} else {
				expr.EmitCpp (cec);
				cec.Buf.Write (" ? ");
			}
			
			if (true_parens) {
				cec.Buf.Write ("(");
				true_expr.EmitCpp (cec);
				cec.Buf.Write (") : ");
			} else {
				true_expr.EmitCpp (cec);
				cec.Buf.Write (" : ");
			}
			
			if (false_parens) {
				cec.Buf.Write ("(");
				false_expr.EmitCpp (cec);
				cec.Buf.Write (")");
			} else {
				false_expr.EmitCpp (cec);
			}
		}
	}

	public partial class VariableReference
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write (Name, Location);
		}
	}

	public partial class Invocation
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			// Write CPP literal code for __cpp__() invocation.
			if (expr is SimpleName && ((SimpleName)expr).Name == "__cpp__" && 
			    arguments.Count == 1 && arguments[0].Expr is StringLiteral) {
				cec.Buf.Write (((StringLiteral)arguments[0].Expr).Value);
				return;
			}
			
			if (expr != null) {
				if (mg.IsStatic && expr is TypeExpr) {
					cec.Buf.Write (cec.MakeCppFullTypeName(((TypeExpr)expr).Type, false), "::", Location);
				} else {
					expr.EmitCpp (cec);
				}
			}
			mg.EmitCallCpp (cec, arguments);
		}

		public override void EmitStatementCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\t", Location);
			
			EmitCpp (cec);
			
			cec.Buf.Write (";\n");
		}
	}

	public partial class New
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write("new ", cec.MakeCppFullTypeName(Type, false) ,"(", Location);
			if (arguments != null)
				arguments.EmitCpp (cec);
			cec.Buf.Write(")");
		}
		
		public override void EmitStatementCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\t", Location);
			EmitCpp (cec);
			cec.Buf.Write (";\n");
		}
	}

	public partial class MemberAccess
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			expr.EmitCpp (cec);
			cec.Buf.Write ("->");
			cec.Buf.Write (Name);
		}
	}

	partial class IndexerExpr
	{
		public override void EmitCpp (CppEmitContext cec)
		{
			InstanceExpression.EmitCpp (cec);
			cec.Buf.Write ("[");
			bool first = true;
			foreach (var arg in arguments) {
				if (!first)
					cec.Buf.Write (", ");
				arg.Expr.EmitCpp (cec);
				first = false;
			}
			cec.Buf.Write ("]");
		}
	}

	public partial class EmptyStatement
	{
		public override void EmitCpp (CppEmitContext cec)
		{
		}

		protected override void DoEmitCpp (CppEmitContext cec)
		{
			throw new NotSupportedException();
		}
	}

	public partial class Block
	{
		public void EmitBlockCpp (CppEmitContext cec, bool as_statement = true, bool no_braces = false)
		{
			if (as_statement && statements.Count == 0 && 
			    (scope_initializers == null || scope_initializers.Count == 0))
				return;
			
			if (!no_braces) {
				if (as_statement) {
					cec.Buf.Write ("\t{\n");
				} else {
					cec.Buf.Write ("{\n");
				}
				cec.Buf.Indent ();
			}
			
			if (scope_initializers != null)
				EmitScopeInitializersCpp (cec);
			
			for (int ix = 0; ix < statements.Count; ix++) {
				statements [ix].EmitCpp (cec);
			}
			
			if (!no_braces) {
				cec.Buf.Unindent ();
				if (as_statement) {
					cec.Buf.Write ("\t}\n");
				} else {
					cec.Buf.Write ("\t}");
				}
			}
		}
		
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			EmitBlockCpp (cec);
			
		}
		
		public override void EmitCpp (CppEmitContext cec)
		{
			DoEmitCpp (cec);
		}
		
		protected void EmitScopeInitializersCpp (CppEmitContext cec)
		{
			foreach (Statement s in scope_initializers)
				s.EmitCpp (cec);
		}
	}

	public partial class MethodGroupExpr
	{
		public void EmitCallCpp (CppEmitContext cec, Arguments arguments)
		{
			cec.Buf.Write("(", Location);
			if (arguments != null)
				arguments.EmitCpp (cec);
			cec.Buf.Write(")");
		}
	}

	partial class PropertyOrIndexerExpr<T> where T : PropertySpec
	{
		public virtual void EmitAssignCpp (CppEmitContext cec, Expression source, bool leave_copy, bool isCompound)
		{
			EmitCpp (cec);
		}	
	}

	public partial class If
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			//
			// If we're a boolean constant, Resolve() already
			// eliminated dead code for us.
			//
			Constant c = expr as Constant;
			if (c != null) {
				
				if (!c.IsDefaultValue)
					TrueStatement.EmitCpp (cec);
				else if (FalseStatement != null)
					FalseStatement.EmitCpp (cec);
				
				return;
			}
			
			cec.Buf.Write ("\tif (", loc);
			expr.EmitCpp (cec);
			cec.Buf.Write (") ");
			
			cec.Buf.WriteBlockStatement (TrueStatement);
			
			if (FalseStatement != null) {
				cec.Buf.Write (" else ");
				
				cec.Buf.WriteBlockStatement (FalseStatement);
			}
			
			cec.Buf.Write ("\n");
		}
	}

	public partial class Do
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\tdo ", loc);
			
			cec.Buf.WriteBlockStatement (EmbeddedStatement);
			
			cec.Buf.Write (" while (", expr.Location);
			expr.EmitCpp (cec);
			cec.Buf.Write (");\n");
		}
	}

	public partial class While
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			if (empty) {
				return;
			}
			
			//
			// Inform whether we are infinite or not
			//
			if (expr is Constant) {
				// expr is 'true', since the 'empty' case above handles the 'false' case
				cec.Buf.Write ("\twhile (true) ", loc);
			} else {
				cec.Buf.Write ("\twhile (", loc);
				expr.EmitCpp (cec);
				cec.Buf.Write (") ");
			}	
			
			cec.Buf.WriteBlockStatement (Statement);
			
			cec.Buf.Write ("\n");
		}
	}

	public partial class For
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			// NOTE: WE don't optimize for emty loop right now..
			
			cec.Buf.Write ("\tfor (", loc);
			cec.PushForceExpr(true);
			if (Initializer != null)
				Initializer.EmitCpp (cec);
			cec.Buf.Write ("; ");
			if (Condition != null)
				Condition.EmitCpp (cec);
			cec.Buf.Write ("; ");
			if (Iterator != null)
				Iterator.EmitCpp (cec);
			cec.Buf.Write (") ");
			cec.PopForceExpr();
			
			cec.Buf.WriteBlockStatement (Statement);
			
			cec.Buf.Write ("\n");
		}
	}

	public partial class StatementExpression
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			expr.EmitStatementCpp (cec);
		}
	}

	public partial class Return
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			if (expr != null) {
				cec.Buf.Write ("\treturn ", loc);
				expr.EmitCpp (cec);
				cec.Buf.Write (";\n");
			}
		}
	}

	public partial class Throw
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\tthrow ");
			expr.EmitCpp (cec);
			cec.Buf.Write (";\n");
		}
	}

	public partial class Break
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ( "\tbreak;\n", loc);
		}
	}

	public partial class Continue
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			cec.Buf.Write ("\tcontinue;\n", loc);
		}
	}

	public partial class BlockVariable
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			if (Initializer != null) {
				cec.Buf.Write ("\t", loc);
				Initializer.EmitCpp (cec);
			} else {
				cec.Buf.Write ("\t", cec.MakeCppFullTypeName(this.type), " ", Variable.Name, loc);
			}
			
			if (declarators != null) {
				foreach (var d in declarators) {
					cec.Buf.Write (", ");
					if (d.Initializer != null) {
						d.Initializer.EmitCpp (cec);
					} else {
						cec.Buf.Write (d.Variable.Name);
					}
				}
			}
			
			cec.Buf.Write (";\n");
		}

	}

	public partial class Switch
	{
		protected override void DoEmitCpp (CppEmitContext cec)
		{
			// FIXME: Switch has changed..
			base.DoEmitCpp (cec);
//			cec.Buf.Write ("\tswitch (", loc);
//			Expr.EmitCpp (cec);
//			cec.Buf.Write (") {\n");
//			cec.Buf.Indent ();
//			
//			foreach (var section in Sections) {
//				foreach (var label in section.Labels) {
//					if (label.IsDefault) {
//						cec.Buf.Write ("\tdefault:\n", label.Location);
//					} else {
//						cec.Buf.Write ("\tcase ", label.Location);
//						label.Label.EmitCpp (cec);
//						cec.Buf.Write (":\n");
//					}
//				}
//				cec.Buf.Indent ();
//				section.Block.EmitBlockCpp (cec, true, true);
//				cec.Buf.Unindent ();
//			}
//			
//			cec.Buf.Unindent ();
//			cec.Buf.Write ("\t}\n");
		}
	}

	public partial class AsUseNamespaceStatement
	{
		public override void EmitCpp (CppEmitContext cec)
		{
		}
	}

	public partial class AsNonAssignStatementExpression
	{
		protected override void DoEmitCpp (CppEmitContext cec) 
		{
			expr.EmitCpp (cec);
		}
		
		public override void EmitCpp (CppEmitContext cec)
		{
			DoEmitCpp (cec);
		}
	}

}