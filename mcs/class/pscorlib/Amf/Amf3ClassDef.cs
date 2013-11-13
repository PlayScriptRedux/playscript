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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Amf
{
    public sealed class Amf3ClassDef : IEquatable<Amf3ClassDef>
    {
        public static readonly Amf3ClassDef Anonymous =
            new Amf3ClassDef("", new string[0], true, false);

		public readonly string[]			Properties;
		public readonly string				Name;
		public readonly bool				Dynamic;
		public readonly bool				Externalizable;
		public IAmf3Serializer				Serializer;			// cached serializer to use for this class definition
		private  string 					mHash;				// cached class hash (backs Hash property)
		private  Amf3Reader 				mReaderPool;		// singly linked list of readers in pool

		private Dictionary<string, int>		mLookup;			// cached name -> index lookup  (could use custom hash table for this)
		private string 						mLastLookupKey;		// cached last key that was looked up
		private int 						mLastLookupIndex;

		internal Amf3Writer					mWriter;			// the last writer to write this object
		internal int 						mId;				// the id associated with this object

		public Amf3ClassDef(string name, string[] properties, bool dynamic = false, bool externalizable = false)
        {
            if (dynamic && externalizable)
                throw new ArgumentException("AMF classes cannot be both dynamic and externalizable");

            Name = name;
            Properties = properties;
            Dynamic = dynamic;
            Externalizable = externalizable;
        }

		// this is a unique hash for this class definition (contains class name and property names)
		// the hash is generated the first time this is accessed
		public string Hash
		{
			get 
			{
				if (mHash == null) {
					// hash properties as one string
					var sb = new System.Text.StringBuilder();
					sb.Append(Name);
					if (Dynamic) sb.Append('*');
					if (Externalizable) sb.Append('>');
					sb.Append(':');
					bool delimiter = false;
					foreach (var prop in Properties) {
						if (delimiter) sb.Append(',');
						sb.Append(prop);
						delimiter = true;
					}
					mHash = sb.ToString();
				}
				return mHash;
			}
		}

		// creates a property reader that can read ordered or named properties from an amf stream 
		public Amf3Reader CreatePropertyReader()
		{
			var reader = mReaderPool;
			if (reader == null) {
				// create new property reader if pool is empty
				reader = new Amf3Reader(this);
				return reader;
			}

			// use next property reader from pool
			mReaderPool = mReaderPool.NextReader;
			return reader;
		}

		// release a property reader back to the pool
		public void ReleasePropertyReader(Amf3Reader reader)
		{
			reader.EndRead();
			// add reader to pool
			reader.NextReader = mReaderPool;
			mReaderPool = reader;
		}

		public int GetPropertyIndex(string name)
		{
			// do a quick comparison against the last lookup that weas performed
			if (mLastLookupKey == name) {
				return mLastLookupIndex;
			}

			if (mLookup == null) {
				// build lookup table
				mLookup = new Dictionary<string, int>(Properties.Length);
				for (int i=0; i < Properties.Length; i++) {
					mLookup[Properties[i]] = i;
				}
			}

			mLastLookupKey = name;

			int value;
			if (mLookup.TryGetValue(name, out value)) {
				// found key
				mLastLookupIndex = value;
				return value;
			} else {
				// did not find key
				mLastLookupIndex = -1;
				return -1;
			}
		}

		// enumerates all properties of this class definition
		public IEnumerator GetKeyEnumerator()
		{
			// enumerate class properties
			for (int i=0; i < Properties.Length; i++) {
				string key = Properties[i];
				// cache last key since its likely to be read next
				mLastLookupKey    = key;
				mLastLookupIndex  = i;
				yield return key;
			}
		}

		// enumerates all properties of this class definition
		public IEnumerator GetKeyEnumerator(IEnumerable<string> dynamicProperties)
		{
			// enumerate dynamic properties
			foreach (var key in dynamicProperties) {
				// invalidate last key since its not in this class definition
				mLastLookupKey    = key;
				mLastLookupIndex  = -1;
				yield return key;
			}

			// enumerate class properties
			for (int i=0; i < Properties.Length; i++) {
				string key = Properties[i];
				// cache last key since its likely to be read next
				mLastLookupKey    = key;
				mLastLookupIndex  = i;
				yield return key;
			}
		}


        public override int GetHashCode()
        {
            return Hash.GetHashCode();
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

			return this.Hash == other.Hash;
        }

		public override string ToString()
		{
			return string.Format("[Amf3ClassDef: {0}]", Hash);
		}

		//
		// class registration 
		//


		// gets system type associated with class alias
		public static System.Type GetTypeFromAlias(string aliasName, bool searchRuntimeTypes = false)
		{
			lock (sAliasToType)
			{
				Type type = null;
				sAliasToType.TryGetValue(aliasName, out type);
				if (type != null) {
					return type;
				}
			}

			if (searchRuntimeTypes) {
				// search all assemblies
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
					var type = assembly.GetType(aliasName);
					if (type != null) {
						// register and return it
						RegisterTypeAlias(type.FullName, type);
						return type;
					}
				}
			}
			return null;
		}

		// gets the alias associated with a system type
		public static string GetAliasFromType(System.Type type)
		{
			lock (sTypeToAlias)
			{
				string alias = null;
				sTypeToAlias.TryGetValue(type, out alias);
				return alias;
			}
		}

		// gets registered serializer associated with class alias
		public static IAmf3Serializer GetSerializerFromAlias(string aliasName)
		{
			lock (sAliasToSerializer)
			{
				IAmf3Serializer serializer = null;
				sAliasToSerializer.TryGetValue(aliasName, out serializer);
				return serializer;
			}
		}

		// get all registered types
		public static KeyValuePair<string, Type>[] GetAllRegisteredTypes()
		{
			lock (sAliasToType)
			{
				return sAliasToType.ToArray();
			}
		}

		// registers a system type to be associated with a class alias
		public static void RegisterTypeAlias(string aliasName, System.Type type) 
		{
			lock (sTypeToAlias)
			{
				sTypeToAlias[type] = aliasName;
			}

			lock (sAliasToType)
			{
				sAliasToType[aliasName] = type;
			}

//			Console.WriteLine("AMF: Registered class alias {0} => {1}", aliasName, type.FullName);
		}

		// register all serializers in assembly
		public static void RegisterAllSerializersInAssembly(Assembly assembly)
		{
			foreach (var type in assembly.GetTypes()) {
				var attr = Attribute.GetCustomAttribute(type, typeof(Amf3SerializableAttribute)) as Amf3SerializableAttribute;
				if (attr != null) { 
					// register alias
					RegisterTypeAlias(attr.ClassName, type);
				}

				var serializerAttr = Attribute.GetCustomAttribute(type, typeof(Amf3SerializerAttribute)) as Amf3SerializerAttribute;
				if (serializerAttr != null) { 
					// register alias
					RegisterTypeAlias(serializerAttr.ClassName, serializerAttr.TargetType);

					// create instance of serializer
					var serializer = Activator.CreateInstance(type) as IAmf3Serializer;
					if (serializer != null) {
						// register serializer
						RegisterSerializer(serializerAttr.ClassName, serializer);
					}
				}
			}
		}

		// register all serializers in the current domain
		public static void RegisterAllSerializers()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				RegisterAllSerializersInAssembly(assembly);
			}
		}

		public static void RegisterSerializer(string aliasName, IAmf3Serializer serializer)
		{
			lock (sAliasToSerializer)
			{
				sAliasToSerializer[aliasName] = serializer;
			}
		}

		static Amf3ClassDef()
		{
			// register all AMF serializers we can find
			RegisterAllSerializers();
		}

		// serializer registration
		private static readonly Dictionary<string, IAmf3Serializer> sAliasToSerializer = new Dictionary<string, IAmf3Serializer>();
		private static readonly Dictionary<Type, string> sTypeToAlias = new Dictionary<Type, string>();
		private static readonly Dictionary<string, Type> sAliasToType = new Dictionary<string, Type>();
    }
}
