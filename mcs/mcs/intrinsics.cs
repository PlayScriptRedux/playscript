using System;
using SLE = System.Linq.Expressions;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using MetaType = System.Type;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{

	public class MsilIntrinsicContext
	{
	}

	/// <summary>
	/// IL Intrinsic methods.
	/// </summary>
	public class MsilIntrinsic : ExpressionStatement
	{
		private OpCode[] _opCodes = {
			OpCodes.Nop,
			OpCodes.Break,
			OpCodes.Ldarg_0,
			OpCodes.Ldarg_1,
			OpCodes.Ldarg_2,
			OpCodes.Ldarg_3,
			OpCodes.Ldloc_0,
			OpCodes.Ldloc_1,
			OpCodes.Ldloc_2,
			OpCodes.Ldloc_3,
			OpCodes.Stloc_0,
			OpCodes.Stloc_1,
			OpCodes.Stloc_2,
			OpCodes.Stloc_3,
			OpCodes.Ldarg_S,
			OpCodes.Ldarga_S,
			OpCodes.Starg_S,
			OpCodes.Ldloc_S,
			OpCodes.Ldloca_S,
			OpCodes.Stloc_S,
			OpCodes.Ldnull,
			OpCodes.Ldc_I4_M1,
			OpCodes.Ldc_I4_0,
			OpCodes.Ldc_I4_1,
			OpCodes.Ldc_I4_2,
			OpCodes.Ldc_I4_3,
			OpCodes.Ldc_I4_4,
			OpCodes.Ldc_I4_5,
			OpCodes.Ldc_I4_6,
			OpCodes.Ldc_I4_7,
			OpCodes.Ldc_I4_8,
			OpCodes.Ldc_I4_S,
			OpCodes.Ldc_I4,
			OpCodes.Ldc_I8,
			OpCodes.Ldc_R4,
			OpCodes.Ldc_R8,
			OpCodes.Dup,
			OpCodes.Pop,
			OpCodes.Jmp,
			OpCodes.Call,
			OpCodes.Calli,
			OpCodes.Ret,
			OpCodes.Br_S,
			OpCodes.Brfalse_S,
			OpCodes.Brtrue_S,
			OpCodes.Beq_S,
			OpCodes.Bge_S,
			OpCodes.Bgt_S,
			OpCodes.Ble_S,
			OpCodes.Blt_S,
			OpCodes.Bne_Un_S,
			OpCodes.Bge_Un_S,
			OpCodes.Bgt_Un_S,
			OpCodes.Ble_Un_S,
			OpCodes.Blt_Un_S,
			OpCodes.Br,
			OpCodes.Brfalse,
			OpCodes.Brtrue,
			OpCodes.Beq,
			OpCodes.Bge,
			OpCodes.Bgt,
			OpCodes.Ble,
			OpCodes.Blt,
			OpCodes.Bne_Un,
			OpCodes.Bge_Un,
			OpCodes.Bgt_Un,
			OpCodes.Ble_Un,
			OpCodes.Blt_Un,
			OpCodes.Switch,
			OpCodes.Ldind_I1,
			OpCodes.Ldind_U1,
			OpCodes.Ldind_I2,
			OpCodes.Ldind_U2,
			OpCodes.Ldind_I4,
			OpCodes.Ldind_U4,
			OpCodes.Ldind_I8,
			OpCodes.Ldind_I,
			OpCodes.Ldind_R4,
			OpCodes.Ldind_R8,
			OpCodes.Ldind_Ref,
			OpCodes.Stind_Ref,
			OpCodes.Stind_I1,
			OpCodes.Stind_I2,
			OpCodes.Stind_I4,
			OpCodes.Stind_I8,
			OpCodes.Stind_R4,
			OpCodes.Stind_R8,
			OpCodes.Add,
			OpCodes.Sub,
			OpCodes.Mul,
			OpCodes.Div,
			OpCodes.Div_Un,
			OpCodes.Rem,
			OpCodes.Rem_Un,
			OpCodes.And,
			OpCodes.Or,
			OpCodes.Xor,
			OpCodes.Shl,
			OpCodes.Shr,
			OpCodes.Shr_Un,
			OpCodes.Neg,
			OpCodes.Not,
			OpCodes.Conv_I1,
			OpCodes.Conv_I2,
			OpCodes.Conv_I4,
			OpCodes.Conv_I8,
			OpCodes.Conv_R4,
			OpCodes.Conv_R8,
			OpCodes.Conv_U4,
			OpCodes.Conv_U8,
			OpCodes.Callvirt,
			OpCodes.Cpobj,
			OpCodes.Ldobj,
			OpCodes.Ldstr,
			OpCodes.Newobj,
			OpCodes.Castclass,
			OpCodes.Isinst,
			OpCodes.Conv_R_Un,
			OpCodes.Unbox,
			OpCodes.Throw,
			OpCodes.Ldfld,
			OpCodes.Ldflda,
			OpCodes.Stfld,
			OpCodes.Ldsfld,
			OpCodes.Ldsflda,
			OpCodes.Stsfld,
			OpCodes.Stobj,
			OpCodes.Conv_Ovf_I1_Un,
			OpCodes.Conv_Ovf_I2_Un,
			OpCodes.Conv_Ovf_I4_Un,
			OpCodes.Conv_Ovf_I8_Un,
			OpCodes.Conv_Ovf_U1_Un,
			OpCodes.Conv_Ovf_U2_Un,
			OpCodes.Conv_Ovf_U4_Un,
			OpCodes.Conv_Ovf_U8_Un,
			OpCodes.Conv_Ovf_I_Un,
			OpCodes.Conv_Ovf_U_Un,
			OpCodes.Box,
			OpCodes.Newarr,
			OpCodes.Ldlen,
			OpCodes.Ldelema,
			OpCodes.Ldelem_I1,
			OpCodes.Ldelem_U1,
			OpCodes.Ldelem_I2,
			OpCodes.Ldelem_U2,
			OpCodes.Ldelem_I4,
			OpCodes.Ldelem_U4,
			OpCodes.Ldelem_I8,
			OpCodes.Ldelem_I,
			OpCodes.Ldelem_R4,
			OpCodes.Ldelem_R8,
			OpCodes.Ldelem_Ref,
			OpCodes.Stelem_I,
			OpCodes.Stelem_I1,
			OpCodes.Stelem_I2,
			OpCodes.Stelem_I4,
			OpCodes.Stelem_I8,
			OpCodes.Stelem_R4,
			OpCodes.Stelem_R8,
			OpCodes.Stelem_Ref,
			OpCodes.Ldelem,
			OpCodes.Stelem,
			OpCodes.Unbox_Any,
			OpCodes.Conv_Ovf_I1,
			OpCodes.Conv_Ovf_U1,
			OpCodes.Conv_Ovf_I2,
			OpCodes.Conv_Ovf_U2,
			OpCodes.Conv_Ovf_I4,
			OpCodes.Conv_Ovf_U4,
			OpCodes.Conv_Ovf_I8,
			OpCodes.Conv_Ovf_U8,
			OpCodes.Refanyval,
			OpCodes.Ckfinite,
			OpCodes.Mkrefany,
			OpCodes.Ldtoken,
			OpCodes.Conv_U2,
			OpCodes.Conv_U1,
			OpCodes.Conv_I,
			OpCodes.Conv_Ovf_I,
			OpCodes.Conv_Ovf_U,
			OpCodes.Add_Ovf,
			OpCodes.Add_Ovf_Un,
			OpCodes.Mul_Ovf,
			OpCodes.Mul_Ovf_Un,
			OpCodes.Sub_Ovf,
			OpCodes.Sub_Ovf_Un,
			OpCodes.Endfinally,
			OpCodes.Leave,
			OpCodes.Leave_S,
			OpCodes.Stind_I,
			OpCodes.Conv_U,
			OpCodes.Prefix7,
			OpCodes.Prefix6,
			OpCodes.Prefix5,
			OpCodes.Prefix4,
			OpCodes.Prefix3,
			OpCodes.Prefix2,
			OpCodes.Prefix1,
			OpCodes.Prefixref,
			OpCodes.Arglist,
			OpCodes.Ceq,
			OpCodes.Cgt,
			OpCodes.Cgt_Un,
			OpCodes.Clt,
			OpCodes.Clt_Un,
			OpCodes.Ldftn,
			OpCodes.Ldvirtftn,
			OpCodes.Ldarg,
			OpCodes.Ldarga,
			OpCodes.Starg,
			OpCodes.Ldloc,
			OpCodes.Ldloca,
			OpCodes.Stloc,
			OpCodes.Localloc,
			OpCodes.Endfilter,
			OpCodes.Unaligned,
			OpCodes.Volatile,
			OpCodes.Tailcall,
			OpCodes.Initobj,
			OpCodes.Constrained,
			OpCodes.Cpblk,
			OpCodes.Initblk,
			OpCodes.Rethrow,
			OpCodes.Sizeof,
			OpCodes.Refanytype,
			OpCodes.Readonly
		};

		// Noop dummy expr
		public partial class DummyExpr : Expression
		{
			public DummyExpr ()
			{
			}

			public override bool ContainsEmitWithAwait ()
			{
				throw new NotSupportedException ();
			}

			public override Expression CreateExpressionTree (ResolveContext ec)
			{
				throw new NotSupportedException ("ET");
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				return null;
			}

			protected override void CloneTo (CloneContext clonectx, Expression t)
			{
			}

			public override void Emit (EmitContext ec)
			{
			}

			public override object Accept (StructuralVisitor visitor)
			{
				return visitor.Visit (this);
			}
		}

		// Used as as a dummy expression where we need to pass an empty emit expr
		private DummyExpr _dummyExpr = new DummyExpr();

		protected Arguments arguments;
		protected Expression expr;
		protected MethodGroupExpr mg;
		protected OpCode opcode;

		public static bool IsIntrnsic(Invocation invoke)
		{
			return false;
		}

		public MsilIntrinsic (Expression expr, Arguments arguments, MethodGroupExpr mg)
		{
			this.expr = expr;		
			this.arguments = arguments;
			this.mg = mg;
			if (expr != null) {
				loc = expr.Location;
			}
		}

		#region Properties

		public Arguments Arguments {
			get {
				return arguments;
			}
		}

		public Expression Exp {
			get {
				return expr;
			}
		}

		public MethodGroupExpr MethodGroup {
			get {
				return mg;
			}
		}

		public OpCode Op {
			get {
				return opcode;
			}
		}

		#endregion

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			MsilIntrinsic target = (MsilIntrinsic) t;

			if (arguments != null)
				target.arguments = arguments.Clone (clonectx);

			target.expr = expr.Clone (clonectx);
		}

		public override bool ContainsEmitWithAwait ()
		{
			if (arguments != null && arguments.ContainsEmitWithAwait ())
				return true;

			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			var invoke = new Invocation (expr, arguments);
			return invoke.CreateExpressionTree (ec);
		}

		private bool CheckConstant(ResolveContext ec, Argument arg) 
		{
			if (!(arg.Expr is Constant)) {
				ec.Report.Error (7801, expr.Location, "Intrinsic parameter must be constant");
				return false;
			}

			return true;
		}

		private bool CheckTypeExpr(ResolveContext ec, Argument arg)
		{
			if (!(arg.Expr is TypeOf) || (((TypeOf)arg.Expr).TypeArgument == null)) {
				ec.Report.Error (7802, expr.Location, "Intrinsic type parameter must be a constant type expression");
				return false;
			}

			return true;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			// We have to set these
			this.type = mg.BestCandidateReturnType;
			this.eclass = ExprClass.Value;

			expr = expr.Resolve (ec);
			if (expr == null) {
				return null;
			}

			Expression argExp;

			bool dynamic_arg;
			if (arguments != null)
				arguments.Resolve (ec, out dynamic_arg);

			switch (mg.Name) {
			case "Emit":
				if (!CheckConstant (ec, arguments [0]))
					return null;
				this.opcode = _opCodes [(int)((Constant)arguments [0].Expr).GetValue ()];
				break;
			case "Load":
				break;
			case "LoadAddr":
				argExp= arguments [0].Expr;
				if (argExp is BoxedCast)
					argExp = ((BoxedCast)argExp).Child;
				var memloc = argExp as IMemoryLocation;
				if (memloc == null) {
					ec.Report.Error (7803, expr.Location, "Argument must be a valid memory location");
					return null;
				}
				break;
			case "LoadInd":
				if (!CheckTypeExpr(ec, arguments[0]))
				    return null;
				if (!CheckConstant (ec, arguments [1]))
					return null;
				break;
			case "Store":
				argExp = arguments [0].Expr;
				if (argExp is BoxedCast)
					argExp = ((BoxedCast)argExp).Child;
				var t = argExp as IAssignMethod;
				if (t == null) {
					ec.Report.Error (7804, expr.Location, "Argument must be a valid assignment target");
					return null;
				}
				if (!CheckConstant (ec, arguments [1]))
					return null;
				break;
			case "StoreInd":
				if (!CheckTypeExpr(ec, arguments[0]))
					return null;
				if (!CheckConstant (ec, arguments [1]))
					return null;
				break;
			default:
				ec.Report.Error (7802, "Invalid intrinsic method");
				return null;
			}

			return this;
		}

		public override string GetSignatureForError ()
		{
			return mg.GetSignatureForError ();
		}


		public override void Emit (EmitContext ec)
		{
			Expression argExp;
			TypeSpec typeSpec;

			switch (mg.Name) {
			case "Emit":
				if (arguments.Count == 1) {
					ec.Emit (opcode);
				}
				break;
			case "Load":
				argExp = arguments [0].Expr;
				if (argExp is BoxedCast) 
					argExp = ((BoxedCast)argExp).Child;
				argExp.Emit (ec);
				break;
			case "LoadAddr":
				argExp = arguments [0].Expr;
				if (argExp is BoxedCast)
					argExp = ((BoxedCast)argExp).Child;
				var memloc = argExp as IMemoryLocation;
				memloc.AddressOf (ec, AddressOp.Load | AddressOp.Store);
				break;
			case "LoadInd":
				if ((bool)(arguments [1].Expr as BoolConstant).GetValue ()) 
					ec.Emit (OpCodes.Dup);
				typeSpec = ((TypeOf)arguments [0].Expr).TypeArgument;
				ec.EmitLoadFromPtr (typeSpec);
				break;
			case "Store":
				argExp = arguments [0].Expr;
				if (argExp is BoxedCast)
					argExp = ((BoxedCast)argExp).Child;
				var t = argExp as IAssignMethod;
				t.EmitAssign (ec, _dummyExpr, (bool)(arguments [1].Expr as BoolConstant).GetValue (), false);
				break;
			case "StoreInd":
				if ((bool)(arguments [1].Expr as BoolConstant).GetValue ()) 
					ec.Emit (OpCodes.Dup);
				typeSpec = ((TypeOf)arguments [0].Expr).TypeArgument;
				ec.EmitStoreFromPtr (typeSpec);
				break;
			}
		}

		public override void EmitStatement (EmitContext ec)
		{
			Emit (ec);
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			return MakeExpression (ctx, mg.InstanceExpression, mg.BestCandidate, arguments);
		}

		public static SLE.Expression MakeExpression (BuilderContext ctx, Expression instance, MethodSpec mi, Arguments args)
		{
			#if STATIC
			throw new NotSupportedException ();
			#else
			var instance_expr = instance == null ? null : instance.MakeExpression (ctx);
			return SLE.Expression.Call (instance_expr, (MethodInfo) mi.GetMetaInfo (), Arguments.MakeExpression (args, ctx));
			#endif
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}


}

