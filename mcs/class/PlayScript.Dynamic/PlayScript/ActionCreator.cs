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

#if DEBUG
	#define RECREATE_DEFAULT_INVOKER
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

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
		private static ConverterFactoryBase[,] sFactories = new ConverterFactoryBase[NumberOfTypeCodes, NumberOfTypeCodes];

		class InvokerInfo
		{
			public InvokerInfo(InvokerFactoryBase factory)
			{
				Factory = factory;
			}
			public InvokerFactoryBase	Factory;
			public int					Usage;
		}

		private static Dictionary<MethodInfo, InvokerInfo> sInvokerInfoByMethodInfo = new Dictionary<MethodInfo, InvokerInfo>();
		private static Dictionary<MethodSignature, InvokerInfo> sInvokerInfoByMethodSignature = new Dictionary<MethodSignature, InvokerInfo>();

		public delegate InvokerFactoryBase CreateInvokerFactoryFromMethodSignatureDelegate(MethodSignature methodSignature, int usage);
	
		public static CreateInvokerFactoryFromMethodSignatureDelegate CreateInvokerFactoryFromMethodSignature;

#if RECREATE_DEFAULT_INVOKER
		public static int RecreateDefaultInvokerEveryNUsages = 100;				// When using the default invoker, we let the user know so (s)he can find the biggest offenders
#endif

		static ActionCreator()
		{
			AddConverterFactories();
			AddInvokerFactories();
		}

		static void AddConverterFactories()
		{
			AddConverterFactory(new ConverterFactory<InvokerThenConverterIntToUint, ConverterThenInvokerIntToUint, ConverterIntToUint, int, uint>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterIntToDouble, ConverterThenInvokerIntToDouble, ConverterIntToDouble, int, double>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterIntToBool, ConverterThenInvokerIntToBool, ConverterIntToBool, int, bool>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterIntToString, ConverterThenInvokerIntToString, ConverterIntToString, int, string>());
			AddConverterFactory(new ConverterFactoryToFromObject<int>());

			AddConverterFactory(new ConverterFactory<InvokerThenConverterUintToInt, ConverterThenInvokerUintToInt, ConverterUintToInt, uint, int>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterUintToDouble, ConverterThenInvokerUintToDouble, ConverterUintToDouble, uint, double>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterUintToBool, ConverterThenInvokerUintToBool, ConverterUintToBool, uint, bool>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterUintToString, ConverterThenInvokerUintToString, ConverterUintToString, uint, string>());
			AddConverterFactory(new ConverterFactoryToFromObject<uint>());

			AddConverterFactory(new ConverterFactory<InvokerThenConverterDoubleToInt, ConverterThenInvokerDoubleToInt, ConverterDoubleToInt, double, int>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterDoubleToUint, ConverterThenInvokerDoubleToUint, ConverterDoubleToUint, double, uint>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterDoubleToBool, ConverterThenInvokerDoubleToBool, ConverterDoubleToBool, double, bool>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterDoubleToString, ConverterThenInvokerDoubleToString, ConverterDoubleToString, double, string>());
			AddConverterFactory(new ConverterFactoryToFromObject<double>());

			AddConverterFactory(new ConverterFactory<InvokerThenConverterBoolToInt, ConverterThenInvokerBoolToInt, ConverterBoolToInt, bool, int>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterBoolToUint, ConverterThenInvokerBoolToUint, ConverterBoolToUint, bool, uint>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterBoolToDouble, ConverterThenInvokerBoolToDouble, ConverterBoolToDouble, bool, double>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterBoolToString, ConverterThenInvokerBoolToString, ConverterBoolToString, bool, string>());
			AddConverterFactory(new ConverterFactoryToFromObject<bool>());

			AddConverterFactory(new ConverterFactory<InvokerThenConverterStringToInt, ConverterThenInvokerStringToInt, ConverterStringToInt, string, int>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterStringToUint, ConverterThenInvokerStringToUint, ConverterStringToUint, string, uint>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterStringToDouble, ConverterThenInvokerStringToDouble, ConverterStringToDouble, string, double>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterStringToBool, ConverterThenInvokerStringToBool, ConverterStringToBool, string, bool>());
			AddConverterFactory(new ConverterFactoryToFromObject<string>());

			AddConverterFactory(new ConverterFactory<InvokerThenConverterObjectToInt, ConverterThenInvokerObjectToInt, ConverterObjectToInt, object, int>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterObjectToUint, ConverterThenInvokerObjectToUint, ConverterObjectToUint, object, uint>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterObjectToDouble, ConverterThenInvokerObjectToDouble, ConverterObjectToDouble, object, double>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterObjectToBool, ConverterThenInvokerObjectToBool, ConverterObjectToBool, object, bool>());
			AddConverterFactory(new ConverterFactory<InvokerThenConverterObjectToString, ConverterThenInvokerObjectToString, ConverterObjectToString, object, string>());

			// For object to object, we actually have to use a special case if FromT and ToT types do not match
			AddConverterFactory(new ConverterFactoryToFromObject<object>());
		}

		static void AddConverterFactory(ConverterFactoryBase converterFactory)
		{
			int from = (int)Type.GetTypeCode(converterFactory.FromType);
			int to = (int)Type.GetTypeCode(converterFactory.ToType);
			Debug.Assert(sFactories[from, to] == null);
			sFactories[from, to] = converterFactory;
		}

		static void AddInvokerFactories()
		{
			// Add some factories that most ActionScript applications will need
			AddInvokerFactory(new InvokerAFactory());

			AddInvokerFactory(new InvokerAFactory<int>());
			AddInvokerFactory(new InvokerAFactory<uint>());
			AddInvokerFactory(new InvokerAFactory<double>());
			AddInvokerFactory(new InvokerAFactory<string>());
			AddInvokerFactory(new InvokerAFactory<bool>());

			AddInvokerFactory(new InvokerAFactory<int, int>());
			AddInvokerFactory(new InvokerAFactory<int, uint>());
			AddInvokerFactory(new InvokerAFactory<int, double>());
			AddInvokerFactory(new InvokerAFactory<int, string>());
			AddInvokerFactory(new InvokerAFactory<int, bool>());

			AddInvokerFactory(new InvokerAFactory<uint, int>());
			AddInvokerFactory(new InvokerAFactory<uint, uint>());
			AddInvokerFactory(new InvokerAFactory<uint, double>());
			AddInvokerFactory(new InvokerAFactory<uint, string>());
			AddInvokerFactory(new InvokerAFactory<uint, bool>());

			AddInvokerFactory(new InvokerAFactory<double, int>());
			AddInvokerFactory(new InvokerAFactory<double, uint>());
			AddInvokerFactory(new InvokerAFactory<double, double>());
			AddInvokerFactory(new InvokerAFactory<double, string>());
			AddInvokerFactory(new InvokerAFactory<double, bool>());

			AddInvokerFactory(new InvokerAFactory<string, int>());
			AddInvokerFactory(new InvokerAFactory<string, uint>());
			AddInvokerFactory(new InvokerAFactory<string, double>());
			AddInvokerFactory(new InvokerAFactory<string, string>());
			AddInvokerFactory(new InvokerAFactory<string, bool>());

			AddInvokerFactory(new InvokerAFactory<bool, int>());
			AddInvokerFactory(new InvokerAFactory<bool, uint>());
			AddInvokerFactory(new InvokerAFactory<bool, double>());
			AddInvokerFactory(new InvokerAFactory<bool, string>());
			AddInvokerFactory(new InvokerAFactory<bool, bool>());
		}

		static void AddInvokerFactory(InvokerFactoryBase invokerFactory)
		{
			// We only add the factories by method signature, it will be automatically exposed to MethodInfo as they are discovered
			MethodSignature methodSignature = invokerFactory.GetMethodSignature();
			sInvokerInfoByMethodSignature.Add(methodSignature, new InvokerInfo(invokerFactory));
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
				ConverterFactoryBase converterFactory;
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
				ConverterFactoryBase converterFactory = sFactories[(int)fromTypeCode, (int)toTypeCode];
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

		public static InvokerBase CreateInvoker(Delegate del)
		{
			return CreateInvoker(del.Target, del.Method);		// We probably want to create a version that takes and store directly the delegate
																// Might be faster if we can populate the invoker with it.
		}

		public static InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			InvokerInfo invokerInfo = GetInvokerInfo(methodInfo);
			invokerInfo.Usage++;
			return invokerInfo.Factory.CreateInvoker(target, methodInfo);
		}

		public static R Convert<P, R>(P value)
		{
			// This code is going to use a function matching the delegate Func<P, R> to do the conversion (as from the types 
			IConverter<P, R> converter = Converter.Instance as IConverter<P, R>;
			return converter.Convert(value);
		}

#if false
		// Conversion is done with a faster path now, however this might be needed if we want to do a dynamic conversion with types not known ahead of times.
		public static R ConvertSameTypes<P, R>(P value)
		{
			Debug.Assert(typeof(P) == typeof(R));
			return SameTypeConverter<P, R>.Convert(value);
		}

		public static R ConvertDifferentTypes<P, R>(P value)
		{
			Debug.Assert(typeof(P) != typeof(R));
			TypeCode paramTypeCode = Type.GetTypeCode(typeof(P));
			TypeCode returnTypeCode = Type.GetTypeCode(typeof(R));
			ConverterFactoryBase converterFactory = sFactories[(int)paramTypeCode, (int)returnTypeCode];
			if (converterFactory != null)
			{
				Converter<P, R> converter = (Converter<P, R>)converterFactory.GetConverterDelegate();
				return converter(value);
			}
			else
			{
				Console.WriteLine("PropertySet - Conversion not supported from '" + typeof(P).FullName + "' to '" + typeof(R).FullName + "'");
				throw new NotSupportedException();		// For the moment, we don't handle this conversion, add it to the matrix
			}
		}

		static class SameTypeConverter<P, R>
		{
			static Converter<P, R> sConverter;

			public static R Convert(P param)
			{
				if (sConverter == null)
				{
					// First time, initializes the cache.
					// And this code works because Converter<P, P> and Converter<P, R> are the same types.
					Converter<P, P> converterP = ConvertMethod;
					sConverter = (Converter<P, R>)(Delegate)converterP;

				}
				return sConverter(param);
			}

			static P ConvertMethod(P param)
			{
				return param;
			}
		}
#endif

		private static InvokerInfo GetInvokerInfo(MethodInfo methodInfo)
		{
			// First, we see if we can find the corresponding InvokerInfo directly by the MethodInfo
			InvokerInfo invokerInfo;
			MethodSignature methodSignature;
			if (sInvokerInfoByMethodInfo.TryGetValue(methodInfo, out invokerInfo))
			{
#if RECREATE_DEFAULT_INVOKER
				if ((invokerInfo.Factory is DefaultInvokerFactory) && (RecreateDefaultInvokerEveryNUsages != 0) && ((invokerInfo.Usage % RecreateDefaultInvokerEveryNUsages) == 0))
				{
					// If we are using the default -slow- factory, once in a while we are bubbling it up to the user so (s)he can detect the issue
					// and provide implementation for the biggest offenders.

					// Set it here as we are by-passing the signature look-up
					methodSignature = new MethodSignature(methodInfo);
				}
				else
#endif
				{
					return invokerInfo;
				}
			}
			else
			{
				// We did not find with the method info, let's try with the signature
				methodSignature = new MethodSignature(methodInfo);
				if (sInvokerInfoByMethodSignature.TryGetValue(methodSignature, out invokerInfo))
				{
#if RECREATE_DEFAULT_INVOKER
					if ((invokerInfo.Factory is DefaultInvokerFactory) && (RecreateDefaultInvokerEveryNUsages != 0) && ((invokerInfo.Usage % RecreateDefaultInvokerEveryNUsages) == 0))
					{
						// If we are using the default -slow- factory, once in a while we are bubbling it up to the user so (s)he can detect the issue
						// and provide implementation for the biggest offenders.
					}
					else
#endif
					{
						// Add it to the MethodInfo cache for next time
						sInvokerInfoByMethodInfo.Add(methodInfo, invokerInfo);
						return invokerInfo;
					}
				}
			}

			// We could not find the invoker factory from the MethodInfo, nor a method with a similar signature
			// Before we use a generic (and slow) implementation, let's see if the user wants to give us a specific implementation

			InvokerFactoryBase factory = null;
			if (CreateInvokerFactoryFromMethodSignature != null)
			{
				// The user registered an invoker factory creation from the method signature
				// This is faster than string based signature, no string building, more code to write on the user side though.
#if RECREATE_DEFAULT_INVOKER
				int currentUsage = (invokerInfo != null) ? invokerInfo.Usage : 1;
#else
				const int currentUsage = 1;
#endif
				factory = CreateInvokerFactoryFromMethodSignature(methodSignature, currentUsage);
			}

			if (factory == null)
			{
				// If we reached here, it means that we could not get a specific factory.
				// We are going to have to use a default factory then...
				factory = new DefaultInvokerFactory();
			}

#if RECREATE_DEFAULT_INVOKER
			if (invokerInfo == null)
			{
				invokerInfo = new InvokerInfo(factory);
			}
			else
			{
				invokerInfo.Factory = factory;		// We want to preserve the usage counter
			}
#else
			invokerInfo = new InvokerInfo(factory);
#endif
			// We do not use Add() as methodInfo / methodSignature might already be cached if we just re-force the creation of the invoker in RECREATE_DEFAULT_INVOKER mode
			sInvokerInfoByMethodInfo[methodInfo] = invokerInfo;			
			sInvokerInfoByMethodSignature[methodSignature] = invokerInfo;
			return invokerInfo;
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

		abstract class ParamConverter<FromT, ToT>
		{
			public abstract Converter<FromT, ToT> GetConverter();
		}

		abstract class ConverterFactoryBase
		{
			public abstract object CreateSetConverter(object target, MethodInfo methodInfo);
			public abstract object CreateGetConverter(object target, MethodInfo methodInfo);
			public abstract Delegate GetConverterDelegate();

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

		class ConverterFactory<GetterT, SetterT, ConverterT, FromT, ToT> : ConverterFactoryBase
			where GetterT : InvokerThenConverter<FromT, ToT>, new()
			where SetterT : ConverterThenInvoker<FromT, ToT>, new()
			where ConverterT : ParamConverter<FromT, ToT>, new ()
		{
			// The various cached info
			static MethodInfo getConverterMethodInfo;
			static MethodInfo setConverterMethodInfo;
			static Converter<FromT, ToT> converter;

			public override object CreateGetConverter(object target, MethodInfo methodInfo)
			{
				return CreateGetConverterInternal<GetterT, FromT, ToT>(ref getConverterMethodInfo, target, methodInfo);
			}

			public override object CreateSetConverter(object target, MethodInfo methodInfo)
			{
				return CreateSetConverterInternal<SetterT, FromT, ToT>(ref setConverterMethodInfo, target, methodInfo);
			}

			public override Delegate GetConverterDelegate ()
			{
				if (converter == null)
				{
					ConverterT t = new ConverterT();
					converter = t.GetConverter();
				}
				return converter;
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

		class ConverterIntToUint : ParamConverter<int, uint>
		{
			public override Converter<int, uint> GetConverter ()
			{
				return Convert;
			}

			public static uint Convert(int value)
			{
				return (uint)value;
			}
		}

		class ConverterThenInvokerIntToDouble : ConverterThenInvoker<int, double>
		{
			public override void ConvertThenInvoke(int value) { mActionTo((double)value); }
		}

		class InvokerThenConverterIntToDouble : InvokerThenConverter<int, double>
		{
			public override double InvokeThenConvert() { return (double)mFuncFrom(); }
		}

		class ConverterIntToDouble : ParamConverter<int, double>
		{
			public override Converter<int, double> GetConverter()
			{
				return Convert;
			}

			public static double Convert(int value)
			{
				return (double)value;
			}
		}

		class ConverterThenInvokerIntToBool : ConverterThenInvoker<int, bool>
		{
			public override void ConvertThenInvoke(int value) { mActionTo(value != 0); }
		}

		class InvokerThenConverterIntToBool : InvokerThenConverter<int, bool>
		{
			public override bool InvokeThenConvert() { return (mFuncFrom() != 0); }
		}

		class ConverterIntToBool : ParamConverter<int, bool>
		{
			public override Converter<int, bool> GetConverter()
			{
				return Convert;
			}

			public static bool Convert(int value)
			{
				return (value != 0);
			}
		}

		class ConverterThenInvokerIntToString : ConverterThenInvoker<int, string>
		{
			public override void ConvertThenInvoke(int value) { mActionTo(value.ToString()); }
		}

		class InvokerThenConverterIntToString : InvokerThenConverter<int, string>
		{
			public override string InvokeThenConvert() { return mFuncFrom().ToString(); }
		}

		class ConverterIntToString : ParamConverter<int, string>
		{
			public override Converter<int, string> GetConverter()
			{
				return Convert;
			}

			public static string Convert(int value)
			{
				return value.ToString();
			}
		}


		class ConverterThenInvokerUintToInt : ConverterThenInvoker<uint, int>
		{
			public override void ConvertThenInvoke(uint value)	{ mActionTo((int)value); }
		}

		class InvokerThenConverterUintToInt : InvokerThenConverter<uint, int>
		{
			public override int InvokeThenConvert() { return (int)mFuncFrom(); }
		}

		class ConverterUintToInt : ParamConverter<uint, int>
		{
			public override Converter<uint, int> GetConverter()
			{
				return Convert;
			}

			public static int Convert(uint value)
			{
				return (int)value;
			}
		}

		class ConverterThenInvokerUintToDouble : ConverterThenInvoker<uint, double>
		{
			public override void ConvertThenInvoke(uint value) { mActionTo((double)value); }
		}

		class InvokerThenConverterUintToDouble : InvokerThenConverter<uint, double>
		{
			public override double InvokeThenConvert() { return (double)mFuncFrom(); }
		}

		class ConverterUintToDouble : ParamConverter<uint, double>
		{
			public override Converter<uint, double> GetConverter()
			{
				return Convert;
			}

			public static double Convert(uint value)
			{
				return (double)value;
			}
		}

		class ConverterThenInvokerUintToBool : ConverterThenInvoker<uint, bool>
		{
			public override void ConvertThenInvoke(uint value) { mActionTo(value != 0); }
		}

		class InvokerThenConverterUintToBool : InvokerThenConverter<uint, bool>
		{
			public override bool InvokeThenConvert() { return (mFuncFrom() != 0); }
		}

		class ConverterUintToBool : ParamConverter<uint, bool>
		{
			public override Converter<uint, bool> GetConverter()
			{
				return Convert;
			}

			public static bool Convert(uint value)
			{
				return (value != 0);
			}
		}

		class ConverterThenInvokerUintToString : ConverterThenInvoker<uint, string>
		{
			public override void ConvertThenInvoke(uint value) { mActionTo(value.ToString()); }
		}

		class InvokerThenConverterUintToString : InvokerThenConverter<uint, string>
		{
			public override string InvokeThenConvert() { return mFuncFrom().ToString(); }
		}

		class ConverterUintToString : ParamConverter<uint, string>
		{
			public override Converter<uint, string> GetConverter()
			{
				return Convert;
			}

			public static string Convert(uint value)
			{
				return value.ToString();
			}
		}

		class ConverterThenInvokerDoubleToInt : ConverterThenInvoker<double, int>
		{
			public override void ConvertThenInvoke(double value)	{ mActionTo((int)value); }
		}

		class InvokerThenConverterDoubleToInt : InvokerThenConverter<double, int>
		{
			public override int InvokeThenConvert() { return (int)mFuncFrom(); }
		}

		class ConverterDoubleToInt : ParamConverter<double, int>
		{
			public override Converter<double, int> GetConverter()
			{
				return Convert;
			}

			public static int Convert(double value)
			{
				return (int)value;
			}
		}

		class ConverterThenInvokerDoubleToUint : ConverterThenInvoker<double, uint>
		{
			public override void ConvertThenInvoke(double value) { mActionTo((uint)value); }
		}

		class InvokerThenConverterDoubleToUint : InvokerThenConverter<double, uint>
		{
			public override uint InvokeThenConvert() { return (uint)mFuncFrom(); }
		}

		class ConverterDoubleToUint : ParamConverter<double, uint>
		{
			public override Converter<double, uint> GetConverter()
			{
				return Convert;
			}

			public static uint Convert(double value)
			{
				return (uint)value;
			}
		}

		class ConverterThenInvokerDoubleToBool : ConverterThenInvoker<double, bool>
		{
			public override void ConvertThenInvoke(double value) { mActionTo(value != 0.0); }
		}

		class InvokerThenConverterDoubleToBool : InvokerThenConverter<double, bool>
		{
			public override bool InvokeThenConvert() { return (mFuncFrom() != 0.0); }
		}

		class ConverterDoubleToBool : ParamConverter<double, bool>
		{
			public override Converter<double, bool> GetConverter()
			{
				return Convert;
			}

			public static bool Convert(double value)
			{
				return (value != 0.0);
			}
		}

		class ConverterThenInvokerDoubleToString : ConverterThenInvoker<double, string>
		{
			public override void ConvertThenInvoke(double value) { mActionTo(value.ToString()); }
		}

		class InvokerThenConverterDoubleToString : InvokerThenConverter<double, string>
		{
			public override string InvokeThenConvert() { return mFuncFrom().ToString(); }
		}

		class ConverterDoubleToString : ParamConverter<double, string>
		{
			public override Converter<double, string> GetConverter()
			{
				return Convert;
			}

			public static string Convert(double value)
			{
				return value.ToString();
			}
		}

		// From bool
		class ConverterThenInvokerBoolToInt : ConverterThenInvoker<bool, int>
		{
			public override void ConvertThenInvoke(bool value)	{ mActionTo(value ? 1 : 0); }
		}

		class InvokerThenConverterBoolToInt : InvokerThenConverter<bool, int>
		{
			public override int InvokeThenConvert() { return (mFuncFrom() ? 1 : 0); }
		}

		class ConverterBoolToInt : ParamConverter<bool, int>
		{
			public override Converter<bool, int> GetConverter()
			{
				return Convert;
			}

			public static int Convert(bool value)
			{
				return value ? 1 : 0;
			}
		}

		class ConverterThenInvokerBoolToUint : ConverterThenInvoker<bool, uint>
		{
			public override void ConvertThenInvoke(bool value) { mActionTo(value ? 1u : 0u); }
		}

		class InvokerThenConverterBoolToUint : InvokerThenConverter<bool, uint>
		{
			public override uint InvokeThenConvert() { return mFuncFrom() ? 1u : 0u; }
		}

		class ConverterBoolToUint : ParamConverter<bool, uint>
		{
			public override Converter<bool, uint> GetConverter()
			{
				return Convert;
			}

			public static uint Convert(bool value)
			{
				return value ? 1u : 0u;
			}
		}

		class ConverterThenInvokerBoolToDouble : ConverterThenInvoker<bool, double>
		{
			public override void ConvertThenInvoke(bool value) { mActionTo(value ? 1.0 : 0.0); }
		}

		class InvokerThenConverterBoolToDouble : InvokerThenConverter<bool, double>
		{
			public override double InvokeThenConvert() { return mFuncFrom() ? 1.0 : 0.0; }
		}

		class ConverterBoolToDouble : ParamConverter<bool, double>
		{
			public override Converter<bool, double> GetConverter()
			{
				return Convert;
			}

			public static double Convert(bool value)
			{
				return value ? 1.0 : 0.0;
			}
		}

		class ConverterThenInvokerBoolToString : ConverterThenInvoker<bool, string>
		{
			public override void ConvertThenInvoke(bool value) { mActionTo(value.ToString()); }
		}

		class InvokerThenConverterBoolToString : InvokerThenConverter<bool, string>
		{
			public override string InvokeThenConvert() { return mFuncFrom().ToString(); }
		}

		class ConverterBoolToString : ParamConverter<bool, string>
		{
			public override Converter<bool, string> GetConverter()
			{
				return Convert;
			}

			public static string Convert(bool value)
			{
				return value.ToString();
			}
		}

		// From string
		class ConverterThenInvokerStringToInt : ConverterThenInvoker<string, int>
		{
			public override void ConvertThenInvoke(string value)	{ mActionTo(int.Parse(value)); }
		}

		class InvokerThenConverterStringToInt : InvokerThenConverter<string, int>
		{
			public override int InvokeThenConvert() { return int.Parse(mFuncFrom()); }
		}

		class ConverterStringToInt : ParamConverter<string, int>
		{
			public override Converter<string, int> GetConverter()
			{
				return Convert;
			}

			public static int Convert(string value)
			{
				return int.Parse(value);
			}
		}

		class ConverterThenInvokerStringToUint : ConverterThenInvoker<string, uint>
		{
			public override void ConvertThenInvoke(string value) { mActionTo(uint.Parse(value)); }
		}

		class InvokerThenConverterStringToUint : InvokerThenConverter<string, uint>
		{
			public override uint InvokeThenConvert() { return uint.Parse(mFuncFrom()); }
		}

		class ConverterStringToUint : ParamConverter<string, uint>
		{
			public override Converter<string, uint> GetConverter()
			{
				return Convert;
			}

			public static uint Convert(string value)
			{
				return uint.Parse(value);
			}
		}

		class ConverterThenInvokerStringToDouble : ConverterThenInvoker<string, double>
		{
			public override void ConvertThenInvoke(string value) { mActionTo(double.Parse(value)); }
		}

		class InvokerThenConverterStringToDouble : InvokerThenConverter<string, double>
		{
			public override double InvokeThenConvert() { return double.Parse(mFuncFrom()); }
		}

		class ConverterStringToDouble : ParamConverter<string, double>
		{
			public override Converter<string, double> GetConverter()
			{
				return Convert;
			}

			public static double Convert(string value)
			{
				return double.Parse(value);
			}
		}

		class ConverterThenInvokerStringToBool : ConverterThenInvoker<string, bool>
		{
			public override void ConvertThenInvoke(string value) { mActionTo(bool.Parse(value)); }
		}

		class InvokerThenConverterStringToBool : InvokerThenConverter<string, bool>
		{
			public override bool InvokeThenConvert() { return bool.Parse(mFuncFrom()); }
		}

		class ConverterStringToBool : ParamConverter<string, bool>
		{
			public override Converter<string, bool> GetConverter()
			{
				return Convert;
			}

			public static bool Convert(string value)
			{
				return bool.Parse(value);
			}
		}

		// From object
		class ConverterThenInvokerObjectToInt : ConverterThenInvoker<object, int>
		{
			public override void ConvertThenInvoke(object value)
			{
				int param = ConverterObjectToInt.Convert(value);		// Should be inlined
				mActionTo(param);
			}
		}

		class InvokerThenConverterObjectToInt : InvokerThenConverter<object, int>
		{
			public override int InvokeThenConvert()
			{
				object value = mFuncFrom();
				return ConverterObjectToInt.Convert(value);				// Should be inlined
			}
		}

		class ConverterObjectToInt : ParamConverter<object, int>
		{
			public override Converter<object, int> GetConverter()
			{
				return Convert;
			}

			public static int Convert(object value)
			{
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
					return System.Convert.ToInt32(value);
				}
			}
		}

		class ConverterThenInvokerObjectToUint : ConverterThenInvoker<object, uint>
		{
			public override void ConvertThenInvoke(object value)
			{
				uint param = ConverterObjectToUint.Convert(value);		// Should be inlined
				mActionTo(param);
			}
		}

		class InvokerThenConverterObjectToUint : InvokerThenConverter<object, uint>
		{
			public override uint InvokeThenConvert()
			{
				object value = mFuncFrom();
				return ConverterObjectToUint.Convert(value);			// Should be inlined
			}
		}

		class ConverterObjectToUint : ParamConverter<object, uint>
		{
			public override Converter<object, uint> GetConverter()
			{
				return Convert;
			}

			public static uint Convert(object value)
			{
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
					return System.Convert.ToUInt32(value);
				}
			}
		}

		class ConverterThenInvokerObjectToDouble : ConverterThenInvoker<object, double>
		{
			public override void ConvertThenInvoke(object value)
			{
				double param = ConverterObjectToDouble.Convert(value);
				mActionTo(param);
			}
		}

		class InvokerThenConverterObjectToDouble : InvokerThenConverter<object, double>
		{
			public override double InvokeThenConvert()
			{
				object value = mFuncFrom();
				return ConverterObjectToDouble.Convert(value);		// Should be inlined
			}
		}

		class ConverterObjectToDouble : ParamConverter<object, double>
		{
			public override Converter<object, double> GetConverter()
			{
				return Convert;
			}

			public static double Convert(object value)
			{
				// Change this to use the common convert method?
				if (value is double)
				{
					return (double)value;
				}
				else if (value is int)
				{
					return (double)(int)value;
				}
				else if (value is uint)
				{
					return (double)(uint)value;
				}
				else
				{
					return System.Convert.ToDouble(value);
				}
			}
		}

		class ConverterThenInvokerObjectToBool : ConverterThenInvoker<object, bool>
		{
			public override void ConvertThenInvoke(object value)
			{
				bool param = ConverterObjectToBool.Convert(value);		// Should be inlined
				mActionTo(param);
			}
		}

		class InvokerThenConverterObjectToBool : InvokerThenConverter<object, bool>
		{
			public override bool InvokeThenConvert()
			{
				object value = mFuncFrom();
				return ConverterObjectToBool.Convert(value);			// Should be inlined
			}
		}

		class ConverterObjectToBool : ParamConverter<object, bool>
		{
			public override Converter<object, bool> GetConverter()
			{
				return Convert;
			}

			public static bool Convert(object value)
			{
				// Change this to use the common convert method?
				if (value is bool)
				{
					return (bool)value;
				}
				if (value is int)
				{
					return ((int)value != 0);
				}
				else if (value is uint)
				{
					return ((uint)value != 0);
				}
				else if (value is double)
				{
					return ((double)value != 0.0);
				}
				else
				{
					return System.Convert.ToBoolean(value);
				}
			}
		}

		class ConverterThenInvokerObjectToString : ConverterThenInvoker<object, string>
		{
			public override void ConvertThenInvoke(object value)
			{
				mActionTo(value.ToString());
			}
		}

		class InvokerThenConverterObjectToString : InvokerThenConverter<object, string>
		{
			public override string InvokeThenConvert()
			{
				return mFuncFrom().ToString();
			}
		}

		class ConverterObjectToString : ParamConverter<object, string>
		{
			public override Converter<object, string> GetConverter()
			{
				return Convert;
			}

			public static string Convert(object value)
			{
				return value.ToString();
			}
		}


		class ConverterFactoryToFromObject<T> : ConverterFactoryBase
		{
			static MethodInfo getConverterMethodInfo;
			static MethodInfo setConverterMethodInfo;
			static Converter<T, object> converter;

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

			public override Delegate GetConverterDelegate()
			{
				if (converter == null)
				{
					ConverterToObject<T> t = new ConverterToObject<T>();
					converter = t.GetConverter();
				}
				return converter;
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

		class ConverterToObject<T> : ParamConverter<T, object>
		{
			public override Converter<T, object> GetConverter ()
			{
				return Convert;
			}

			public static object Convert(T value)
			{
				return value;
			}
		}
	}

	public class MethodSignature
	{
		public MethodSignature(MethodInfo methodInfo)
		{
			ReturnType = methodInfo.ReturnType;
			ParameterInfo[] parameterInfos = methodInfo.GetParameters();
			int count = parameterInfos.Length;
			if (count != 0)
			{
				ParameterTypes = new Type[count];
				for (int i = 0 ; i < count ; ++i)
				{
					ParameterTypes[i] = parameterInfos[i].ParameterType;
				}
			}
		}

		public MethodSignature(Type returnType, Type[] parameterTypes)
		{
			ReturnType = returnType;
			if ((parameterTypes != null) && (parameterTypes.Length != 0))
			{
				ParameterTypes = parameterTypes;
			}
		}

		Type			ReturnType;
		// TODO: Check if we need to do something different for the variadic parameters
		Type[]			ParameterTypes;				// We make sure that if there is no parameter, ParameterTypes is null

		public override int GetHashCode ()
		{
			int hashCode = ReturnType.GetHashCode();
			if (ParameterTypes != null)
			{
				int count = ParameterTypes.Length;
				for (int i = 0 ; i < count ; ++i)
				{
					hashCode ^= ParameterTypes[i].GetHashCode();
				}
			}
			return hashCode;
		}

		public override bool Equals (object obj)
		{
			MethodSignature otherSignature = obj as MethodSignature;
			if (otherSignature == null)
			{
				return false;
			}
			if (ReturnType != otherSignature.ReturnType)
			{
				return false;
			}

			// ParameterTypes should be both bull, or both not null to consider comparing them side by side.
			if (ParameterTypes == null)
			{
				return (otherSignature.ParameterTypes == null);
			}
			if (otherSignature.ParameterTypes == null)
			{
				return false;
			}

			int count = ParameterTypes.Length;
			if (count != otherSignature.ParameterTypes.Length)
			{
				return false;
			}
			for (int i = 0 ; i < count ; ++i)
			{
				if (ParameterTypes[i] != otherSignature.ParameterTypes[i])
				{
					return false;
				}
			}
			return true;
		}

		public string GetInvokerFactorySignature()
		{
			if (mSignatureAsString == null)
			{
				sBuilder.Length = 0;
				bool isFunc = (ReturnType != typeof(void));
				if (isFunc)
				{
					sBuilder.Append("InvokerFFactory");
				}
				else
				{
					sBuilder.Append("InvokerAFactory");
				}

				if ((ParameterTypes != null) || isFunc)
				{
					sBuilder.Append('<');
					int count = 0;
					if (ParameterTypes != null)
					{
						count = ParameterTypes.Length;
						for (int i = 0 ; i < count ; ++i)
						{
							if (i != 0)
							{
								sBuilder.Append(", ");
							}
							sBuilder.Append(ParameterTypes[i].FullName);
						}
					}

					if (isFunc)
					{
						if (count != 0)
						{
							sBuilder.Append(", ");
						}
						sBuilder.Append(ReturnType.FullName);
					}
					sBuilder.Append('>');
				}
				mSignatureAsString = sBuilder.ToString();
			}
			return mSignatureAsString;
		}

		string mSignatureAsString;
		static StringBuilder sBuilder = new StringBuilder();		// let's keep one builder around so there is less allocation needed at runtime
	}

	public interface IConverter<ToT>
	{
		ToT ConvertFromObject(object value);
	}

	public interface IConverter<FromT, ToT>
	{
		ToT Convert(FromT value);
	}

	/// <summary>
	/// Class enabling conversion of type T1 to type T2 (using the interface IConverter<T1, T2> on the Converter.Instance.
	/// 
	/// Due to the large number of interfaces, it might be better to use different objects to reduce the cost of interface lookup.
	/// Also, the conversion to string might be overkill as the compiler could generate the toString() IL instead.
	/// </summary>
	public class Converter :	IConverter<int, int>, IConverter<int, uint>, IConverter<int, double>, IConverter<int, bool>, IConverter<int, string>,
								IConverter<uint, int>, IConverter<uint, uint>, IConverter<uint, double>, IConverter<uint, bool>, IConverter<uint, string>,
								IConverter<double, int>, IConverter<double, uint>, IConverter<double, double>, IConverter<double, bool>, IConverter<double, string>,
								IConverter<bool, int>,  IConverter<bool, uint>, IConverter<bool, double>, IConverter<bool, bool>, IConverter<bool, string>,
								IConverter<string, int>, IConverter<string, uint>, IConverter<string, double>, IConverter<string, bool>, IConverter<string, string>,
								IConverter<int>, IConverter<uint>, IConverter<double>, IConverter<bool>, IConverter<string>, IConverter<object>
	{
		public static Converter Instance = new Converter();

		int IConverter<int, int>.Convert(int value)
		{
			return value;
		}

		uint IConverter<int, uint>.Convert(int value)
		{
			return (uint)value;
		}

		double IConverter<int, double>.Convert(int value)
		{
			return (double)value;
		}

		bool IConverter<int, bool>.Convert(int value)
		{
			return (value != 0);
		}

		string IConverter<int, string>.Convert(int value)
		{
			return value.ToString();
		}

		int IConverter<uint, int>.Convert(uint value)
		{
			return (int)value;
		}

		uint IConverter<uint, uint>.Convert(uint value)
		{
			return value;
		}

		double IConverter<uint, double>.Convert(uint value)
		{
			return (double)value;
		}

		bool IConverter<uint, bool>.Convert(uint value)
		{
			return (value != 0);
		}

		string IConverter<uint, string>.Convert(uint value)
		{
			return value.ToString();
		}

		int IConverter<double, int>.Convert(double value)
		{
			return (int)value;
		}

		uint IConverter<double, uint>.Convert(double value)
		{
			return (uint)value;
		}

		double IConverter<double, double>.Convert(double value)
		{
			return (double)value;
		}

		bool IConverter<double, bool>.Convert(double value)
		{
			return (value != 0.0);
		}

		string IConverter<double, string>.Convert(double value)
		{
			return value.ToString();
		}

		int IConverter<bool, int>.Convert(bool value)
		{
			return value ? 1 : 0;
		}

		uint IConverter<bool, uint>.Convert(bool value)
		{
			return value ? 1u : 0u;
		}

		double IConverter<bool, double>.Convert(bool value)
		{
			return value ? 1.0 : 0.0;
		}

		bool IConverter<bool, bool>.Convert(bool value)
		{
			return value;
		}

		string IConverter<bool, string>.Convert(bool value)
		{
			return value.ToString();
		}

		int IConverter<string, int>.Convert(string value)
		{
			return int.Parse(value);
		}

		uint IConverter<string, uint>.Convert(string value)
		{
			return uint.Parse(value);
		}

		double IConverter<string, double>.Convert(string value)
		{
			return double.Parse(value);
		}

		bool IConverter<string, bool>.Convert(string value)
		{
			return bool.Parse(value);
		}

		string IConverter<string, string>.Convert(string value)
		{
			return value;
		}

		int IConverter<int>.ConvertFromObject(object value)
		{
			if (value is double)
			{
				return (int)(double)value;
			}
			else if (value is uint)
			{
				return (int)(uint)value;
			}
			else if (value is string)
			{
				return int.Parse((string)value);
			}
			else if (value is bool)
			{
				return (bool)value ? 1 : 0;
			}
			else if (value is int)
			{
				// This is the target type, but it should have been tested already earlier
				// So we test it last (just in case) before we do the slow conversion
				return (int)value;
			}

			return Convert.ToInt32(value);
		}

		uint IConverter<uint>.ConvertFromObject(object value)
		{
			if (value is int)
			{
				return (uint)(int)value;
			}
			else if (value is double)
			{
				return (uint)(double)value;
			}
			else if (value is string)
			{
				return uint.Parse((string)value);
			}
			else if (value is bool)
			{
				return (bool)value ? 1u : 0u;
			}
			else if (value is uint)
			{
				// This is the target type, but it should have been tested already earlier
				// So we test it last (just in case) before we do the slow conversion
				return (uint)value;
			}
			return Convert.ToUInt32(value);
		}

		double IConverter<double>.ConvertFromObject(object value)
		{
			if (value is int)
			{
				return (double)(int)value;
			}
			else if (value is string)
			{
				return double.Parse((string)value);
			}
			else if (value is uint)
			{
				return (double)(uint)value;
			}
			else if (value is bool)
			{
				return (bool)value ? 1.0 : 0.0;
			}
			else if (value is double)
			{
				// This is the target type, but it should have been tested already earlier
				// So we test it last (just in case) before we do the slow conversion
				return (double)value;
			}
			return Convert.ToDouble(value);
		}

		bool IConverter<bool>.ConvertFromObject(object value)
		{
			if (value is int)
			{
				return ((int)value != 0);
			}
			else if (value is double)
			{
				return ((double)value != 0.0);
			}
			else if (value is string)
			{
				return bool.Parse((string)value);
			}
			else if (value is uint)
			{
				return ((uint)value != 0);
			}
			else if (value is bool)
			{
				// This is the target type, but it should have been tested already earlier
				// So we test it last (just in case) before we do the slow conversion
				return (bool)value;
			}
			return Convert.ToBoolean(value);
		}

		string IConverter<string>.ConvertFromObject(object value)
		{
			return value.ToString();
		}

		object IConverter<object>.ConvertFromObject(object value)
		{
			return value;
		}
	}

	public static class Convert<ToT>
	{
		static IConverter<ToT> sInterface;

		static Convert()
		{
			// We cache the cast interface for this type, so we don't have to do a full lookup every time.
			// The hope is that the embedded if test during the static call (to see if static constructor has been called)
			// is faster than the interface cast.
			sInterface = Converter.Instance as IConverter<ToT>;
		}

		public static ToT FromObject(object value)
		{
			if (value is ToT) {
				return (ToT)value;
			}

			if (value == null) {
				return default(ToT);
			}

			// We need to do a conversion
			if (sInterface != null)
			{
				return sInterface.ConvertFromObject(value);
			}
			// We could not find a fast converter for this combination of FromT to ToT.
			// We assume these are various classes and structs (and not primitive types)
			// We don't have other choice than boxing and cast - for classes it should be pretty quick
			return (ToT)value;
		}
	}


	public static class Convert<FromT, ToT>
	{
		static IConverter<FromT, ToT> sInterface;

		static Convert()
		{
			// We cache the cast interface for this type, so we don't have to do a full lookup every time.
			// The hope is that the embedded if test during the static call (to see if static constructor has been called)
			// is faster than the interface cast.
			sInterface = Converter.Instance as IConverter<FromT, ToT>;
		}

		public static ToT From(FromT value)
		{
			if (sInterface != null)
			{
				return sInterface.Convert(value);
			}
			// We could not find a fast converter for this combination of FromT to ToT.
			// We assume these are various classes and structs (and not primitive types)
			// We don't have other choice than boxing and cast - for classes it should be pretty quick
			object boxedValue = value;
			return (ToT)boxedValue;
		}
	}
}

