//
// as-lang.cs: ActionScript language support
//
// Author: Ben Cooley (bcooley@zynga.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//

using System;
using System.Collections.Generic;

namespace Mono.CSharp
{
	//
	// Constants
	//

	public static class AsConsts 
	{
		//
		// The namespace used for the root package.
		//
		public const string AsRootNamespace = "_root";
	}

	//
	// Expressions
	//

	//
	// ActionScript: Object initializers implement standard JSON style object
	// initializer syntax in the form { ident : expr [ , ... ] } or { "literal" : expr [, ... ]}
	// Like the array initializer, type is inferred from assignment type, parameter type, or
	// field, var initializer type, or of no type can be inferred it is of type Dictionary<String,Object>.
	//
	public class AsObjectInitializer : Expression
	{
		List<Expression> elements;
		BlockVariableDeclaration variable;
		Assign assign;
		TypeSpec inferredObjType;

		public AsObjectInitializer (List<Expression> init, Location loc)
		{
			elements = init;
			this.loc = loc;
		}

		public AsObjectInitializer (int count, Location loc)
			: this (new List<Expression> (count), loc)
		{
		}

		public AsObjectInitializer (Location loc)
			: this (4, loc)
		{
		}

		#region Properties

		public int Count {
			get { return elements.Count; }
		}

		public List<Expression> Elements {
			get {
				return elements;
			}
		}

		public Expression this [int index] {
			get {
				return elements [index];
			}
		}

		public BlockVariableDeclaration VariableDeclaration {
			get {
				return variable;
			}
			set {
				variable = value;
			}
		}

		public Assign Assign {
			get {
				return assign;
			}
			set {
				assign = value;
			}
		}

		#endregion

		public void Add (Expression expr)
		{
			elements.Add (expr);
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			var target = (AsObjectInitializer) t;

			target.elements = new List<Expression> (elements.Count);
			foreach (var element in elements)
				target.elements.Add (element.Clone (clonectx));
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			var current_field = rc.CurrentMemberDefinition as FieldBase;
			TypeExpression type;
			if (inferredObjType != null) {
				type = new TypeExpression (inferredObjType, Location);
			} else if (current_field != null && rc.CurrentAnonymousMethod == null) {
				type = new TypeExpression (current_field.MemberType, current_field.Location);
			} else if (variable != null) {
				if (variable.TypeExpression is VarExpr) {
					type = new TypeExpression (rc.BuiltinTypes.Dynamic, Location);
				} else {
					type = new TypeExpression (variable.Variable.Type, variable.Variable.Location);
				}
			} else if (assign != null) {
				type = new TypeExpression (assign.Target.Type, assign.Target.Location);
			} else {
				type = new TypeExpression (rc.BuiltinTypes.Dynamic, Location);
			}

			return new NewInitialize (type, null, 
				new CollectionOrObjectInitializers(elements, Location), Location).Resolve (rc);
		}

		public Expression InferredResolveWithObjectType(ResolveContext rc, TypeSpec objType) 
		{
			inferredObjType = objType;
			return Resolve (rc);
		}

		public override void Emit (EmitContext ec)
		{
			throw new InternalErrorException ("Missing Resolve call");
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// ActionScript: Array initializer expression is a standard expression
	// allowed anywhere an expression is valid.  The type is inferred from
	// assignment type, parameter type, or field/variable initializer type.
	// If no type is inferred, the type is Vector.<Object>.
	//
	public class AsArrayInitializer : ArrayInitializer
	{
		Assign assign;
		TypeSpec inferredArrayType;

		public AsArrayInitializer (List<Expression> init, Location loc)
			: base(init, loc)
		{
		}

		public AsArrayInitializer (int count, Location loc)
			: this (new List<Expression> (count), loc)
		{
		}

		public AsArrayInitializer (Location loc)
			: this (4, loc)
		{
		}

		#region Properties

		public Assign Assign {
			get {
				return assign;
			}
			set {
				assign = value;
			}
		}

		#endregion

		protected override Expression DoResolve (ResolveContext rc)
		{
			var current_field = rc.CurrentMemberDefinition as FieldBase;
			TypeExpression type;
			if (inferredArrayType != null) {
				type = new TypeExpression (inferredArrayType, Location);
			} else if (current_field != null && rc.CurrentAnonymousMethod == null) {
				type = new TypeExpression (current_field.MemberType, current_field.Location);
			} else if (variable != null) {
				if (variable.TypeExpression is VarExpr) {
					type = new TypeExpression (rc.Module.PredefinedTypes.AsArray.Resolve(), Location);
				} else {
					type = new TypeExpression (variable.Variable.Type, variable.Variable.Location);
				}
			} else if (assign != null) {
				type = new TypeExpression (assign.Target.Type, assign.Target.Location);
			} else {
				type = new TypeExpression (rc.Module.PredefinedTypes.AsArray.Resolve(), Location);
			}

			TypeSpec typeSpec = type.ResolveAsType(rc.MemberContext);
			if (typeSpec.IsArray) {
				ArrayCreation arrayCreate = (ArrayCreation)new ArrayCreation (type, this).Resolve (rc);
				return arrayCreate;
			} else {
				var initElems = new List<Expression>();
				foreach (var e in elements) {
					initElems.Add (new CollectionElementInitializer(e));
				}
				return new NewInitialize (type, null, 
					new CollectionOrObjectInitializers(initElems, Location), Location).Resolve (rc);
			}
		}

		public Expression InferredResolveWithArrayType(ResolveContext rc, TypeSpec arrayType) 
		{
			inferredArrayType = arrayType;
			return Resolve (rc);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// ActionScript: Implements the ActionScript delete expression.
	// This expression is used to implement the delete expression as
	// well as the delete statement.  Handles both the element access
	// form or the member access form.
	//
	public class AsDelete : ExpressionStatement {

		public Expression Expr;
		private Invocation removeExpr;
		
		public AsDelete (Expression expr, Location l)
		{
			this.Expr = expr;
			loc = l;
		}

		public override bool IsSideEffectFree {
			get {
				return removeExpr.IsSideEffectFree;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return removeExpr.ContainsEmitWithAwait ();
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (Expr is ElementAccess) {

				var elem_access = Expr as ElementAccess;

				if (elem_access.Arguments.Count != 1) {
					ec.Report.Error (7021, loc, "delete statement must have only one index argument.");
					return null;
				}

				var expr = elem_access.Expr.Resolve (ec);
				if (expr.Type == null) {
					return null;
				}

				if (expr.Type.IsArray) {
					ec.Report.Error (7021, loc, "delete statement not allowed on arrays.");
					return null;
				}

				removeExpr = new Invocation (new MemberAccess (expr, "Remove", loc), elem_access.Arguments);
				return removeExpr.Resolve (ec);

			} else if (Expr is MemberAccess) {

				var memb_access = Expr as MemberAccess;

				var expr = memb_access.LeftExpression.Resolve (ec);
				if (expr.Type == null) {
					return null;
				}

				var args = new Arguments(1);
				args.Add (new Argument(new StringLiteral(ec.BuiltinTypes, memb_access.Name, loc)));
				removeExpr = new Invocation (new MemberAccess (expr, "Remove", loc), args);
				return removeExpr.Resolve (ec);

			} else {
				// Error is reported elsewhere.
				return null;
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			var target = (AsDelete) t;

			target.Expr = Expr.Clone (clonectx);
		}

		public override void Emit (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}

		public override void EmitStatement (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return removeExpr.CreateExpressionTree(ec);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

}

