//
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayScript;

namespace Amf
{
	// the default (but slow) serializer that uses reflection
	public class ReflectionSerializer : IAmf3Serializer
	{
		#region IAmfSerializer implementation

		public object NewInstance(Amf3ClassDef classDef)
		{
			// First, we look the default constrcutor
			ConstructorInfo constructor = mType.GetConstructor(Type.EmptyTypes);
			if (constructor != null)
			{
				return constructor.Invoke(null);
			}

			// If there was no default constructor, use a constructor that only has default values
			ConstructorInfo[] allConstructors = mType.GetConstructors();
			foreach (ConstructorInfo oneConstructor in allConstructors)
			{
				ParameterInfo[] parameters = oneConstructor.GetParameters();
				if (parameters.Length == 0) {
					// Why did we not get this with the default constructor?
					// In any case, handle the case gracefully
				} else if (parameters[0].IsOptional) {
					// First parameter is optional, so all others are too, this constructor is good enough
				} else {
					// We can't use this constructor, try the next one
					continue;
				}

				object[] arguments = new object[parameters.Length];
				for (int i = 0 ; i < parameters.Length ; ++i) {
					arguments[i] = parameters[i].DefaultValue;
				}

				return oneConstructor.Invoke(arguments);
			}

			// Did we miss something?
			throw new NotSupportedException();
		}

		public IList NewVector(uint num, bool isFixed)
		{
			// We use IList so it works for all types, regardless of objectTypeName (cast works because _root.Vector<> implements IList)
			IList vector = (IList)Activator.CreateInstance(mVectorType, num, isFixed);
			return vector;
		}

		public void   WriteObject(Amf3Writer writer, object obj)
		{
			if (obj.GetType() != mType)
				throw new Exception("Serializer type mismatch");

			// begin write
			writer.WriteObjectHeader(mClassDef);

			// write all fields using reflection
			foreach (var field in mFieldList){
				// get field value and write it
				object value = field.GetValue(obj);
				writer.Write(value);
			}

			// read all properties using reflection
			foreach (var property in mPropertyList){
				// get property value and write it
				object value = property.GetValue(obj, null);
				writer.Write(value);
			}
		}

		public void ReadObject(Amf3Reader reader, object obj)
		{
			// see if object implements IAmf3Readable, if so, use it instead of reflection
			var serializable = obj as IAmf3Readable;
			if (serializable != null) {
				// read using custom method (fast)
				serializable.Serialize(reader);
				return;
			} 

			// read using reflection (slow)
			// begin read
			reader.ReadObjectHeader(mClassDef);

			// read all fields using reflection
			foreach (var field in mFieldList){
				// read value
				Variant value = reader.ReadAsVariant();
				if (value.IsDefined) {
					field.SetValue(obj, value.AsType(field.FieldType));
				}
			}

			// read all properties using reflection
			foreach (var property in mPropertyList){
				// read value
				Variant value = reader.ReadAsVariant();
				if (value.IsDefined) {
					property.SetValue(obj, value.AsType(property.PropertyType), null);
				}
			}
		}
		#endregion

		public ReflectionSerializer(string alias, System.Type type, bool addFields = true, bool addProperties = true)
		{
			if (alias == null || type == null)
				throw new ArgumentNullException();

			mType  = type;
			mVectorType = typeof(_root.Vector<>).MakeGenericType(new Type[1] {mType});

			var properties = new List<string>();

			// get all instance fields of type (public or private)
			mFieldList = new List<FieldInfo>();
			if (addFields) {
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))	{
					mFieldList.Add(field);
					properties.Add(field.Name);
				}
			}

			// get all instance properties of type (public or private)
			mPropertyList = new List<PropertyInfo>();
			if (addProperties) {
				foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))	{
					mPropertyList.Add(prop);
					properties.Add(prop.Name);
				}
			}

			// create class definition from member list
			// this defines the property ordering
			mClassDef = new Amf3ClassDef(alias, properties.ToArray());
		}

		private readonly Amf3ClassDef		mClassDef;			// class definition for this serializer
		private readonly System.Type		mType;				// type we are able to serialize
		private readonly System.Type		mVectorType;		// vector type
		private readonly List<FieldInfo>	mFieldList;			// cache of reflection fields for this type
		private readonly List<PropertyInfo>	mPropertyList;		// cache of reflection properties for this type
	};
}
