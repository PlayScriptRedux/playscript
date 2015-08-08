//
// module.cs: keeps a tree representation of the generated code
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Marek Safar  (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin Inc
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.CompilerServices.SymbolWriter;
using System.Linq;
using Mono.CSharp.JavaScript;
using Mono.CSharp.Cpp;

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	//
	// Module (top-level type) container
	//
	public sealed partial class ModuleContainer : TypeContainer
	{
#if STATIC
		//
		// Compiler generated container for static data
		//
		sealed class StaticDataContainer : CompilerGeneratedContainer
		{
			readonly Dictionary<int, Struct> size_types;
			int fields;

			public StaticDataContainer (ModuleContainer module)
				: base (module, new MemberName ("<PrivateImplementationDetails>" + module.builder.ModuleVersionId.ToString ("B"), Location.Null),
					Modifiers.STATIC | Modifiers.INTERNAL)
			{
				size_types = new Dictionary<int, Struct> ();
			}

			public override void CloseContainer ()
			{
				base.CloseContainer ();

				foreach (var entry in size_types) {
					entry.Value.CloseContainer ();
				}
			}

			public FieldSpec DefineInitializedData (byte[] data, Location loc)
			{
				Struct size_type;
				if (!size_types.TryGetValue (data.Length, out size_type)) {
					//
					// Build common type for this data length. We cannot use
					// DefineInitializedData because it creates public type,
					// and its name is not unique among modules
					//
					size_type = new Struct (this, new MemberName ("$ArrayType=" + data.Length, loc), Modifiers.PRIVATE | Modifiers.COMPILER_GENERATED, null);
					size_type.CreateContainer ();
					size_type.DefineContainer ();

					size_types.Add (data.Length, size_type);

					// It has to work even if StructLayoutAttribute does not exist
					size_type.TypeBuilder.__SetLayout (1, data.Length);
				}

				var name = "$field-" + fields.ToString ("X");
				++fields;
				const Modifiers fmod = Modifiers.STATIC | Modifiers.INTERNAL;
				var fbuilder = TypeBuilder.DefineField (name, size_type.CurrentType.GetMetaInfo (), ModifiersExtensions.FieldAttr (fmod) | FieldAttributes.HasFieldRVA);
				fbuilder.__SetDataAndRVA (data);

				return new FieldSpec (CurrentType, null, size_type.CurrentType, fbuilder, fmod);
			}
		}

		StaticDataContainer static_data;

		//
		// Makes const data field inside internal type container
		//
		public FieldSpec MakeStaticData (byte[] data, Location loc)
		{
			if (static_data == null) {
				static_data = new StaticDataContainer (this);
				static_data.CreateContainer ();
				static_data.DefineContainer ();

				AddCompilerGeneratedClass (static_data);
			}

			return static_data.DefineInitializedData (data, loc);
		}
#endif

		public CharSet? DefaultCharSet;
		public TypeAttributes DefaultCharSetType = TypeAttributes.AnsiClass;

		readonly Dictionary<int, List<AnonymousTypeClass>> anonymous_types;
		readonly Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> array_types;
		readonly Dictionary<TypeSpec, PointerContainer> pointer_types;
		readonly Dictionary<TypeSpec, ReferenceContainer> reference_types;
		readonly Dictionary<TypeSpec, MethodSpec> attrs_cache;
		readonly Dictionary<TypeSpec, AwaiterDefinition> awaiters;

		AssemblyDefinition assembly;
		readonly CompilerContext context;
		readonly RootNamespace global_ns;
		readonly Dictionary<string, RootNamespace> alias_ns;

		ModuleBuilder builder;

		bool has_extenstion_method;

		PredefinedAttributes predefined_attributes;
		PredefinedTypes predefined_types;
		PredefinedMembers predefined_members;

		public Binary.PredefinedOperator[] OperatorsBinaryEqualityLifted;
		public Binary.PredefinedOperator[] OperatorsBinaryLifted;

		static readonly string[] attribute_targets = new string[] { "assembly", "module" };

		public ModuleContainer (CompilerContext context)
			: base (null, MemberName.Null, null, 0)
		{
			this.context = context;

			caching_flags &= ~(Flags.Obsolete_Undetected | Flags.Excluded_Undetected);

			containers = new List<TypeContainer> ();
			anonymous_types = new Dictionary<int, List<AnonymousTypeClass>> ();
			global_ns = new GlobalRootNamespace ();
			alias_ns = new Dictionary<string, RootNamespace> ();
			array_types = new Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> ();
			pointer_types = new Dictionary<TypeSpec, PointerContainer> ();
			reference_types = new Dictionary<TypeSpec, ReferenceContainer> ();
			attrs_cache = new Dictionary<TypeSpec, MethodSpec> ();
			awaiters = new Dictionary<TypeSpec, AwaiterDefinition> ();
		}

		#region Properties

		internal Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> ArrayTypesCache {
			get {
				return array_types;
			}
		}

		//
		// Cache for parameter-less attributes
		//
		internal Dictionary<TypeSpec, MethodSpec> AttributeConstructorCache {
			get {
				return attrs_cache;
			}
		}

 		public override AttributeTargets AttributeTargets {
 			get {
 				return AttributeTargets.Assembly;
 			}
		}

		public ModuleBuilder Builder {
			get {
				return builder;
			}
		}

		public override CompilerContext Compiler {
			get {
				return context;
			}
		}

		public int CounterAnonymousTypes { get; set; }

		public AssemblyDefinition DeclaringAssembly {
			get {
				return assembly;
			}
		}

		internal DocumentationBuilder DocumentationBuilder {
			get; set;
		}

		public override string DocCommentHeader {
			get {
				throw new NotSupportedException ();
			}
		}

		public Evaluator Evaluator {
			get; set;
		}

		public bool HasDefaultCharSet {
			get {
				return DefaultCharSet.HasValue;
			}
		}

		public bool HasExtensionMethod {
			get {
				return has_extenstion_method;
			}
			set {
				has_extenstion_method = value;
			}
		}

		public bool HasTypesFullyDefined {
			get; set;
		}

		//
		// Returns module global:: namespace
		//
		public RootNamespace GlobalRootNamespace {
		    get {
		        return global_ns;
		    }
		}

		public override ModuleContainer Module {
			get {
				return this;
			}
		}

		internal Dictionary<TypeSpec, PointerContainer> PointerTypesCache {
			get {
				return pointer_types;
			}
		}

		internal PredefinedAttributes PredefinedAttributes {
			get {
				return predefined_attributes;
			}
		}

		internal PredefinedMembers PredefinedMembers {
			get {
				return predefined_members;
			}
		}

		internal PredefinedTypes PredefinedTypes {
			get {
				return predefined_types;
			}
		}

		internal Dictionary<TypeSpec, ReferenceContainer> ReferenceTypesCache {
			get {
				return reference_types;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}

		#endregion

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return;
				}
				foreach (var cont in containers) {
					if (visitor.Continue && visitor.Depth >= VisitDepth.Namespaces && cont != null)
						cont.Accept (visitor);
				}
			}
		}

		public void AddAnonymousType (AnonymousTypeClass type)
		{
			List<AnonymousTypeClass> existing;
			if (!anonymous_types.TryGetValue (type.Parameters.Count, out existing))
			if (existing == null) {
				existing = new List<AnonymousTypeClass> ();
				anonymous_types.Add (type.Parameters.Count, existing);
			}

			existing.Add (type);
		}

		public void AddAttribute (Attribute attr, IMemberContext context)
		{
			attr.AttachTo (this, context);

			if (attributes == null) {
				attributes = new Attributes (attr);
				return;
			}

			attributes.AddAttribute (attr);
		}

		public override void AddTypeContainer (TypeContainer tc)
		{
			AddTypeContainerMember (tc);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Target == AttributeTargets.Assembly) {
				assembly.ApplyAttributeBuilder (a, ctor, cdata, pa);
				return;
			}

			if (a.Type == pa.DefaultCharset) {
				switch (a.GetCharSetValue ()) {
				case CharSet.Ansi:
				case CharSet.None:
					break;
				case CharSet.Auto:
					DefaultCharSet = CharSet.Auto;
					DefaultCharSetType = TypeAttributes.AutoClass;
					break;
				case CharSet.Unicode:
					DefaultCharSet = CharSet.Unicode;
					DefaultCharSetType = TypeAttributes.UnicodeClass;
					break;
				default:
					Report.Error (1724, a.Location, "Value specified for the argument to `{0}' is not valid",
						a.GetSignatureForError ());
					break;
				}
			} else if (a.Type == pa.CLSCompliant) {
				Attribute cls = DeclaringAssembly.CLSCompliantAttribute;
				if (cls == null) {
					Report.Warning (3012, 1, a.Location,
						"You must specify the CLSCompliant attribute on the assembly, not the module, to enable CLS compliance checking");
				} else if (DeclaringAssembly.IsCLSCompliant != a.GetBoolean ()) {
					Report.SymbolRelatedToPreviousError (cls.Location, cls.GetSignatureForError ());
					Report.Warning (3017, 1, a.Location,
						"You cannot specify the CLSCompliant attribute on a module that differs from the CLSCompliant attribute on the assembly");
					return;
				}
			}

			builder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		public override void CloseContainer ()
		{
			if (anonymous_types != null) {
				foreach (var atypes in anonymous_types)
					foreach (var at in atypes.Value)
						at.CloseContainer ();
			}

			base.CloseContainer ();
		}

		public TypeBuilder CreateBuilder (string name, TypeAttributes attr, int typeSize)
		{
			return builder.DefineType (name, attr, null, typeSize);
		}

		//
		// Creates alias global namespace
		//
		public RootNamespace CreateRootNamespace (string alias)
		{
			if (alias == global_ns.Alias) {
				RootNamespace.Error_GlobalNamespaceRedefined (Report, Location.Null);
				return global_ns;
			}

			RootNamespace rn;
			if (!alias_ns.TryGetValue (alias, out rn)) {
				rn = new RootNamespace (alias);
				alias_ns.Add (alias, rn);
			}

			return rn;
		}

		public void Create (AssemblyDefinition assembly, ModuleBuilder moduleBuilder)
		{
			this.assembly = assembly;
			builder = moduleBuilder;
		}

		public override bool Define ()
		{
			DefineContainer ();

			if (Compiler.Settings.AutoSeal) {
				AutoSealTypes ();
			}

			ExpandBaseInterfaces ();

			base.Define ();

			ApplyAssemblyAttributes ();

			HasTypesFullyDefined = true;

			return true;
		}

		public override bool DefineContainer ()
		{
			DefineNamespace ();

			return base.DefineContainer ();
		}

		public void EnableRedefinition ()
		{
			is_defined = false;
		}

		private void ApplyAssemblyAttributes ()
		{
			if (OptAttributes != null) {
				foreach (Attribute a in OptAttributes.Attrs) {
					// cannot rely on any resolve-based members before you call Resolve
					if (a.ExplicitTarget != "assembly")
						continue;

					if ((a.Name == "AllowDynamic" || a.Name == "ForbidDynamic") && a.NamedArguments != null && 
					    a.NamedArguments.Count == 1 && a.NamedArguments[0].Expr is StringLiteral) {
						string nsName = (a.NamedArguments [0].Expr as StringLiteral).GetValue() as string;
						Namespace ns = GlobalRootNamespace.GetNamespace (nsName, false);
						if (ns != null) {
							ns.AllowDynamic = (a.Name == "AllowDynamic");
						}
					}
				}
			}

		}

		private class AutoSealVisitor : StructuralVisitor 
		{
			public enum Pass
			{
				/// <summary>
				/// We are going through all classes and methods, properties and indexers and add them to the cache
				/// </summary>
				DiscoverClassesAndMethods,
				/// <summary>
				/// Once the methods are discovered, we set all the virtual types accordingly.
				/// </summary>
				SetVirtualTypes,
				/// <summary>
				/// Once all the virtual types have been discovered, we can promote the FirstAndOnlyVirtual to NotVirtual.
				/// </summary>
				FinalizeModifierFlags,	
			}

			enum VirtualType
			{
				/// <summary>
				/// We know the function is not virtual. It can be inlined.
				/// </summary>
				NotVirtual,
				/// <summary>
				/// This is the first virtual function, and there are overrides. It can't be inlined (unless called explicitly).
				/// </summary>
				FirstVirtual,
				/// <summary>
				/// This is an override of a virtual function. It can't be inlined (unless called explicitly).
				/// </summary>
				OverrideVirtual,
				/// <summary>
				/// This is the first virtual function, and there may or may not be overrides.
				/// It will be changed at some later point to NotVirtual if not overriden, or FirstVirtual if overriden.
				/// </summary>
				FirstAndOnlyVirtual,
				/// <summary>
				/// Placeholder in the method cache during Pass.DiscoverMethods, will be set correctly during Pass.SetVirtualTypes.
				/// </summary>
				Unknown,
			}

			class MethodInfo
			{
				public MethodInfo(MemberCore member)
				{
					Member = member;
				}
				public VirtualType Type
				{
					get
					{
						return type;
					}
					set
					{
						//Console.WriteLine("[Auto-sealing] Setting method {0} virtual type to {1}.", Member.GetSignatureForError(), value.ToString());
						type = value;
					}
				}
				public MemberCore Member;

				private VirtualType type = VirtualType.Unknown;
			}

			HashSet<TypeSpec> baseTypes = new HashSet<TypeSpec>();
			Dictionary<TypeSpec, Dictionary<string, MethodInfo>> methodsByTypes = new Dictionary<TypeSpec, Dictionary<string, MethodInfo>>();
			HashSet<Class> visitedClasses = new HashSet<Class>();
			Pass currentPass = Pass.DiscoverClassesAndMethods;
			bool verbose = false;

			public AutoSealVisitor(bool verbose) 
			{
				AutoVisit = true;
				Depth = VisitDepth.Members;
				this.verbose = verbose;
			}

			public Pass CurrentPass
			{
				get { return currentPass; } set { currentPass = value; visitedClasses.Clear(); }
			}

			public override void Visit (MemberCore member)
			{
				if (member is TypeContainer) {
					var tc = member as TypeContainer;
					foreach (var container in tc.Containers) {
						container.Accept (this);
					}
				}
			}

			public override void Visit (Class c)
			{
				VisitClass(c);
			}

			private void VisitClass(Class c)
			{
				if (visitedClasses.Contains(c)) {
					Skip = true;
					return;
				}
				visitedClasses.Add(c);

				switch (CurrentPass) {
					case Pass.DiscoverClassesAndMethods: {
						TypeSpec baseType = c.BaseType;
						if (baseType != null) {
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Found parent class {0} for class {1}.", baseType.GetSignatureForError(), c.GetSignatureForError());
							}
							baseTypes.Add (baseType);

							if (baseType.MemberDefinition is ImportedTypeDefinition) {
								// The base class is coming from another assembly - It will not be visited as part of this assembly
								// But we still have to get some of its information recursively
								// and visit all the methods

								// TODO: Do the parsing work
							}
						}
						break;
					}

					case Pass.FinalizeModifierFlags:
						// Last class of a hierarchy are auto-sealed if they are not static
						if (IsLeafClass(c.CurrentType) && ((c.ModFlags & Modifiers.STATIC) == 0)) {
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Making class {0} sealed.", c.GetSignatureForError());
							}

							// When we seal here, we get proper compile error, however the class does not seem to be marked as sealed in IL
							//c.ModFlags |= Modifiers.SEALED;
						}
						break;
				}
			}

			/// <summary>
			/// Visits the method.
			/// Create a specific version with a different name to avoid unexpected overrides.
			/// </summary>
			/// <param name="m">The method to visit.</param>
			private void VisitMethod(MemberCore m, bool updateModFlags)
			{
				switch (CurrentPass) {
					case Pass.DiscoverClassesAndMethods:
						AddMethodToCache(m);
						break;
					case Pass.SetVirtualTypes:
						DetermineVirtualState(m);
						break;
					case Pass.FinalizeModifierFlags:
						FinalizeVirtualState(m, updateModFlags);
						break;
				}
			}

			public override void Visit (Method m)
			{
				VisitMethod(m, true);
			}

			public void VisitProperty (PropertyBase p)
			{
				if (p.Get != null)
					VisitMethod(p.Get, false);		// We don't change the state of getter and setter
				if (p.Set != null)
					VisitMethod(p.Set, false);

				//Do we need to update some state on the property?
				switch (CurrentPass) {
					case Pass.DiscoverClassesAndMethods:
						break;

					case Pass.SetVirtualTypes:
						break;

					case Pass.FinalizeModifierFlags: {
						bool hasVirtual = false;
						bool hasOverride = false;
						if (p.Get != null) {
							MethodInfo methodInfo = GetMethodInfoFromCache(p.Get);
							if (methodInfo != null) {
								switch (methodInfo.Type) {
									case VirtualType.NotVirtual:
										if (verbose) {
											Console.WriteLine("[Auto-sealing] get property {0} is not virtual.", p.Get.GetSignatureForError());
										}
										break;
									case VirtualType.FirstVirtual:
										hasVirtual = true;
										if (verbose) {
											Console.WriteLine("[Auto-sealing] get property {0} is first virtual.", p.Get.GetSignatureForError());
										}
										break;
									case VirtualType.OverrideVirtual:
										hasOverride = true;
										if (verbose) {
											Console.WriteLine("[Auto-sealing] get property {0} is override.", p.Get.GetSignatureForError());
										}
										break;
									default:
										if (verbose) {
											Console.WriteLine("[Auto-sealing] Unexpected virtual type in get property {0}.", p.Get.GetSignatureForError());
										}
										break;
								}
							}
						}
						if (p.Set != null) {
							MethodInfo methodInfo = GetMethodInfoFromCache(p.Set);
							if (methodInfo != null) {
								switch (methodInfo.Type) {
									case VirtualType.NotVirtual:
										if (verbose) {
											Console.WriteLine("[Auto-sealing] set property {0} is not virtual.", p.Set.GetSignatureForError());
										}
										break;
									case VirtualType.FirstVirtual:
										hasVirtual = true;
										if (verbose) {
											Console.WriteLine("[Auto-sealing] set property {0} is first virtual.", p.Set.GetSignatureForError());
										}
										break;
									case VirtualType.OverrideVirtual:
										hasOverride = true;
										if (verbose) {
											Console.WriteLine("[Auto-sealing] set property {0} is override.", p.Set.GetSignatureForError());
										}
										break;
									default:
										if (verbose) {
											Console.WriteLine("[Auto-sealing] Unexpected virtual type in set property {0}.", p.Set.GetSignatureForError());
										}
										break;
								}
							}
						}

						if (hasVirtual) {
							p.ModFlags &= ~Modifiers.OVERRIDE;
							p.ModFlags |= Modifiers.VIRTUAL;
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Make property {0} virtual.", p.GetSignatureForError());
							}
						} else if (hasOverride) {
							p.ModFlags &= ~Modifiers.VIRTUAL;
							p.ModFlags |= Modifiers.OVERRIDE;
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Make property {0} override.", p.GetSignatureForError());
							}
						} else {
							p.ModFlags &= ~(Modifiers.VIRTUAL | Modifiers.OVERRIDE);
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Remove virtual and override on property {0}.", p.GetSignatureForError());
							}
						}
						break;
					}
				}
			}

			public override void Visit (Property p)
			{
				VisitProperty(p);
			}

			public override void Visit (Indexer i)
			{
				VisitProperty(i);
			}

			private void AddMethodToCache(MemberCore m)
			{
				TypeSpec containerType = m.Parent.CurrentType;
				Dictionary<string, MethodInfo> listOfMethods;
				if (methodsByTypes.TryGetValue(containerType, out listOfMethods) == false) {
					listOfMethods = new Dictionary<string, MethodInfo>();
					methodsByTypes.Add(containerType, listOfMethods);
				}
				string signature = GetSignature(m);
				MethodInfo methodInfo = new MethodInfo(m);
				listOfMethods[signature] = methodInfo;			// Note that a method can be visited several times
			}

			private MethodInfo GetMethodInfoFromCache(MemberCore m)
			{
				string signature;
				return GetMethodInfoFromCache(m, out signature);
			}

			private MethodInfo GetMethodInfoFromCache(MemberCore m, out string signature)
			{
				signature = "unknown";

				TypeSpec containerType = m.Parent.CurrentType;
				Dictionary<string, MethodInfo> listOfMethods;
				if (methodsByTypes.TryGetValue(containerType, out listOfMethods) == false) {
					return null;
				}

				signature = GetSignature(m);
				MethodInfo methodInfo = listOfMethods[signature];
				if (methodInfo == null) {
					if (verbose) {
						Console.WriteLine("[Auto-sealing] Error when looking for method {0}.", m.GetSignatureForError());
					}
				} else if (methodInfo.Member != m) {
					if (verbose) {
						Console.WriteLine("[Auto-sealing] Error when matching method {0}.", m.GetSignatureForError());
					}
				}
				return methodInfo;
			}

			private MethodInfo GetMethodInfoFromCache(TypeSpec containerType, string signature)
			{
				Dictionary<string, MethodInfo> listOfMethods;
				if (methodsByTypes.TryGetValue(containerType, out listOfMethods) == false) {
					return null;
				}

				MethodInfo methodInfo;
				listOfMethods.TryGetValue(signature, out methodInfo);
				return methodInfo;
			}

			private bool DetermineVirtualState(MemberCore m)
			{
				// We should find the method in the cache
				string signature;
				MethodInfo methodInfo = GetMethodInfoFromCache(m, out signature);
				if (methodInfo != null) {
					return DetermineVirtualState(signature, methodInfo);
				} else {
					return false;
				}
			}

			private bool DetermineVirtualState(string signature, MethodInfo methodInfo)
			{
				if (methodInfo.Type != VirtualType.Unknown) {
					// Has been already determined, no further work needed (parent methods have been scanned too)
					return (methodInfo.Type != VirtualType.NotVirtual);
				}

				bool isVirtual = ((methodInfo.Member.ModFlags & Modifiers.VIRTUAL) != 0);
				if (methodInfo.Member is PropertyBase.PropertyMethod) {
					// If property (or indexer), we also look at the property itself
					isVirtual |= (((PropertyBase.PropertyMethod)(methodInfo.Member)).Property.ModFlags & Modifiers.VIRTUAL) != 0;
				}
				bool isOverride = ((methodInfo.Member.ModFlags & Modifiers.OVERRIDE) != 0);
				if (methodInfo.Member is PropertyBase.PropertyMethod) {
					// If property (or indexer), we also look at the property itself
					isOverride |= (((PropertyBase.PropertyMethod)(methodInfo.Member)).Property.ModFlags & Modifiers.OVERRIDE) != 0;
				}

				if (isVirtual || isOverride) {
					methodInfo.Type = VirtualType.FirstAndOnlyVirtual;		// Initial state if virtual

					// Now we need to recursively go up the base classes, and find methods with the same signature
					// And if there is one and it was already virtual, we need to change the current method to VirtualType.OverrideVirtual.
					// We have to double-check if a class skip a level
					TypeSpec parentType = methodInfo.Member.Parent.CurrentType.BaseType;
					while (parentType != null)	{
						MethodInfo parentMethodInfo = GetMethodInfoFromCache(parentType, signature);
						if (parentMethodInfo != null) {
							// Recurse here, it will go through each base method, one parent class at a time
							bool isParentVirtual = DetermineVirtualState(signature, parentMethodInfo);
							// We should expect the base method to be virtual as this method is virtual
							if (isParentVirtual == false) {
								if (verbose) {
									Console.WriteLine("[Auto-sealing] Error with method {0}. Base method is not virtual, child method is.", parentMethodInfo.Member.GetSignatureForError());
								}
							}
							if (parentMethodInfo.Type == VirtualType.FirstAndOnlyVirtual) {
								// The parent method is actually not the the only virtual in the tree, mark it as top of the tree
								parentMethodInfo.Type = VirtualType.FirstVirtual;
							}

							// But in any case, we know that the current method is overriden (we don't support new, just override)
							methodInfo.Type = VirtualType.OverrideVirtual;
							break;		// No need to continue with all the base types as they have been already parsed
						} else {
							// No parent method, we keep the state FirstAndOnlyVirtual for the moment
							// But we have to go through all parents to make sure

							if (parentType.MemberDefinition is ImportedTypeDefinition) {
								// Due to the lack of full parsing of imported types (to be done later),
								// There is only one case where we rely on the flags of the method.
								// If the base type is imported and the current method is override, we assume that it is true,
								// And up the chain one method was FirstVirtual (but it won't be visited)
								if (isOverride) {
									methodInfo.Type = VirtualType.OverrideVirtual;

									if (verbose) {
										Console.WriteLine("[Auto-sealing] Assume method {0} is override due to imported base class {1}", methodInfo.Member.GetSignatureForError(), parentType.GetSignatureForError());
									}
								}
								// We stop at the first imported type, as we won't get more information going further up the chain
								// as visited types have not been visited
								break;
							}
						}

						parentType = parentType.BaseType;
					}
					return true;
				} else {
					methodInfo.Type = VirtualType.NotVirtual;
					return false;
				}
			}

			private void FinalizeVirtualState(MemberCore m, bool updateModFlags)
			{
				string signature;
				MethodInfo methodInfo = GetMethodInfoFromCache(m, out signature);
				if (methodInfo != null) {
					switch (methodInfo.Type) {
						case VirtualType.Unknown:
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Error with method {0}. Still has an unknown virtual type.", methodInfo.Member.GetSignatureForError());
							}
							break;
						case VirtualType.FirstAndOnlyVirtual:
							// This the first and only virtual, it is as if the method was not virtual at all
							if (updateModFlags) {
								m.ModFlags &= ~Modifiers.VIRTUAL;
								methodInfo.Member.ModFlags &= ~Modifiers.VIRTUAL;
							}
							methodInfo.Type = VirtualType.NotVirtual;
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Remove virtual on {0}", m.GetSignatureForError());
							}
							break;
						case VirtualType.OverrideVirtual:
							// Set the override flag in case it was not set
							if (updateModFlags) {
								m.ModFlags &= ~Modifiers.VIRTUAL;
								m.ModFlags |= Modifiers.OVERRIDE;
							}
							if (verbose) {
								Console.WriteLine("[Auto-sealing] Make override on {0}", m.GetSignatureForError());
							}
							break;
						case VirtualType.FirstVirtual:
							if ((m.Parent.ModFlags & Modifiers.SEALED) != 0) {
								// This case can happen if we could not track correctly the method earlier
								if (verbose) {
									Console.WriteLine("[Auto-sealing] Remove virtual (due to sealed class) on {0}", m.GetSignatureForError());
								}
								m.ModFlags &= ~Modifiers.VIRTUAL;
							}
							break;
					}
				}
			}

			private bool IsLeafClass(TypeSpec type)
			{
				return (!baseTypes.Contains(type) && (type.Modifiers & Modifiers.ABSTRACT) == 0);
			}


			private string GetSignature(MemberCore m)
			{
				// We want the signature without any class info
				signatureBuilder.Length = 0;
				signatureBuilder.Append(m.MemberName.Basename);
				signatureBuilder.Append('(');
				if (m.CurrentTypeParameters != null) {
					bool passedFirstParameter = false;
					foreach (TypeParameterSpec parameter in m.CurrentTypeParameters.Types) {
						if (passedFirstParameter) {
							signatureBuilder.Append(',');
						}
						signatureBuilder.Append(parameter.Name);
					}
				}
				signatureBuilder.Append(')');
				return signatureBuilder.ToString();
			}

			private System.Text.StringBuilder signatureBuilder = new System.Text.StringBuilder();
		}

		private void AutoSealTypes ()
		{
			bool verbose = Compiler.Settings.AutoSealVerbosity;
			var visitor = new AutoSealVisitor (verbose);
			visitor.CurrentPass = AutoSealVisitor.Pass.DiscoverClassesAndMethods;
			if (verbose) {
				Console.WriteLine("[Auto-sealing] Pass {0}.", visitor.CurrentPass);
			}
			this.Accept (visitor);

			visitor.CurrentPass = AutoSealVisitor.Pass.SetVirtualTypes;
			if (verbose) {
				Console.WriteLine("[Auto-sealing] Pass {0}.", visitor.CurrentPass);
			}
			this.Accept (visitor);

			visitor.CurrentPass = AutoSealVisitor.Pass.FinalizeModifierFlags;
			if (verbose) {
				Console.WriteLine("[Auto-sealing] Pass {0}.", visitor.CurrentPass);
			}
			this.Accept (visitor );
		}

		public override void EmitContainer ()
		{
			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (Compiler.Settings.Unsafe && !assembly.IsSatelliteAssembly) {
				var pa = PredefinedAttributes.UnverifiableCode;
				if (pa.IsDefined)
					pa.EmitAttribute (builder);
			}

			foreach (var tc in containers) {
				tc.PrepareEmit ();
			}

			base.EmitContainer ();

			if (Compiler.Report.Errors == 0 && !Compiler.Settings.WriteMetadataOnly)
				VerifyMembers ();

			if (anonymous_types != null) {
				foreach (var atypes in anonymous_types)
					foreach (var at in atypes.Value)
						at.EmitContainer ();
			}
		}

		public override void EmitContainerJs (JsEmitContext jec)
		{
			if (OptAttributes != null)
				OptAttributes.EmitJs (jec);

			foreach (var tc in containers) {
				tc.PrepareEmit ();
			}
			
			base.EmitContainerJs (jec);

			if (Compiler.Report.Errors == 0 && !Compiler.Settings.WriteMetadataOnly)
				VerifyMembers ();
			
			if (anonymous_types != null) {
				foreach (var atypes in anonymous_types)
					foreach (var at in atypes.Value)
						at.EmitContainerJs (jec);
			}
		}

		internal override void GenerateDocComment (DocumentationBuilder builder)
		{
			foreach (var tc in containers)
				tc.GenerateDocComment (builder);
		}

		public AnonymousTypeClass GetAnonymousType (IList<AnonymousTypeParameter> parameters)
		{
			List<AnonymousTypeClass> candidates;
			if (!anonymous_types.TryGetValue (parameters.Count, out candidates))
				return null;

			int i;
			foreach (AnonymousTypeClass at in candidates) {
				for (i = 0; i < parameters.Count; ++i) {
					if (!parameters [i].Equals (at.Parameters [i]))
						break;
				}

				if (i == parameters.Count)
					return at;
			}

			return null;
		}

		//
		// Return container with awaiter definition. It never returns null
		// but all container member can be null for easier error reporting
		//
		public AwaiterDefinition GetAwaiter (TypeSpec type)
		{
			AwaiterDefinition awaiter;
			if (awaiters.TryGetValue (type, out awaiter))
				return awaiter;

			awaiter = new AwaiterDefinition ();

			//
			// Predefined: bool IsCompleted { get; } 
			//
			awaiter.IsCompleted = MemberCache.FindMember (type, MemberFilter.Property ("IsCompleted", Compiler.BuiltinTypes.Bool),
				BindingRestriction.InstanceOnly) as PropertySpec;

			//
			// Predefined: GetResult ()
			//
			// The method return type is also result type of await expression
			//
			awaiter.GetResult = MemberCache.FindMember (type, MemberFilter.Method ("GetResult", 0,
				ParametersCompiled.EmptyReadOnlyParameters, null),
				BindingRestriction.InstanceOnly) as MethodSpec;

			//
			// Predefined: INotifyCompletion.OnCompleted (System.Action)
			//
			var nc = PredefinedTypes.INotifyCompletion;
			awaiter.INotifyCompletion = !nc.Define () || type.ImplementsInterface (nc.TypeSpec, false);

			awaiters.Add (type, awaiter);
			return awaiter;
		}

		public override void GetCompletionStartingWith (string prefix, List<string> results)
		{
			var names = Evaluator.GetVarNames ();
			results.AddRange (names.Where (l => l.StartsWith (prefix)));
		}

		public RootNamespace GetRootNamespace (string name)
		{
			RootNamespace rn;
			alias_ns.TryGetValue (name, out rn);
			return rn;
		}

		public override string GetSignatureForError ()
		{
			return "<module>";
		}

		public Binary.PredefinedOperator[] GetPredefinedEnumAritmeticOperators (TypeSpec enumType, bool nullable)
		{
			TypeSpec underlying;
			Binary.Operator mask = 0;

			if (nullable) {
				underlying = Nullable.NullableInfo.GetEnumUnderlyingType (this, enumType);
				mask = Binary.Operator.NullableMask;
			} else {
				underlying = EnumSpec.GetUnderlyingType (enumType);
			}

			var operators = new[] {
				new Binary.PredefinedOperator (enumType, underlying,
					mask | Binary.Operator.AdditionMask | Binary.Operator.SubtractionMask | Binary.Operator.DecomposedMask, enumType),
				new Binary.PredefinedOperator (underlying, enumType,
					mask | Binary.Operator.AdditionMask | Binary.Operator.SubtractionMask | Binary.Operator.DecomposedMask, enumType),
				new Binary.PredefinedOperator (enumType, mask | Binary.Operator.SubtractionMask, underlying)
			};

			return operators;
		}

		public void InitializePredefinedTypes ()
		{
			predefined_attributes = new PredefinedAttributes (this);
			predefined_types = new PredefinedTypes (this);
			predefined_members = new PredefinedMembers (this);

			OperatorsBinaryEqualityLifted = Binary.CreateEqualityLiftedOperatorsTable (this);
			OperatorsBinaryLifted = Binary.CreateStandardLiftedOperatorsTable (this);
		}

		public override bool IsClsComplianceRequired ()
		{
			return DeclaringAssembly.IsCLSCompliant;
		}

		public Attribute ResolveAssemblyAttribute (PredefinedAttribute a_type)
		{
			Attribute a = OptAttributes.Search ("assembly", a_type);
			if (a != null) {
				a.Resolve ();
			}
			return a;
		}

		public void SetDeclaringAssembly (AssemblyDefinition assembly)
		{
			// TODO: This setter is quite ugly but I have not found a way around it yet
			this.assembly = assembly;
		}
	}
}
