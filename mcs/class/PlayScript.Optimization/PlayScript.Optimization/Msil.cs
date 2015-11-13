using System;

namespace PlayScript.Optimization
{
	// Msil ops
	public enum Op {
		Nop,
		Break,
		Ldarg_0,
		Ldarg_1,
		Ldarg_2,
		Ldarg_3,
		Ldloc_0,
		Ldloc_1,
		Ldloc_2,
		Ldloc_3,
		Stloc_0,
		Stloc_1,
		Stloc_2,
		Stloc_3,
		Ldarg_S,
		Ldarga_S,
		Starg_S,
		Ldloc_S,
		Ldloca_S,
		Stloc_S,
		Ldnull,
		Ldc_I4_M1,
		Ldc_I4_0,
		Ldc_I4_1,
		Ldc_I4_2,
		Ldc_I4_3,
		Ldc_I4_4,
		Ldc_I4_5,
		Ldc_I4_6,
		Ldc_I4_7,
		Ldc_I4_8,
		Ldc_I4_S,
		Ldc_I4,
		Ldc_I8,
		Ldc_R4,
		Ldc_R8,
		Dup,
		Pop,
		Jmp,
		Call,
		Calli,
		Ret,
		Br_S,
		Brfalse_S,
		Brtrue_S,
		Beq_S,
		Bge_S,
		Bgt_S,
		Ble_S,
		Blt_S,
		Bne_Un_S,
		Bge_Un_S,
		Bgt_Un_S,
		Ble_Un_S,
		Blt_Un_S,
		Br,
		Brfalse,
		Brtrue,
		Beq,
		Bge,
		Bgt,
		Ble,
		Blt,
		Bne_Un,
		Bge_Un,
		Bgt_Un,
		Ble_Un,
		Blt_Un,
		Switch,
		Ldind_I1,
		Ldind_U1,
		Ldind_I2,
		Ldind_U2,
		Ldind_I4,
		Ldind_U4,
		Ldind_I8,
		Ldind_I,
		Ldind_R4,
		Ldind_R8,
		Ldind_Ref,
		Stind_Ref,
		Stind_I1,
		Stind_I2,
		Stind_I4,
		Stind_I8,
		Stind_R4,
		Stind_R8,
		Add,
		Sub,
		Mul,
		Div,
		Div_Un,
		Rem,
		Rem_Un,
		And,
		Or,
		Xor,
		Shl,
		Shr,
		Shr_Un,
		Neg,
		Not,
		Conv_I1,
		Conv_I2,
		Conv_I4,
		Conv_I8,
		Conv_R4,
		Conv_R8,
		Conv_U4,
		Conv_U8,
		Callvirt,
		Cpobj,
		Ldobj,
		Ldstr,
		Newobj,
		Castclass,
		Isinst,
		Conv_R_Un,
		Unbox,
		Throw,
		Ldfld,
		Ldflda,
		Stfld,
		Ldsfld,
		Ldsflda,
		Stsfld,
		Stobj,
		Conv_Ovf_I1_Un,
		Conv_Ovf_I2_Un,
		Conv_Ovf_I4_Un,
		Conv_Ovf_I8_Un,
		Conv_Ovf_U1_Un,
		Conv_Ovf_U2_Un,
		Conv_Ovf_U4_Un,
		Conv_Ovf_U8_Un,
		Conv_Ovf_I_Un,
		Conv_Ovf_U_Un,
		Box,
		Newarr,
		Ldlen,
		Ldelema,
		Ldelem_I1,
		Ldelem_U1,
		Ldelem_I2,
		Ldelem_U2,
		Ldelem_I4,
		Ldelem_U4,
		Ldelem_I8,
		Ldelem_I,
		Ldelem_R4,
		Ldelem_R8,
		Ldelem_Ref,
		Stelem_I,
		Stelem_I1,
		Stelem_I2,
		Stelem_I4,
		Stelem_I8,
		Stelem_R4,
		Stelem_R8,
		Stelem_Ref,
		Ldelem,
		Stelem,
		Unbox_Any,
		Conv_Ovf_I1,
		Conv_Ovf_U1,
		Conv_Ovf_I2,
		Conv_Ovf_U2,
		Conv_Ovf_I4,
		Conv_Ovf_U4,
		Conv_Ovf_I8,
		Conv_Ovf_U8,
		Refanyval,
		Ckfinite,
		Mkrefany,
		Ldtoken,
		Conv_U2,
		Conv_U1,
		Conv_I,
		Conv_Ovf_I,
		Conv_Ovf_U,
		Add_Ovf,
		Add_Ovf_Un,
		Mul_Ovf,
		Mul_Ovf_Un,
		Sub_Ovf,
		Sub_Ovf_Un,
		Endfinally,
		Leave,
		Leave_S,
		Stind_I,
		Conv_U,
		Prefix7,
		Prefix6,
		Prefix5,
		Prefix4,
		Prefix3,
		Prefix2,
		Prefix1,
		Prefixref,
		Arglist,
		Ceq,
		Cgt,
		Cgt_Un,
		Clt,
		Clt_Un,
		Ldftn,
		Ldvirtftn,
		Ldarg,
		Ldarga,
		Starg,
		Ldloc,
		Ldloca,
		Stloc,
		Localloc,
		Endfilter,
		Unaligned,
		Volatile,
		Tailcall,
		Initobj,
		Constrained,
		Cpblk,
		Initblk,
		Rethrow,
		Sizeof,
		Refanytype,
		Readonly
	}

	unsafe public static class Msil {

		/// <summary>
		/// Emit the specified IL opcode.
		/// </summary>
		/// <param name="op">The opcode to emit.</param>
		public static void Emit(Op op) {
		}

		/// <summary>
		/// Load the specified value or variable and places it on the top of the stack.
		/// </summary>
		/// <param name="arg">The argument to load.</param>
		public static void Load(object arg) {
		}

		/// <summary>
		/// Load the specified value or variable and places it on the top of the stack.
		/// </summary>
		/// <param name="ptr">The pointer to load.</param>
		public static void Load(byte* ptr) {
		}

		/// <summary>
		/// Loads the address of the given argument.
		/// </summary>
		/// <param name="arg">The argument must be a valid memory location (local variable, array element, class or structure field, etc.)</param>
		public static void LoadAddr(object arg) {
		}

		/// <summary>
		/// Store's value on the top of the stack to the given target, and optionally leaves a copy of the value on the stack.
		/// </summary>
		/// <param name="target">The target variable, member field, member writable property, etc.</param>
		/// <param name="leave_copy">If set to <c>true</c> will leave a copy of the value on the stack.</param>
		public static void Store(object target, bool leave_copy = false) {
		}

		/// <summary>
		/// Store's value on the top of the stack to the given target, and optionally leaves a copy of the value on the stack.
		/// </summary>
		/// <param name="ptr">The target ptr to store to.</param>
		/// <param name="leave_copy">If set to <c>true</c> will leave a copy of the value on the stack.</param>
		public static void Store(byte* ptr, bool leave_copy = false) {
		}

		/// <summary>
		/// Loads a value indirectly using the pointer location on the top of the stack.
		/// </summary>
		/// <param name="type">Type.</param>
		/// <param name="leave_copy">If set to <c>true</c> will leave a copy of the pointer on the stack.</param>
		public static void LoadInd(Type type, bool leave_copy = false) {
		}

		/// <summary>
		/// Stores a value indirectly to the pointer location on the top of the stack.
		/// </summary>
		/// <param name="type">Type.</param>
		/// <param name="leave_copy">If set to <c>true</c> will leave a copy of the pointer on the stack.</param>
		public static void StoreInd(Type type, bool leave_copy = false) {
		}

	}
}

