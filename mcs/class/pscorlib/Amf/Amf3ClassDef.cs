// Amf3ClassDef.cs
//
// Copyright (c) 2009 Chris Howie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Amf
{
    public class Amf3ClassDef : IEquatable<Amf3ClassDef>
    {
		// registered class info (type, callbacks, etcs)
		public class ClassInfo
		{
			public System.Type				Type;
			public Amf3ObjectConstructor 	Constructor;
			public Amf3ObjectSerializer		Serializer;
			public Amf3ObjectDeserializer 	Deserializer;
			public string[]				 	DeserializerOrder;
		};

        public static readonly Amf3ClassDef Anonymous =
            new Amf3ClassDef("", new string[0], true, false);

		public readonly string[] 				Properties;
		public readonly string 	 				Name;
		public readonly bool 					Dynamic;
		public readonly bool 					Externalizable;
		public int[] 							PropertyRemapTable;
		public ClassInfo Info
		{
			get 
			{
				if (mInfo == null) {
					mInfo = GetOrCreateClassInfo(Name);
				}
				return mInfo;
			}
		}
		private ClassInfo mInfo;

		public Amf3ClassDef(string name, string[] properties, bool dynamic = false, bool externalizable = false)
        {
            if (dynamic && externalizable)
                throw new ArgumentException("AMF classes cannot be both dynamic and externalizable");

            Name = name;
            Properties = properties;
            Dynamic = dynamic;
            Externalizable = externalizable;
        }

		public int GetPropertyIndex(string name)
		{
			for (int i=0; i < Properties.Length; i++) {
				if (Properties[i] == name) {
					return i;
				}
			}
			return -1;
		}

		public object CreateInstance()
		{
			// get class info
			ClassInfo info = this.Info;

			if (info.Constructor != null) {
				// invoke provided constructor delegate if we have one
				return info.Constructor();
			}

			if (info.Type == null) {
				// no type registered? use an expando object 
				return new PlayScript.Expando.ExpandoObject();
			}

			// construct object using reflection (slower)

			// First, we look the default constrcutor
			ConstructorInfo constructor = info.Type.GetConstructor(Type.EmptyTypes);
			if (constructor != null)
			{
				return constructor.Invoke(null);
			}

			// If there was no default constructor, use a constructor that only has default values
			ConstructorInfo[] allConstructors = info.Type.GetConstructors();
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

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^
                (Dynamic ? 1 : 0) ^
                (Externalizable ? 2 : 0);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Amf3ClassDef);
        }

        public bool Equals(Amf3ClassDef other)
        {
            if (other == null)
                return false;

            if (object.ReferenceEquals(this, other))
                return true;

            if (Name             != other.Name ||
                Dynamic          != other.Dynamic ||
                Externalizable   != other.Externalizable ||
			    Properties.Length != other.Properties.Length)
                return false;

			for (int i = 0; i < Properties.Length; i++)
            {
                if (Properties[i] != other.Properties[i])
                    return false;
            }

            return true;
        }

		//
		// class registration 
		//

		// gets system type associated with class alias
		public static System.Type GetClassType(string aliasName)
		{
			ClassInfo info = null;
			if (!sClassInfo.TryGetValue(aliasName, out info)) {
				return null;
			}
			return info.Type;
		}

		// registers a system type to be associated with a class alias
		public static void RegisterClassType(string aliasName, System.Type type) {
			var info = GetOrCreateClassInfo(aliasName);
			info.Type = type;
		}

		// registers a constructor method to be associated with a class alias
		public static void RegisterClassConstructor(string aliasName, Amf3ObjectConstructor func) {
			var info = GetOrCreateClassInfo(aliasName);
			info.Constructor = func;
		}

		// registers a serializer (amf writer) method to be associated with a class alias
		public static void RegisterClassSerializer(string aliasName, Amf3ObjectSerializer func) {
			var info = GetOrCreateClassInfo(aliasName);
			info.Serializer = func;
		}

		// registers a deserializer (amf reader) method to be associated with a class alias
		public static void RegisterClassDeserializer(string aliasName, Amf3ObjectDeserializer func, string[] propertyOrder = null) {
			var info = GetOrCreateClassInfo(aliasName);
			info.Deserializer = func;
			info.DeserializerOrder = propertyOrder;
		}

		// register all classes in assembly that have the Amf3Serializable attribute
		public static void RegisterAllClassesInAssembly(Assembly assembly)
		{
			foreach (var type in assembly.GetTypes()) {
				var attr = type.GetCustomAttribute<Amf3SerializableAttribute>();
				if (attr != null) { 
					RegisterClassType(attr.ClassName, type);
				}
			}
		}

		// register all classes in the current domain that have the Amf3Serializable attribute
		public static void RegisterAllClasses()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				RegisterAllClassesInAssembly(assembly);
			}
		}

		private static ClassInfo GetOrCreateClassInfo(string aliasName)
		{
			lock (sClassInfo)
			{
				ClassInfo info;
				if (!sClassInfo.TryGetValue(aliasName, out info))
				{
					info = new ClassInfo();
					sClassInfo.Add(aliasName, info);
				}
				return info;
			}
		}

		// the last writer to write this object
		internal Amf3Writer				mWriter;
		// the id associated with this object
		internal int 					mId;

		// class info registration
		private static readonly Dictionary<string, ClassInfo> sClassInfo = new Dictionary<string, ClassInfo>();
    }
}
