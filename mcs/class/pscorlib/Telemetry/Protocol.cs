using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Amf;

namespace Telemetry
{
	public static class Protocol
	{
		//
		// These are the telemetry classes that are serialized to and from AMF
		//

		// describes a single name/value pair
		public class Value : IAmf3Serializable
		{
			public string name;
			public object value;

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(value);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".value", new string[] { "name", "value" }, false, false);
		}

		// describes a span of time
		public class Span : IAmf3Serializable
		{
			public string name;		// name of span
			public int    span;		// length of span (in microseconds)
			public int    delta;	// time since last span (in microseconds)

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(span);
				writer.Write(delta);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".span", new string[] { "name", "span", "delta" }, false, false);
		}

		// describes a span of time with a value
		public class SpanValue : IAmf3Serializable
		{
			public string name;		// name of span
			public int    span;		// length of span (in microseconds)
			public int    delta;	// time since last span (in microseconds)
			public object value;	// value

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(span);
				writer.Write(delta);
				writer.Write(value);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".spanValue", new string[] { "name", "span", "delta", "value" }, false, false);
		}

		// describes a named time stamp
		public class Time : IAmf3Serializable
		{
			public string name;		// name of timestamp 
			public int    delta;	// time since last span (in microseconds) 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(delta);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".time", new string[] { "name", "delta" }, false, false);
		}

		public class Rect : IAmf3Serializable 
		{
			public int xmin;
			public int xmax;
			public int ymin;
			public int ymax;

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(xmin);
				writer.Write(xmax);
				writer.Write(ymin);
				writer.Write(ymax);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".rect", new string[] { "xmin", "xmax", "ymin", "ymax" }, false, false);
		}

		public class Region : IAmf3Serializable 
		{
			public int xmin;
			public int xmax;
			public int ymin;
			public int ymax;
			public string name;
			public string symbolname;
			public bool modified;

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(xmin);
				writer.Write(xmax);
				writer.Write(ymin);
				writer.Write(ymax);
				writer.Write(name);
				writer.Write(symbolname);
				writer.Write(modified);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".region", new string[] { "xmin", "xmax", "ymin", "ymax", "name", "symbolname", "modified" }, false, false);
		}

		// this class is used for object alloc tracking through ".memory.newObject"
		public class Memory_objectAllocation : IAmf3Serializable 
		{
			public int time;
			public int id;
			public int size;
			public int stackid;
			public string type;

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(time);
				writer.Write(id);
				writer.Write(size);
				writer.Write(stackid);
				writer.Write(type);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".memory.objectAllocation", new string[] { "time", "id", "size", "stackid", "type"}, false, false);
		}

		// this class is used for object alloc tracking through ".memory.deleteObject"
		public class Memory_deallocation : IAmf3Serializable 
		{
			public int time;
			public int id;

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(time);
				writer.Write(id);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".memory.deallocation", new string[] { "time", "id"}, false, false);
		}


		// this class is used for sampler data through ".sampler.sample"
		public class Sampler_sampler : IAmf3Serializable 
		{
			public int time;
			public int numticks;
			public uint[] ticktimes;
			public uint[] callstack;

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer)
			{
				writer.WriteObjectHeader(ClassDef);
				writer.Write(time);
				writer.Write(numticks);
				writer.Write(ticktimes);
				writer.Write(callstack);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Sampler_sampler", new string[] { "time", "numticks", "ticktimes", "callstack"}, false, false);
		}

	}
}

