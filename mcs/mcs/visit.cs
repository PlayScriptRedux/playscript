//
// visit.cs: Visitors for parsed dom
//
// Authors: Mike Kr�ger (mkrueger@novell.com)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// Copyright 2011 Xamarin Inc
//

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Mono.CSharp
{
	public enum VisitDepth
	{
		Namespaces 		= 1,	// Namespaces
		Types 			= 2,	// Types
		Members 		= 3,	// Member declarations
		Initializers 	= 4,	// Field, const initializers
		MethodBodies 	= 5,	// Method,constructor,property bodies
		All				= 6		// Everything
	}

	public abstract class StructuralVisitor
	{
		// True to automatically visit all children, false to manually implement traversal in visit methods
		public bool AutoVisit;
		// True to continue auto traversal, false exits immediately
		public bool Continue = true;
		// True to skip children of this element and continue to next element
		public bool Skip;
		// Maximum depth for visitor
		public VisitDepth Depth = VisitDepth.All;

		public virtual void Visit (MemberCore member)
		{
			Debug.Fail ("unknown member type: " + member.GetType ());
		}

		void VisitTypeContainer (TypeContainer tc)
		{
			foreach (var container in tc.Containers) {
				container.Accept (this);
			}
		}

		void VisitTypeContainer (TypeDefinition tc)
		{
			foreach (var member in tc.Members) {
				member.Accept (this);
			}
		}

		public virtual void Visit (UsingClause usingClause)
		{
		}

		public virtual void Visit (ModuleContainer module)
		{
			if (!AutoVisit)
				VisitTypeContainer (module);
		}

		public virtual void Visit (UsingNamespace un)
		{
		}

		public virtual void Visit (UsingAliasNamespace uan)
		{
		}
		
		public virtual void Visit (UsingExternAlias uea)
		{
		}

		public virtual void Visit (NamespaceContainer ns)
		{
			if (!AutoVisit)
				VisitTypeContainer (ns);
		}

		public virtual void Visit (CompilationSourceFile csf)
		{
			VisitTypeContainer (csf);
		}

		public virtual void Visit (Class c)
		{
			if (!AutoVisit)
				VisitTypeContainer (c);
		}

		public virtual void Visit (Struct s)
		{
			if (!AutoVisit)
				VisitTypeContainer (s);
		}


		public virtual void Visit (Interface i)
		{
			if (!AutoVisit)
				VisitTypeContainer (i);
		}

		public virtual void Visit (Delegate d)
		{
		}

		public virtual void Visit (Enum e)
		{
			if (!AutoVisit)
				VisitTypeContainer (e);
		}

		public virtual void Visit (FixedField f)
		{
		}

		public virtual void Visit (Const c)
		{
		}

		public virtual void Visit (Field f)
		{
		}

		public virtual void Visit (Operator o)
		{
		}

		public virtual void Visit (Indexer i)
		{
		}

		public virtual void Visit (Method m)
		{
		}

		public virtual void Visit (Property p)
		{
		}

		public virtual void Visit (Constructor c)
		{
		}

		public virtual void Visit (Destructor d)
		{
		}

		public virtual void Visit (EventField e)
		{
		}
		public virtual void Visit (EventProperty ep)
		{
		}

		public virtual void Visit (EnumMember em)
		{
		}

		public virtual object Visit (Statement stmt)
		{
			Debug.Fail ("unknown statement:" + stmt);
			return null;
		}
		
		public virtual object Visit (BlockVariable blockVariable)
		{
			return null;
		}
		
		public virtual object Visit (BlockConstant blockConstantDeclaration)
		{
			return null;
		}
		
		public virtual object Visit (EmptyStatement emptyStatement)
		{
			return null;
		}

		public virtual object Visit (EmptyExpressionStatement emptyExpressionStatement)
		{
			return null;
		}

		public virtual object Visit (EmptyExpression emptyExpression)
		{
			return null;
		}
		
		public virtual object Visit (ErrorExpression errorExpression)
		{
			return null;
		}

		public virtual object Visit (If ifStatement)
		{
			return null;
		}


		public virtual object Visit (Do doStatement)
		{
			return null;
		}

		public virtual object Visit (While whileStatement)
		{
			return null;
		}

		public virtual object Visit (For forStatement)
		{
			return null;
		}

		public virtual object Visit (StatementExpression statementExpression)
		{
			return null;
		}

		public virtual object Visit (StatementErrorExpression errorStatement)
		{
			return null;
		}

		public virtual object Visit (Return returnStatement)
		{
			return null;
		}

		public virtual object Visit (Goto gotoStatement)
		{
			return null;
		}

		public virtual object Visit (LabeledStatement labeledStatement)
		{
			return null;
		}

		public virtual object Visit (SwitchLabel switchLabel)
		{
			return null;
		}

		public virtual object Visit (GotoDefault gotoDefault)
		{
			return null;
		}

		public virtual object Visit (GotoCase gotoCase)
		{
			return null;
		}

		public virtual object Visit (Throw throwStatement)
		{
			return null;
		}

		public virtual object Visit (Break breakStatement)
		{
			return null;
		}

		public virtual object Visit (Continue continueStatement)
		{
			return null;
		}

		public virtual object Visit (Block blockStatement)
		{
			return null;
		}
		
		public virtual object Visit (Switch switchStatement)
		{
			return null;
		}
		
		public virtual object Visit (StatementList statementList)
		{
			return null;
		}
		
		public virtual object Visit (Lock lockStatement)
		{
			return null;
		}

		public virtual object Visit (Unchecked uncheckedStatement)
		{
			return null;
		}

		public virtual object Visit (Checked checkedStatement)
		{
			return null;
		}

		public virtual object Visit (Unsafe unsafeStatement)
		{
			return null;
		}


		public virtual object Visit (Fixed fixedStatement)
		{
			return null;
		}


		public virtual object Visit (TryFinally tryFinallyStatement)
		{
			return null;
		}

		public virtual object Visit (TryCatch tryCatchStatement)
		{
			return null;
		}

		public virtual object Visit (Using usingStatement)
		{
			return null;
		}

		public virtual object Visit (Foreach foreachStatement)
		{
			return null;
		}

		public virtual object Visit (Yield yieldStatement)
		{
			return null;
		}

		public virtual object Visit (YieldBreak yieldBreakStatement)
		{
			return null;
		}

		public virtual object Visit (Expression expression)
		{
			Debug.Fail ("Visit unknown expression:" + expression);
			return null;
		}

		public virtual object Visit (MemberAccess memberAccess)
		{
			return null;
		}

		public virtual object Visit (QualifiedAliasMember qualifiedAliasMember)
		{
			return null;
		}

		public virtual object Visit (LocalVariableReference localVariableReference)
		{
			return null;
		}

		public virtual object Visit (Constant constant)
		{
			return null;
		}

		public virtual object Visit (BooleanExpression booleanExpression)
		{
			return null;
		}

		public virtual object Visit (SimpleName simpleName)
		{
			return null;
		}

		public virtual object Visit (ParenthesizedExpression parenthesizedExpression)
		{
			return null;
		}

		public virtual object Visit (Unary unaryExpression)
		{
			return null;
		}

		public virtual object Visit (UnaryMutator unaryMutatorExpression)
		{
			return null;
		}

		// *expr
		public virtual object Visit (Indirection indirectionExpression)
		{
			return null;
		}

		public virtual object Visit (Is isExpression)
		{
			return null;
		}

		public virtual object Visit (As asExpression)
		{
			return null;
		}

		public virtual object Visit (Cast castExpression)
		{
			return null;
		}

		public virtual object Visit (ComposedCast composedCast)
		{
			return null;
		}

		public virtual object Visit (DefaultValueExpression defaultValueExpression)
		{
			return null;
		}
		
		public virtual object Visit (DefaultParameterValueExpression defaultParameterValueExpression)
		{
			return null;
		}
		
		public virtual object Visit (Binary binaryExpression)
		{
			return null;
		}

		public virtual object Visit (Nullable.NullCoalescingOperator nullCoalescingOperator)
		{
			return null;
		}

		public virtual object Visit (Conditional conditionalExpression)
		{
			return null;
		}

		public virtual object Visit (Invocation invocationExpression)
		{
			return null;
		}

		public virtual object Visit (New newExpression)
		{
			return null;
		}

		public virtual object Visit (NewAnonymousType newAnonymousType)
		{
			return null;
		}

		public virtual object Visit (NewInitialize newInitializeExpression)
		{
			return null;
		}

		public virtual object Visit (ArrayCreation arrayCreationExpression)
		{
			return null;
		}

		public virtual object Visit (This thisExpression)
		{
			return null;
		}

		public virtual object Visit (ArglistAccess argListAccessExpression)
		{
			return null;
		}

		public virtual object Visit (Arglist argListExpression)
		{
			return null;
		}

		public virtual object Visit (TypeOf typeOfExpression)
		{
			return null;
		}

		public virtual object Visit (SizeOf sizeOfExpression)
		{
			return null;
		}

		public virtual object Visit (CheckedExpr checkedExpression)
		{
			return null;
		}

		public virtual object Visit (UnCheckedExpr uncheckedExpression)
		{
			return null;
		}

		public virtual object Visit (ElementAccess elementAccessExpression)
		{
			return null;
		}

		public virtual object Visit (BaseThis baseAccessExpression)
		{
			return null;
		}

		public virtual object Visit (StackAlloc stackAllocExpression)
		{
			return null;
		}

		public virtual object Visit (SimpleAssign simpleAssign)
		{
			return null;
		}

		public virtual object Visit (CompoundAssign compoundAssign)
		{
			return null;
		}

		public virtual object Visit (TypeExpression typeExpression)
		{
			return null;
		}

		public virtual object Visit (AnonymousMethodExpression anonymousMethodExpression)
		{
			return null;
		}
		
		public virtual object Visit (LambdaExpression lambdaExpression)
		{
			return null;
		}
		
		public virtual object Visit (ConstInitializer constInitializer)
		{
			return null;
		}
		
		public virtual object Visit (ArrayInitializer arrayInitializer)
		{
			return null;
		}
		
		public virtual object Visit (Linq.QueryExpression queryExpression)
		{
			return null;
		}

		public virtual object Visit (Linq.QueryStartClause queryExpression)
		{
			return null;
		}
		
		public virtual object Visit (Linq.SelectMany selectMany)
		{
			return null;
		}

		public virtual object Visit (Linq.Select select)
		{
			return null;
		}

		public virtual object Visit (Linq.GroupBy groupBy)
		{
			return null;
		}

		public virtual object Visit (Linq.Let let)
		{
			return null;
		}

		public virtual object Visit (Linq.Where where)
		{
			return null;
		}

		public virtual object Visit (Linq.Join join)
		{
			return null;
		}

		public virtual object Visit (Linq.GroupJoin groupJoin)
		{
			return null;
		}

		public virtual object Visit (Linq.OrderByAscending orderByAscending)
		{
			return null;
		}

		public virtual object Visit (Linq.OrderByDescending orderByDescending)
		{
			return null;
		}

		public virtual object Visit (Linq.ThenByAscending thenByAscending)
		{
			return null;
		}

		public virtual object Visit (Linq.ThenByDescending thenByDescending)
		{
			return null;
		}
		
		// undocumented expressions
		public virtual object Visit (RefValueExpr refValueExpr)
		{
			return null;
		}
		
		public virtual object Visit (RefTypeExpr refTypeExpr)
		{
			return null;
		}
		
		public virtual object Visit (MakeRefExpr makeRefExpr)
		{
			return null;
		}

		public virtual object Visit (Await awaitExpr)
		{
			return null;
		}

		// ActionScript/PlayScript Expressions

		public virtual object Visit (AsObjectInitializer initializer)
		{
			return null;
		}

		public virtual object Visit (AsArrayInitializer initializer)
		{
			return null;
		}

		public virtual object Visit (AsNew newExpression)
		{
			return null;
		}

		public virtual object Visit (AsDelete deleteExpr)
		{
			return null;
		}

		public virtual object Visit (AsTypeOf typeOfExpr)
		{
			return null;
		}

		public virtual object Visit (AsIn inExpr)
		{
			return null;
		}

		public virtual object Visit (AsLocalFunction localFunc)
		{
			return null;
		}

		public virtual object Visit (AsUseNamespaceStatement useNamespace)
		{
			return null;
		}

		public virtual object Visit (AsNonAssignStatementExpression asStmntExpr)
		{
			return null;
		}

		public virtual object Visit (AsXmlQueryExpression xmlQueryExpr)
		{
			return null;
		}

	}
}
