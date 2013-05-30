using System;

namespace PlayScript {

	// Delegate types with a params as the last argument.  Allows us to implement variadic method types in ActionScript/PlayScript.

	public delegate void ActionP(params object[] args);

	public delegate void ActionP<T1>(T1 a1, params object[] args);

	public delegate void ActionP<T1,T2>(T1 a1, T2 a2, params object[] args);

	public delegate void ActionP<T1,T2,T3>(T1 a1, T2 a2, T3 a3, params object[] args);

	public delegate void ActionP<T1,T2,T3,T4>(T1 a1, T2 a2, T3 a3, T4 a4, params object[] args);

	public delegate void ActionP<T1,T2,T3,T4,T5>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, params object[] args);

	public delegate void ActionP<T1,T2,T3,T4,T5,T6>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, params object[] args);

	public delegate void ActionP<T1,T2,T3,T4,T5,T6,T7>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, params object[] args);

	public delegate void ActionP<T1,T2,T3,T4,T5,T6,T7,T8>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8, params object[] args);

	public delegate TR FuncP<TR>(params object[] args);

	public delegate TR FuncP<T1,TR>(T1 a1, params object[] args);

	public delegate TR FuncP<T1,T2,TR>(T1 a1, T2 a2, params object[] args);

	public delegate TR FuncP<T1,T2,T3,TR>(T1 a1, T2 a2, T3 a3, params object[] args);

	public delegate TR FuncP<T1,T2,T3,T4,TR>(T1 a1, T2 a2, T3 a3, T4 a4, params object[] args);

	public delegate TR FuncP<T1,T2,T3,T4,T5,TR>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, params object[] args);

	public delegate TR FuncP<T1,T2,T3,T4,T5,T6,TR>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, params object[] args);

	public delegate TR FuncP<T1,T2,T3,T4,T5,T6,T7,TR>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, params object[] args);

	public delegate TR FuncP<T1,T2,T3,T4,T5,T6,T7,T8,TR>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8, params object[] args);

}


