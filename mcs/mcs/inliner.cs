using System;

namespace Mono.CSharp
{
	public class Inliner : StructuralVisitor
	{
		public Expression Expr;
		private Expression inlineExpr;

		public Statement Statement;
		public Statement inlineStmnt;

		private ResolveContext rc;
		private bool inlineFailed;
		private Invocation invocation;
		private MethodSpec methodSpec;
		private MemberCore method;
		private IMethodData methodData;


		public Inliner (Expression expr)
		{
			this.Statement = null;
			this.Expr = expr;
			this.AutoVisit = true;
		}

		public Inliner (Statement stmnt)
		{
			this.Statement = stmnt;
			this.Expr = ((StatementExpression)stmnt).Expr;
			this.AutoVisit = true;
		}

		private const int MAX_INLINE_STATEMENTS = 30;

		private class InlinableValidator : StructuralVisitor
		{
			public MemberCore method;
			public bool can_inline = true;
			public int statements = 0;

			public InlinableValidator(MemberCore method)
			{
				this.method = method;
			}

			public bool Validate(CompilerContext compiler, bool isExplicit) 
			{
				((IMethodData)method).Block.Accept (this);
				return can_inline && (isExplicit || statements < MAX_INLINE_STATEMENTS);
			}

			public override object Visit (Mono.CSharp.If s) 
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.For s) 
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Foreach s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.While s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Do s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Switch s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.SwitchLabel s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.BlockVariableDeclaration s) 
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Checked s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Unchecked s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Using s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Break s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Continue s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.Throw s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.TryCatch s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.TryFinally s)
			{
				statements++;
				return null;
			}

			public override object Visit (Mono.CSharp.LabeledStatement s)
			{
				can_inline = false;
				return null;
			}

			public override object Visit (Mono.CSharp.Goto s)
			{
				can_inline = false;
				return null;
			}

		}

		public static bool DetermineIsInlinable(CompilerContext compiler, MemberCore method)
		{
			bool isInlinable = false;

			if (compiler.Settings.Inlining == InliningMode.None) {

				isInlinable = false;

			} else {

				bool potentiallyInlinable = false;

				if ((method.ModFlags & (Modifiers.STATIC | Modifiers.SEALED)) != 0 || 
				    (method.Parent.ModFlags & Modifiers.SEALED) != 0 || (method.ModFlags & Modifiers.VIRTUAL) == 0) {
					potentiallyInlinable = true;
				}

				bool isExplicit = false;

				if (potentiallyInlinable) {
					isExplicit = method.OptAttributes.Contains (method.Parent.Module.PredefinedAttributes.InlineAttribute);
					if (compiler.Settings.Inlining == InliningMode.Explicit && isExplicit == false) {
						potentiallyInlinable = false;
					}
				}

				if (potentiallyInlinable) {
					var validator = new InlinableValidator (method);
					isInlinable = validator.Validate (compiler, isExplicit);
				}
			}

			return isInlinable;
		}

		private object TryInline(ResolveContext rc) {
			if (!(Expr is Invocation)) {
				return (object)Statement ?? (object)Expr;
			}

			invocation = (Expr as Invocation);
			if (invocation.MethodGroup.BestCandidate == null) {
				return (object)Statement ?? (object)Expr;
			}

			methodSpec = invocation.MethodGroup.BestCandidate;
			if (!(methodSpec.MemberDefinition is MethodCore)) {
				return (object)Statement ?? (object)Expr;
			}

			method = methodSpec.MemberDefinition as MemberCore;
			methodData = method as IMethodData;

			if (methodData.IsInlinable) {
				return (object)Statement ?? (object)Expr;
			}

			TypeSpec returnType = methodData.ReturnType;

			ToplevelBlock block = methodData.Block;
			if (block.Parameters.Count > 0 || block.TopBlock.NamesCount > 0 && block.TopBlock.LabelsCount > 0) {
				return (object)Statement ?? (object)Expr;
			}

			if (returnType != rc.BuiltinTypes.Void && 
			    block.Statements.Count == 1 && block.Statements [0] is Return) {
				inlineExpr = ((Return)block.Statements [0]).Expr.Clone (new CloneContext());
			} else if (returnType == rc.BuiltinTypes.Void) {
				Block newBlock = new Block (rc.CurrentBlock, block.StartLocation, block.EndLocation);
				foreach (var st in block.Statements) {
					newBlock.AddStatement (st.Clone (new CloneContext()));
				}
				inlineStmnt = newBlock;
			}

			this.rc = rc;
			this.inlineFailed = false;

			object ret;

			if (inlineExpr != null) {
				inlineExpr.Accept (this);
				ret = inlineExpr;
			} else {
				inlineStmnt.Accept (this);
				ret = inlineStmnt;
			}

			if (inlineFailed) {
				return (object)Statement ?? (object)Expr;
			}

			return ret;
		}

		public Statement TryInlineStatement(ResolveContext rc) {
			return TryInline (rc) as Statement;
		}

		public Expression TryInlineExpression(ResolveContext rc) {
			return TryInline (rc) as Expression;
		}

	}
}

