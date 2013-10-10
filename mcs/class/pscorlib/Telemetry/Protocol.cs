using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Amf;
using _root;
using flash.utils;

namespace Telemetry.Protocol
{
		[Amf3Serializable(".value")]
		public class Value : IAmf3Serializable
		{
			public string name; 
			public object value; // multiple types found: string double bool int Rect Memory_objectAllocation Memory_deallocation ByteArray Vector<uint> BitmapDataRegion Stage3D_requestContext3D Sampler_sample Context3D_configureBackBuffer Context3D_clear Context3D_setDepthTest Context3D_setScissorRectangle Context3D_setBlendFactors Memory_reallocation Context3D_createProgram Program3D_upload Context3D_setProgram Context3D_setCulling Context3D_setProgramConstantsDouble Context3D_createTexture Texture3D_upload Context3D_setTextureAt Context3D_createVertexBuffer VertexBuffer3D_uploadDouble Context3D_setVertexBufferAt Context3D_setProgramConstants Context3D_createIndexBuffer IndexBuffer3D_upload32 Context3D_drawTriangles Context3D_present 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(value);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out name);
				reader.Read(out value);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".value", new string[] {"name", "value"} );
		}

		[Amf3Serializable(".span")]
		public class Span : IAmf3Serializable
		{
			public string name; 
			public int span; 
			public int delta; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(span);
				writer.Write(delta);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out name);
				reader.Read(out span);
				reader.Read(out delta);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".span", new string[] {"name", "span", "delta"} );
		}

		[Amf3Serializable(".spanValue")]
		public class SpanValue : IAmf3Serializable
		{
			public string name; 
			public int span; 
			public int delta; 
			public object value; // multiple types found: string Region int BitmapDataRegion 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(span);
				writer.Write(delta);
				writer.Write(value);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out name);
				reader.Read(out span);
				reader.Read(out delta);
				reader.Read(out value);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".spanValue", new string[] {"name", "span", "delta", "value"} );
		}

		[Amf3Serializable(".rect")]
		public class Rect : IAmf3Serializable
		{
			public int xmin; 
			public int xmax; 
			public int ymin; 
			public int ymax; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(xmin);
				writer.Write(xmax);
				writer.Write(ymin);
				writer.Write(ymax);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out xmin);
				reader.Read(out xmax);
				reader.Read(out ymin);
				reader.Read(out ymax);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".rect", new string[] {"xmin", "xmax", "ymin", "ymax"} );
		}

		[Amf3Serializable(".region")]
		public class Region : IAmf3Serializable
		{
			public int xmin; 
			public int xmax; 
			public int ymin; 
			public int ymax; 
			public object name; // multiple types found: object string 
			public object symbolname; // multiple types found: object string 
			public bool modified; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(xmin);
				writer.Write(xmax);
				writer.Write(ymin);
				writer.Write(ymax);
				writer.Write(name);
				writer.Write(symbolname);
				writer.Write(modified);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out xmin);
				reader.Read(out xmax);
				reader.Read(out ymin);
				reader.Read(out ymax);
				reader.Read(out name);
				reader.Read(out symbolname);
				reader.Read(out modified);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".region", new string[] {"xmin", "xmax", "ymin", "ymax", "name", "symbolname", "modified"} );
		}

		[Amf3Serializable(".memory.objectAllocation")]
		public class Memory_objectAllocation : IAmf3Serializable
		{
			public int time; 
			public int id; 
			public int size; 
			public int stackid; 
			public string type; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(time);
				writer.Write(id);
				writer.Write(size);
				writer.Write(stackid);
				writer.Write(type);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out time);
				reader.Read(out id);
				reader.Read(out size);
				reader.Read(out stackid);
				reader.Read(out type);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".memory.objectAllocation", new string[] {"time", "id", "size", "stackid", "type"} );
		}

		[Amf3Serializable(".memory.deallocation")]
		public class Memory_deallocation : IAmf3Serializable
		{
			public int time; 
			public int id; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(time);
				writer.Write(id);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out time);
				reader.Read(out id);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".memory.deallocation", new string[] {"time", "id"} );
		}

		[Amf3Serializable(".time")]
		public class Time : IAmf3Serializable
		{
			public string name; 
			public int delta; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(name);
				writer.Write(delta);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out name);
				reader.Read(out delta);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".time", new string[] {"name", "delta"} );
		}

		[Amf3Serializable(".bitmapDataRegion")]
		public class BitmapDataRegion : IAmf3Serializable
		{
			public int xmin; 
			public int xmax; 
			public int ymin; 
			public int ymax; 
			public int id; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(xmin);
				writer.Write(xmax);
				writer.Write(ymin);
				writer.Write(ymax);
				writer.Write(id);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out xmin);
				reader.Read(out xmax);
				reader.Read(out ymin);
				reader.Read(out ymax);
				reader.Read(out id);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".bitmapDataRegion", new string[] {"xmin", "xmax", "ymin", "ymax", "id"} );
		}

		[Amf3Serializable("Stage3D_requestContext3D")]
		public class Stage3D_requestContext3D : IAmf3Serializable
		{
			public int mode; 
			public int profile; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(mode);
				writer.Write(profile);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out mode);
				reader.Read(out profile);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Stage3D_requestContext3D", new string[] {"mode", "profile"} );
		}

		[Amf3Serializable("Sampler_sample")]
		public class Sampler_sample : IAmf3Serializable
		{
			public int time; 
			public int numticks; 
			public Vector<double> ticktimes; 
			public Vector<uint> callstack; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(time);
				writer.Write(numticks);
				writer.Write(ticktimes);
				writer.Write(callstack);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out time);
				reader.Read(out numticks);
				reader.Read(out ticktimes);
				reader.Read(out callstack);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Sampler_sample", new string[] {"time", "numticks", "ticktimes", "callstack"} );
		}

		[Amf3Serializable("Context3D_configureBackBuffer")]
		public class Context3D_configureBackBuffer : IAmf3Serializable
		{
			public int width; 
			public int height; 
			public int antialias; 
			public bool depthstencil; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(width);
				writer.Write(height);
				writer.Write(antialias);
				writer.Write(depthstencil);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out width);
				reader.Read(out height);
				reader.Read(out antialias);
				reader.Read(out depthstencil);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_configureBackBuffer", new string[] {"width", "height", "antialias", "depthstencil"} );
		}

		[Amf3Serializable("Context3D_clear")]
		public class Context3D_clear : IAmf3Serializable
		{
			public double red; 
			public double green; 
			public double blue; 
			public double alpha; 
			public double depth; 
			public int stencil; 
			public double mask; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(red);
				writer.Write(green);
				writer.Write(blue);
				writer.Write(alpha);
				writer.Write(depth);
				writer.Write(stencil);
				writer.Write(mask);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out red);
				reader.Read(out green);
				reader.Read(out blue);
				reader.Read(out alpha);
				reader.Read(out depth);
				reader.Read(out stencil);
				reader.Read(out mask);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_clear", new string[] {"red", "green", "blue", "alpha", "depth", "stencil", "mask"} );
		}

		[Amf3Serializable("Context3D_setDepthTest")]
		public class Context3D_setDepthTest : IAmf3Serializable
		{
			public bool depthMask; 
			public int passCompareMode; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(depthMask);
				writer.Write(passCompareMode);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out depthMask);
				reader.Read(out passCompareMode);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setDepthTest", new string[] {"depthMask", "passCompareMode"} );
		}

		[Amf3Serializable("Context3D_setScissorRectangle")]
		public class Context3D_setScissorRectangle : IAmf3Serializable
		{
			public int x; 
			public int y; 
			public int w; 
			public int h; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(x);
				writer.Write(y);
				writer.Write(w);
				writer.Write(h);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out x);
				reader.Read(out y);
				reader.Read(out w);
				reader.Read(out h);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setScissorRectangle", new string[] {"x", "y", "w", "h"} );
		}

		[Amf3Serializable("Context3D_setBlendFactors")]
		public class Context3D_setBlendFactors : IAmf3Serializable
		{
			public int sourceFactor; 
			public int destFactor; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(sourceFactor);
				writer.Write(destFactor);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out sourceFactor);
				reader.Read(out destFactor);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setBlendFactors", new string[] {"sourceFactor", "destFactor"} );
		}

		[Amf3Serializable(".memory.reallocation")]
		public class Memory_reallocation : IAmf3Serializable
		{
			public int time; 
			public int id; 
			public int size; 
			public int stackid; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(time);
				writer.Write(id);
				writer.Write(size);
				writer.Write(stackid);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out time);
				reader.Read(out id);
				reader.Read(out size);
				reader.Read(out stackid);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef(".memory.reallocation", new string[] {"time", "id", "size", "stackid"} );
		}

		[Amf3Serializable("Context3D_createProgram")]
		public class Context3D_createProgram : IAmf3Serializable
		{
			public int resultId; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(resultId);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out resultId);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_createProgram", new string[] {"resultId"} );
		}

		[Amf3Serializable("Program3D_upload")]
		public class Program3D_upload : IAmf3Serializable
		{
			public int programId; 
			public ByteArray vertexProgram; 
			public ByteArray fragmentProgram; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(programId);
				writer.Write(vertexProgram);
				writer.Write(fragmentProgram);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out programId);
				reader.Read(out vertexProgram);
				reader.Read(out fragmentProgram);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Program3D_upload", new string[] {"programId", "vertexProgram", "fragmentProgram"} );
		}

		[Amf3Serializable("Context3D_setProgram")]
		public class Context3D_setProgram : IAmf3Serializable
		{
			public int programId; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(programId);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out programId);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setProgram", new string[] {"programId"} );
		}

		[Amf3Serializable("Context3D_setCulling")]
		public class Context3D_setCulling : IAmf3Serializable
		{
			public int triangleFaceToCull; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(triangleFaceToCull);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out triangleFaceToCull);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setCulling", new string[] {"triangleFaceToCull"} );
		}

		[Amf3Serializable("Context3D_setProgramConstantsDouble")]
		public class Context3D_setProgramConstantsDouble : IAmf3Serializable
		{
			public int programType; 
			public int firstRegister; 
			public ByteArray data; 
			public int numRegisters; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(programType);
				writer.Write(firstRegister);
				writer.Write(data);
				writer.Write(numRegisters);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out programType);
				reader.Read(out firstRegister);
				reader.Read(out data);
				reader.Read(out numRegisters);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setProgramConstantsDouble", new string[] {"programType", "firstRegister", "data", "numRegisters"} );
		}

		[Amf3Serializable("Context3D_createTexture")]
		public class Context3D_createTexture : IAmf3Serializable
		{
			public int width; 
			public int height; 
			public int format; 
			public bool optimizeForRenderToTexture; 
			public int streamingLevels; 
			public int resultId; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(width);
				writer.Write(height);
				writer.Write(format);
				writer.Write(optimizeForRenderToTexture);
				writer.Write(streamingLevels);
				writer.Write(resultId);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out width);
				reader.Read(out height);
				reader.Read(out format);
				reader.Read(out optimizeForRenderToTexture);
				reader.Read(out streamingLevels);
				reader.Read(out resultId);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_createTexture", new string[] {"width", "height", "format", "optimizeForRenderToTexture", "streamingLevels", "resultId"} );
		}

		[Amf3Serializable("Texture3D_upload")]
		public class Texture3D_upload : IAmf3Serializable
		{
			public int textureId; 
			public ByteArray srcdata; 
			public int srcbytesperline; 
			public int srcformat; 
			public int srccompressedformat; 
			public int srcw; 
			public int srch; 
			public bool inverted; 
			public int miplevel; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(textureId);
				writer.Write(srcdata);
				writer.Write(srcbytesperline);
				writer.Write(srcformat);
				writer.Write(srccompressedformat);
				writer.Write(srcw);
				writer.Write(srch);
				writer.Write(inverted);
				writer.Write(miplevel);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out textureId);
				reader.Read(out srcdata);
				reader.Read(out srcbytesperline);
				reader.Read(out srcformat);
				reader.Read(out srccompressedformat);
				reader.Read(out srcw);
				reader.Read(out srch);
				reader.Read(out inverted);
				reader.Read(out miplevel);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Texture3D_upload", new string[] {"textureId", "srcdata", "srcbytesperline", "srcformat", "srccompressedformat", "srcw", "srch", "inverted", "miplevel"} );
		}

		[Amf3Serializable("Context3D_setTextureAt")]
		public class Context3D_setTextureAt : IAmf3Serializable
		{
			public int sampler; 
			public int textureId; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(sampler);
				writer.Write(textureId);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out sampler);
				reader.Read(out textureId);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setTextureAt", new string[] {"sampler", "textureId"} );
		}

		[Amf3Serializable("Context3D_createVertexBuffer")]
		public class Context3D_createVertexBuffer : IAmf3Serializable
		{
			public int count; 
			public int dwordsPerVertex; 
			public int resultId; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(count);
				writer.Write(dwordsPerVertex);
				writer.Write(resultId);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out count);
				reader.Read(out dwordsPerVertex);
				reader.Read(out resultId);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_createVertexBuffer", new string[] {"count", "dwordsPerVertex", "resultId"} );
		}

		[Amf3Serializable("VertexBuffer3D_uploadDouble")]
		public class VertexBuffer3D_uploadDouble : IAmf3Serializable
		{
			public int vertexBufferId; 
			public ByteArray source; 
			public int startVertex; 
			public int countVertices; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(vertexBufferId);
				writer.Write(source);
				writer.Write(startVertex);
				writer.Write(countVertices);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out vertexBufferId);
				reader.Read(out source);
				reader.Read(out startVertex);
				reader.Read(out countVertices);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("VertexBuffer3D_uploadDouble", new string[] {"vertexBufferId", "source", "startVertex", "countVertices"} );
		}

		[Amf3Serializable("VertexBuffer3D_upload")]
		public class VertexBuffer3D_upload : IAmf3Serializable
		{
			public int vertexBufferId; 
			public ByteArray source; 
			public int startVertex; 
			public int countVertices; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(vertexBufferId);
				writer.Write(source);
				writer.Write(startVertex);
				writer.Write(countVertices);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out vertexBufferId);
				reader.Read(out source);
				reader.Read(out startVertex);
				reader.Read(out countVertices);
			}
			#endregion

			public static Amf3ClassDef ClassDef = new Amf3ClassDef("VertexBuffer3D_upload", new string[] {"vertexBufferId", "source", "startVertex", "countVertices"} );
		}

		[Amf3Serializable("Context3D_setVertexBufferAt")]
		public class Context3D_setVertexBufferAt : IAmf3Serializable
		{
			public int index; 
			public int vertexBufferId; 
			public int bufferOffset; 
			public int format; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(index);
				writer.Write(vertexBufferId);
				writer.Write(bufferOffset);
				writer.Write(format);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out index);
				reader.Read(out vertexBufferId);
				reader.Read(out bufferOffset);
				reader.Read(out format);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setVertexBufferAt", new string[] {"index", "vertexBufferId", "bufferOffset", "format"} );
		}

		[Amf3Serializable("Context3D_setProgramConstants")]
		public class Context3D_setProgramConstants : IAmf3Serializable
		{
			public int programType; 
			public int firstRegister; 
			public ByteArray data; 
			public int numRegisters; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(programType);
				writer.Write(firstRegister);
				writer.Write(data);
				writer.Write(numRegisters);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out programType);
				reader.Read(out firstRegister);
				reader.Read(out data);
				reader.Read(out numRegisters);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_setProgramConstants", new string[] {"programType", "firstRegister", "data", "numRegisters"} );
		}

		[Amf3Serializable("Context3D_createIndexBuffer")]
		public class Context3D_createIndexBuffer : IAmf3Serializable
		{
			public int count; 
			public int resultId; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(count);
				writer.Write(resultId);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out count);
				reader.Read(out resultId);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_createIndexBuffer", new string[] {"count", "resultId"} );
		}

		[Amf3Serializable("IndexBuffer3D_upload32")]
		public class IndexBuffer3D_upload32 : IAmf3Serializable
		{
			public int indexBufferId; 
			public ByteArray source; 
			public int offset; 
			public int count; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(indexBufferId);
				writer.Write(source);
				writer.Write(offset);
				writer.Write(count);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out indexBufferId);
				reader.Read(out source);
				reader.Read(out offset);
				reader.Read(out count);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("IndexBuffer3D_upload32", new string[] {"indexBufferId", "source", "offset", "count"} );
		}

		[Amf3Serializable("Context3D_drawTriangles")]
		public class Context3D_drawTriangles : IAmf3Serializable
		{
			public int indexBufferId; 
			public int firstIndex; 
			public int numTriangles; 

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
				writer.Write(indexBufferId);
				writer.Write(firstIndex);
				writer.Write(numTriangles);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
				reader.Read(out indexBufferId);
				reader.Read(out firstIndex);
				reader.Read(out numTriangles);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_drawTriangles", new string[] {"indexBufferId", "firstIndex", "numTriangles"} );
		}

		[Amf3Serializable("Context3D_present")]
		public class Context3D_present : IAmf3Serializable
		{

			#region IAmf3Serializable implementation
			public void Serialize(Amf3Writer writer) {
				writer.WriteObjectHeader(ClassDef);
			}

			public void Serialize(Amf3Reader reader) {
				reader.ReadObjectHeader(ClassDef);
			}

			#endregion
			public static Amf3ClassDef ClassDef = new Amf3ClassDef("Context3D_present", new string[] {} );
		}
}

