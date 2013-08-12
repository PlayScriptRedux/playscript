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
using System.Diagnostics;
using System.Reflection;

namespace PlayScript
{
	/// <summary>
	/// This class is used to create actions (for property get, set...).
	/// It also takes care of simple conversion in an optimal manner.
	/// </summary>
	public static class ActionCreator
	{
		private const int NumberOfTypeCodes = 18 + 1;	// 18 is the last (string), +1 for index 0

		// This array end up being 19*19*4 = 1444 bytes long.
		private static IConverterFactory[,] sFactories = new IConverterFactory[NumberOfTypeCodes, NumberOfTypeCodes];
		static ActionCreator()
		{
			AddFactory(new ConverterFactory<InvokerThenConverterIntToUint, ConverterThenInvokerIntToUint, int, uint>());
			AddFactory(new ConverterFactory<InvokerThenConverterIntToDouble, ConverterThenInvokerIntToDouble, int, double>());
			AddFactory(new ConverterFactoryToFromObject<int>());

			AddFactory(new ConverterFactory<InvokerThenConverterUintToInt, ConverterThenInvokerUintToInt, uint, int>());
			AddFactory(new ConverterFactory<InvokerThenConverterUintToDouble, ConverterThenInvokerUintToDouble, uint, double>());
			AddFactory(new ConverterFactoryToFromObject<uint>());

			AddFactory(new ConverterFactory<InvokerThenConverterDoubleToInt, ConverterThenInvokerDoubleToInt, double, int>());
			AddFactory(new ConverterFactory<InvokerThenConverterDoubleToUint, ConverterThenInvokerDoubleToUint, double, uint>());
			AddFactory(new ConverterFactoryToFromObject<double>());

			AddFactory(new ConverterFactory<InvokerThenConverterObjectToInt, ConverterThenInvokerObjectToInt, object, int>());
			AddFactory(new ConverterFactory<InvokerThenConverterObjectToUint, ConverterThenInvokerObjectToUint, object, uint>());
			AddFactory(new ConverterFactory<InvokerThenConverterObjectToDouble, ConverterThenInvokerObjectToDouble, object, double>());

			// For object to object, we actually have to use a special case if FromT and ToT types do not match
			AddFactory(new ConverterFactoryToFromObject<object>());
		}

		static void AddFactory(IConverterFactory converterFactory)
		{
			int from = (int)Type.GetTypeCode(converterFactory.FromType);
			int to = (int)Type.GetTypeCode(converterFactory.ToType);
			Debug.Assert(sFactories[from, to] == null);
			sFactories[from, to] = converterFactory;
		}

		public static Func<ToT> CreatePropertyGetAction<ToT>(object target, PropertyInfo propertyInfo)
		{
			Func<ToT> funcTo;

			MethodInfo methodInfo = propertyInfo.GetGetMethod();
			Type propertyType = methodInfo.ReturnType;
			if (propertyType == typeof(ToT))
			{
				funcTo = (Func<ToT>)Delegate.CreateDelegate(typeof(Func<ToT>), target, methodInfo);
			}
			else
			{
				// If the type is not the same, we have to handle conversion between types (using the conversion matrix)
				TypeCode fromTypeCode = Type.GetTypeCode(propertyType);
				TypeCode toTypeCode = Type.GetTypeCode(typeof(ToT));

				// In the getter: If the fromTypeCode is an object, then we actually swap from and to as the object factory
				IConverterFactory converterFactory;
				converterFactory = sFactories[(int)fromTypeCode, (int)toTypeCode];
				if (converterFactory != null)
				{
					funcTo = (Func<ToT>)converterFactory.CreateGetConverter(target, methodInfo);
				}
				else
				{
					Console.WriteLine("PropertyGet - Conversion not supported from '" + propertyType.FullName + "' to '" + typeof(ToT).FullName + "'");
					throw new NotSupportedException();		// For the moment, we don't handle this conversion, add it to the matrix
				}
			}
			return funcTo;
		}

		public static Action<FromT> CreatePropertySetAction<FromT>(object target, PropertyInfo propertyInfo)
		{
			Action<FromT> actionFrom;

			MethodInfo methodInfo = propertyInfo.GetSetMethod();
			Type propertyType = methodInfo.GetParameters()[0].ParameterType;
			if (propertyType == typeof(FromT))
			{
				// If we have the same type, we will have a direct invocation
				// This also takes care of the object -> object, so we don't have dynamic invoke in that case
				actionFrom = (Action<FromT>)Delegate.CreateDelegate(typeof(Action<FromT>), target, methodInfo);
			}
			else
			{
				// If the type is not the same, we have to handle conversion between types (using the conversion matrix)
				TypeCode fromTypeCode = Type.GetTypeCode(typeof(FromT));
				TypeCode toTypeCode = Type.GetTypeCode(propertyType);
				IConverterFactory converterFactory = sFactories[(int)fromTypeCode, (int)toTypeCode];
				if (converterFactory != null)
				{
					actionFrom = (Action<FromT>)converterFactory.CreateSetConverter(target, methodInfo);
				}
				else
				{
					Console.WriteLine("PropertySet - Conversion not supported from '" + typeof(FromT).FullName + "' to '" + propertyType.FullName + "'");
					throw new NotSupportedException();		// For the moment, we don't handle this conversion, add it to the matrix
				}
			}
			return actionFrom;
		}

		abstract class ConverterThenInvoker<FromT, ToT>
		{
			protected Action<ToT> mActionTo;
			public void SetAction(Action<ToT> actionTo)
			{
				mActionTo = actionTo;
			}
			// We set this as abstract so we are sure that the aot compiler does not strip away the implementation (so generic method can reference it)
			public abstract void ConvertThenInvoke(FromT value);
		}

		abstract class InvokerThenConverter<FromT, ToT>
		{
			protected Func<FromT> mFuncFrom;
			public void SetFunc(Func<FromT> funcFrom)
			{
				mFuncFrom = funcFrom;
			}
			// We set this as abstract so we are sure that the aot compiler does not strip away the implementation (so generic method can reference it)
			public abstract ToT InvokeThenConvert();
		}

		abstract class IConverterFactory
		{
			public abstract object CreateSetConverter(object target, MethodInfo methodInfo);
			public abstract object CreateGetConverter(object target, MethodInfo methodInfo);
			public abstract Type FromType { get; }
			public abstract Type ToType { get; }

			protected static object CreateGetConverterInternal<GetterT, FromT, ToT>(ref MethodInfo getConverterMethodInfo, object target, MethodInfo methodInfo)
				where GetterT : InvokerThenConverter<FromT, ToT>, new()
			{
				Func<FromT> funcFrom = (Func<FromT>)Delegate.CreateDelegate(typeof(Func<FromT>), target, methodInfo);
				GetterT converter = new GetterT();
				converter.SetFunc(funcFrom);
				if (getConverterMethodInfo != null)
				{
					return Delegate.CreateDelegate(typeof(Func<ToT>), converter, getConverterMethodInfo);
				}
				else
				{
					// Note that we can't use Reflection to lookup the method "ConvertThenInvoke"
					// as the aot compiler optimizes away the whole function if no code "seems" to use it.
					// Instead, the first invocation, we use this explicitly so the aot compiler keeps reference of the function
					Func<ToT> funcTo = converter.InvokeThenConvert;
					// Then we update the converterMethodInfo, so it will be cached for next time
					getConverterMethodInfo = funcTo.Method;
					return funcTo;
				}
			}

			protected static object CreateSetConverterInternal<SetterT, FromT, ToT>(ref MethodInfo setConverterMethodInfo, object target, MethodInfo methodInfo)
				where SetterT : ConverterThenInvoker<FromT, ToT>, new()
			{
				Action<ToT> actionTo = (Action<ToT>)Delegate.CreateDelegate(typeof(Action<ToT>), target, methodInfo);
				SetterT converter = new SetterT();
				converter.SetAction(actionTo);
				if (setConverterMethodInfo != null)
				{
					return Delegate.CreateDelegate(typeof(Action<FromT>), converter, setConverterMethodInfo);
				}
				else
				{
					// Note that we can't use Reflection to lookup the method "ConvertThenInvoke"
					// as the aot compiler optimizes away the whole function if no code "seems" to use it.
					// Instead, the first invocation, we use this explicitly so the aot compiler keeps reference of the function
					Action<FromT> actionFrom = converter.ConvertThenInvoke;
					// Then we update the converterMethodInfo, so it will be cached for next time
					setConverterMethodInfo = actionFrom.Method;
					return actionFrom;
				}
			}
		}

		class ConverterFactory<GetterT, SetterT, FromT, ToT> : IConverterFactory
			where GetterT : InvokerThenConverter<FromT, ToT>, new()
			where SetterT : ConverterThenInvoker<FromT, ToT>, new()
		{
			static MethodInfo getConverterMethodInfo;
			static MethodInfo setConverterMethodInfo;
			public override object CreateGetConverter(object target, MethodInfo methodInfo)
			{
				return CreateGetConverterInternal<GetterT, FromT, ToT>(ref getConverterMethodInfo, target, methodInfo);
			}
			public override object CreateSetConverter(object target, MethodInfo methodInfo)
			{
				return CreateSetConverterInternal<SetterT, FromT, ToT>(ref setConverterMethodInfo, target, methodInfo);
			}
			public override Type FromType	{ get { return typeof(FromT); } }
			public override Type ToType		{ get { return typeof(ToT); } }
		}

		class ConverterThenInvokerIntToUint : ConverterThenInvoker<int, uint>
		{
			public override void ConvertThenInvoke(int value) { mActionTo((uint)value); }
		}

		class InvokerThenConverterIntToUint : InvokerThenConverter<int, uint>
		{
			public override uint InvokeThenConvert() { return (uint)mFuncFrom(); }
		}

		class ConverterThenInvokerIntToDouble : ConverterThenInvoker<int, double>
		{
			public override void ConvertThenInvoke(int value) { mActionTo((double)value); }
		}

		class InvokerThenConverterIntToDouble : InvokerThenConverter<int, double>
		{
			public override double InvokeThenConvert() { return (double)mFuncFrom(); }
		}

		class ConverterThenInvokerUintToInt : ConverterThenInvoker<uint, int>
		{
			public override void ConvertThenInvoke(uint value)	{ mActionTo((int)value); }
		}

		class InvokerThenConverterUintToInt : InvokerThenConverter<uint, int>
		{
			public override int InvokeThenConvert() { return (int)mFuncFrom(); }
		}

		class ConverterThenInvokerUintToDouble : ConverterThenInvoker<uint, double>
		{
			public override void ConvertThenInvoke(uint value) { mActionTo((double)value); }
		}

		class InvokerThenConverterUintToDouble : InvokerThenConverter<uint, double>
		{
			public override double InvokeThenConvert() { return (double)mFuncFrom(); }
		}

		class ConverterThenInvokerDoubleToInt : ConverterThenInvoker<double, int>
		{
			public override void ConvertThenInvoke(double value)	{ mActionTo((int)value); }
		}

		class InvokerThenConverterDoubleToInt : InvokerThenConverter<double, int>
		{
			public override int InvokeThenConvert() { return (int)mFuncFrom(); }
		}

		class ConverterThenInvokerDoubleToUint : ConverterThenInvoker<double, uint>
		{
			public override void ConvertThenInvoke(double value) { mActionTo((uint)value); }
		}

		class InvokerThenConverterDoubleToUint : InvokerThenConverter<double, uint>
		{
			public override uint InvokeThenConvert() { return (uint)mFuncFrom(); }
		}

		class ConverterThenInvokerObjectToInt : ConverterThenInvoker<object, int>
		{
			public override void ConvertThenInvoke(object value)
			{
				// Change this to use the common convert method?
				if (value is int)
				{
					mActionTo((int)value);
				}
				else if (value is uint)
				{
					mActionTo((int)(uint)value);
				}
				else if (value is double)
				{
					mActionTo((int)(double)value);
				}
				else
				{
					mActionTo(Convert.ToInt32(value));
				}
			}
		}

		class InvokerThenConverterObjectToInt : InvokerThenConverter<object, int>
		{
			public override int InvokeThenConvert()
			{
				object value = mFuncFrom();
				// Change this to use the common convert method?
				if (value is int)
				{
					return (int)value;
				}
				else if (value is uint)
				{
					return (int)(uint)value;
				}
				else if (value is double)
				{
					return (int)(double)value;
				}
				else
				{
					return Convert.ToInt32(value);
				}
			}
		}

		class ConverterThenInvokerObjectToUint : ConverterThenInvoker<object, uint>
		{
			public override void ConvertThenInvoke(object value)
			{
				// Change this to use the common convert method?
				if (value is uint)
				{
					mActionTo((uint)value);
				}
				else if (value is int)
				{
					mActionTo((uint)(int)value);
				}
				else if (value is double)
				{
					mActionTo((uint)(double)value);
				}
				else
				{
					mActionTo(Convert.ToUInt32(value));
				}
			}
		}

		class InvokerThenConverterObjectToUint : InvokerThenConverter<object, uint>
		{
			public override uint InvokeThenConvert()
			{
				object value = mFuncFrom();
				// Change this to use the common convert method?
				if (value is uint)
				{
					return (uint)value;
				}
				else if (value is int)
				{
					return (uint)(int)value;
				}
				else if (value is double)
				{
					return (uint)(double)value;
				}
				else
				{
					return Convert.ToUInt32(value);
				}
			}
		}

		class ConverterThenInvokerObjectToDouble : ConverterThenInvoker<object, double>
		{
			public override void ConvertThenInvoke(object value)
			{
				// Change this to use the common convert method?
				if (value is double)
				{
					mActionTo((double)value);
				}
				else if (value is int)
				{
					mActionTo((int)value);
				}
				else if (value is uint)
				{
					mActionTo((uint)value);
				}
				else
				{
					mActionTo(Convert.ToDouble(value));
				}
			}
		}

		class InvokerThenConverterObjectToDouble : InvokerThenConverter<object, double>
		{
			public override double InvokeThenConvert()
			{
				object value = mFuncFrom();
				// Change this to use the common convert method?
				if (value is double)
				{
					return (double)value;
				}
				else if (value is int)
				{
					return (int)value;
				}
				else if (value is uint)
				{
					return (uint)value;
				}
				else
				{
					return Convert.ToDouble(value);
				}
			}
		}


		class ConverterFactoryToFromObject<T> : IConverterFactory
		{
			static MethodInfo getConverterMethodInfo;
			static MethodInfo setConverterMethodInfo;

			public override object CreateGetConverter(object target, MethodInfo methodInfo)
			{
				InvokerThenConverterToObject<T> converter = new InvokerThenConverterToObject<T>(target, methodInfo);
				if (getConverterMethodInfo != null)
				{
					return Delegate.CreateDelegate(typeof(Func<object>), converter, getConverterMethodInfo);
				}
				else
				{
					// Note that we can't use Reflection to lookup the method "ConvertThenInvoke"
					// as the aot compiler optimizes away the whole function if no code "seems" to use it.
					// Instead, the first invocation, we use this explicitly so the aot compiler keeps reference of the function
					Func<object> funcTo = converter.InvokeThenConvert;
					// Then we update the converterMethodInfo, so it will be cached for next time
					getConverterMethodInfo = funcTo.Method;
					return funcTo;
				}
			}

			public override object CreateSetConverter(object target, MethodInfo methodInfo)
			{
				ConverterThenInvokerToObject<T> converter = new ConverterThenInvokerToObject<T>(target, methodInfo);
				if (setConverterMethodInfo != null)
				{
					return Delegate.CreateDelegate(typeof(Action<T>), converter, setConverterMethodInfo);
				}
				else
				{
					// Note that we can't use Reflection to lookup the method "ConvertThenInvoke"
					// as the aot compiler optimizes away the whole function if no code "seems" to use it.
					// Instead, the first invocation, we use this explicitly so the aot compiler keeps reference of the function
					Action<T> actionFrom = converter.ConvertThenInvoke;
					// Then we update the converterMethodInfo, so it will be cached for next time
					setConverterMethodInfo = actionFrom.Method;
					return actionFrom;
				}
			}

			public override Type FromType { get { return typeof(T); } }
			public override Type ToType { get { return typeof(object); } }
		}

		class ConverterThenInvokerToObject<FromT>
		{
			object mTarget;
			MethodInfo mMethodInfo;

			static object[] sParameters = new object[1];		// Static as only one invoker is called at a time
																// It is not thread safe though
			public ConverterThenInvokerToObject(object target, MethodInfo methodInfo)
			{
				mTarget = target;
				mMethodInfo = methodInfo;
			}

			public void ConvertThenInvoke(FromT value)
			{
				sParameters[0] = value;							// Cast is not even needed here as the invocation is taking care of any cast needed
																// For value type, this is going to box it
				mMethodInfo.Invoke(mTarget, sParameters);		// Invoke will throw an exception if value was not of the expected type
			}
		}

		class InvokerThenConverterToObject<FromT>
		{
			protected Func<FromT> mFuncFrom;
			public InvokerThenConverterToObject(object target, MethodInfo methodInfo)
			{
				mFuncFrom = (Func<FromT>)Delegate.CreateDelegate(typeof(Func<FromT>), target, methodInfo);
			}
			public object InvokeThenConvert()
			{
				return mFuncFrom();
			}
		}
	}
}

