using System;
using System.Collections.Generic;
using System.Reflection;
using Token = YSToken;
using Type = YSToken.TokenType;

//TODO Create Function Scope from Existing function and feed it to FindNameScope

public class YSStateModule	{
	public static string CONSOLE_TAG = "[State]";
	public static bool DEBUG = false;
	public static bool VERBOSE = false;
	/*public enum DataType { Boolean, Number, Text, List, GameObject, Structure, Unknown };*/
	YSInterpreter Context;

	public YSStateModule ()
	{
		Console.WriteLine ("Creating scope...");
		Global_Scope = new ScopeFrame ("", ScopeFrame.FrameTypes.None);
		Scope_Stack = new Stack<ScopeFrame> ();

		TemporaryStorage = new ScopeFrame();
		Console.WriteLine ("Finished Scope Creation...");
	}

	public void SetContext(YSInterpreter context)
	{
		Context = context;
	}

	public IdentityType TranslateTokenTypeToIdentityType(Type current)
	{
		switch (current) {
		case Type.Number:
			return IdentityType.Number;
		case Type.Text:
			return IdentityType.Text;
		case Type.Boolean:
			return IdentityType.Boolean;
		case Type.Structure:
			return IdentityType.Structure;
		default:
			return IdentityType.Unknown;
		}
	}

	public struct Primitives
	{
		public Dictionary<string, Double> Numbers;
		public Dictionary<string, string> Text;
		public Dictionary<string, Boolean> Booleans;

	}

	static Primitives PrepareEmptyPrimitives()
	{
		Primitives p = new Primitives ();
		p.Numbers = new Dictionary<string, double> ();
		p.Booleans = new Dictionary<string, bool> ();
		p.Text = new Dictionary<string, string> ();
		return p;
	}

	public void RegisterPrimitive (Token identity, Token value)
	{
		switch (value.Type) {
		case Type.NumberData:
			double insert_double;
			Double.TryParse (value.Content, out insert_double);
			current_scope.Primitives.Numbers.Add (identity.Content, insert_double);
			break;
		case Type.TextData:
			current_scope.Primitives.Text.Add (identity.Content, value.Content);
			break;
		case Type.Boolean:
			bool insert_bool;
			Boolean.TryParse (value.Content, out insert_bool);
			current_scope.Primitives.Booleans.Add (identity.Content, insert_bool);
			break;
		default:
			Debug ("Cannot resolve the datatype of token " + value.Content);
			break;
		}
	}

	/* Address format:
	 * <type>:<name>
	 * s:<name> - structure
	 * f:<name> - function
	 * l:<name> - loop
	 * u - unknown
	 * 
	 * [feature redacted] ..	go back into parent scope
	 * / 	global scope
	 */
	void FindNameScope(IDPacket packet, out ScopeFrame scope)
	{
		Debug("Attempting to find scope of " + packet.Address + ", name: " + packet.Name);
		if (packet.Address.Length == 0) {
			scope = TemporaryStorage;
			return;
		}
		string[] _path = packet.Address.Split ('/');
		List<string> pathList = new List<string>();
		foreach (string tok in _path)
			if (tok.Length > 0)
				pathList.Add (tok);

		string[] path = pathList.ToArray ();
		scope = Global_Scope;
		int PARENT_COUNTER = 0;
		ScopeFrame[] _SS = Scope_Stack.ToArray ();
		for (int i = 0; i < path.Length; i++) {
			string str = path [i];

			/* -- parent keyword feature has been removed
			 * 
			if (str.Equals (IDPacket.PARENT_TYPE)) {
				scope = _SS [_SS.Length - 1 - ++PARENT_COUNTER];
				continue;
			}
			*/

			string[] addr_parts = str.Split (':');
			string addr_type = addr_parts [0], addr_name = addr_parts [1];

			if (!addr_type.Equals ("s")) {
				Debug ("Searching scope stack...");
				if (_SS [i].Name.Equals (addr_name)) {
					Debug (String.Format ("Found {0} on top of {1}", addr_name, scope.Name));
					scope = _SS [i];
				} else {
					Debug (String.Format ("Didn't find {0} on top of {1}", addr_name, scope.Name));
				}
			} else {
				Debug ("Searching Generics...");
				GenericFrame schild;
				if (scope.Generics.TryGetValue (addr_name, out schild)) {
					Debug ("[Generic] Child " + addr_name + " found");
					scope = StructureFrame.Appropriate (addr_name, schild);
				} else {
					Error (String.Format ("There is no structure named {0} in the scope", addr_name)); 
				}
			}
		}
		Debug ("Exiting Finder");
	}

	public void PutNumber(IDPacket packet, double number)
	{
		Debug (String.Format("Putting number into {0}{1}", packet.Address, packet.Name));
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		Debug (String.Format ("Found scope {0}, type {1}", scope.Name, scope.Type));
		double trash;
		if (TryGetNumber (packet, out trash))
			scope.Primitives.Numbers [packet.Name] = number;
		else
			scope.Primitives.Numbers.Add (packet.Name, number);
		Debug ("Put number");
	}

	public bool TryGetNumber(IDPacket packet, out double value)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		return scope.Primitives.Numbers.TryGetValue (packet.Name, out value);
	}

	public void PutBoolean(IDPacket packet, Boolean boolean)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		bool trash;
		if (TryGetBoolean (packet, out trash))
			scope.Primitives.Booleans [packet.Name] = boolean;
		else
			scope.Primitives.Booleans.Add (packet.Name, boolean);
	}

	public bool TryGetBoolean(IDPacket packet, out Boolean value)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		return scope.Primitives.Booleans.TryGetValue (packet.Name, out value);
	}

	public void PutText(IDPacket packet, string text)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		string trash;
		if (TryGetText (packet, out trash))
			scope.Primitives.Text [packet.Name] = text;
		else
			scope.Primitives.Text.Add (packet.Name, text);
	}

	public bool TryGetText(IDPacket packet, out string value)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		return scope.Primitives.Text.TryGetValue (packet.Name, out value);
	}

	public void PutGeneric(IDPacket packet, GenericFrame structure)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		GenericFrame trash;
		if (TryGetGeneric (packet, out trash))
			scope.Generics [packet.Name] = structure;
		else
			scope.Generics.Add (packet.Name, structure);
	}

	public void PutParseStructure(string name, StructureFrame value)
	{
		if(!current_scope.Generics.ContainsKey(name))
		current_scope.Generics.Add (name, value);
	}

	public bool TryGetGeneric(IDPacket packet, out GenericFrame value)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		return scope.Generics.TryGetValue (packet.Name, out value);
	}

	public bool TryGetParseStructure(string name, out GenericFrame value)
	{
		return current_scope.Generics.TryGetValue (name, out value);
	}

	public void PutFunction(IDPacket packet, FunctionFrame function)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		FunctionFrame trash;
		if (TryGetFunction (packet, out trash))
			scope.Functions [packet.Name] = function;
		else
			scope.Functions.Add (packet.Name, function);
	}

	public void PutParseFunction(string name, FunctionFrame value)
	{
		if(!current_scope.Functions.ContainsKey(name))
			current_scope.Functions.Add (name, value);
	}

	public bool TryGetFunction(IDPacket packet, out FunctionFrame value)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		return scope.Functions.TryGetValue (packet.Name, out value);
	}

	public bool TryGetParseFunction (string name, out FunctionFrame value)
	{
		return current_scope.Functions.TryGetValue (name, out value);
	}

	public void CreateParsePrimitive(string name, IdentityType type)
	{
		Debug ("Adding var name: " + name + " type: " + type + " in " + current_scope.Name + " scope level " + Scope_Stack.Count);
		switch (type) {
		case IdentityType.Number:
			if(!current_scope.Primitives.Numbers.ContainsKey(name))
				current_scope.Primitives.Numbers.Add(name,0);
			break;
		case IdentityType.Boolean:
			if(!current_scope.Primitives.Numbers.ContainsKey(name))
				current_scope.Primitives.Booleans.Add (name, false);
			break;
		case IdentityType.Text:
			if(!current_scope.Primitives.Numbers.ContainsKey(name))
				current_scope.Primitives.Text.Add (name, "");
			break;
		default:
			Console.WriteLine ("Not a primitive type");
			break;
		}
	}

	public void RemoveIdentity(IDPacket packet)
	{
		ScopeFrame scope;
		FindNameScope (packet, out scope);
		switch (packet.Type) {
		case IdentityType.Number:
			scope.Primitives.Numbers.Remove (packet.Name);
			break;
		case IdentityType.Boolean:
			scope.Primitives.Booleans.Remove (packet.Name);
			break;
		case IdentityType.Text:
			scope.Primitives.Text.Remove (packet.Name);
			break;
		case IdentityType.Structure:
			scope.Generics.Remove (packet.Name);
			break;
		case IdentityType.Function:
			scope.Functions.Remove (packet.Name);
			break;
		}
	}

	public IDPacket GET(string name)
	{
		if (current_scope.Primitives.Numbers.ContainsKey (name)) {
			return IDPacket.CreateIDPacket (this, name, IdentityType.Number);
		} else if (current_scope.Primitives.Booleans.ContainsKey (name)) {
			return IDPacket.CreateIDPacket (this, name, IdentityType.Boolean);
		} else if (current_scope.Primitives.Text.ContainsKey (name)) {
			return IDPacket.CreateIDPacket (this, name, IdentityType.Text);
		} else if (current_scope.Generics.ContainsKey (name)) {
			IDPacket StructPacket = IDPacket.CreateIDPacket (this, name, IdentityType.Structure);
			GenericFrame GF;
			ArrayFrame AF = new ArrayFrame();
			TryGetGeneric (StructPacket, out GF);
			if (GF.GetType () == AF.GetType()) {
				AF = (ArrayFrame)GF;
				StructPacket.ArrayType = AF.ResolvedType;
			}
			return StructPacket;
		} else if (current_scope.Functions.ContainsKey (name)) {
			return IDPacket.CreateIDPacket (this, name, IdentityType.Function);
		} else {
			return null;
		}
	}

	public void COPY(IDPacket TO, IDPacket FROM)
	{
		if (TO.Type != FROM.Type) {
			Error ("Attempting to copy non-matching types");
		}

		if (TO.Type == IdentityType.Number) {
			double d;
			TryGetNumber (FROM, out d);
			PutNumber (TO, d);
		} else if (TO.Type == IdentityType.Boolean) {
			bool b;
			TryGetBoolean (FROM, out b);
			PutBoolean (TO, b);
		} else if (TO.Type == IdentityType.Text) {
			string t;
			TryGetText (FROM, out t);
			PutText (TO, t);
		} else if (TO.Type == IdentityType.Structure) {
			GenericFrame sf;
			TryGetGeneric (FROM, out sf);
			PutGeneric (TO, sf);
		} else if (TO.Type == IdentityType.Function) {
			FunctionFrame ff;
			TryGetFunction (FROM, out ff);
			PutFunction (TO, ff);
		} else {
			Error ("Unknown Idenitity Type");
		}
		Debug ("Finished copy");
	}

	public class GenericFrame
	{
		public Primitives Primitives;
		public Dictionary<string, GenericFrame> Generics;
		public Dictionary<string, FunctionFrame> Functions;

		public GenericFrame() {}

		public GenericFrame(IdentityType[] Types) 
		{
			foreach (IdentityType type in Types) {
				if(type == IdentityType.Number){
					Primitives.Numbers = new Dictionary<string, double>();
				} else if(type == IdentityType.Boolean){
					Primitives.Booleans = new Dictionary<string, bool>();
				} else if(type == IdentityType.Text){
					Primitives.Text = new Dictionary<string, string>();
				} else if(type == IdentityType.Structure){
					Generics = new Dictionary<string, GenericFrame>();
				} else if(type == IdentityType.Function){
					Functions = new Dictionary<string, FunctionFrame>();
				} else {
					//nothing
				}
			}
		}

		public GenericFrame(GenericFrame original)
		{
			Merge(original);
		}

		public void Merge(GenericFrame merge)
		{
			//Copy primitives

			if (Primitives.Numbers != null && merge.Primitives.Numbers != null) {
				foreach (string key in merge.Primitives.Numbers.Keys) {
					double val; 
					merge.Primitives.Numbers.TryGetValue (key, out val);
					Primitives.Numbers.Add (key, val);
				}
			}

			if (Primitives.Booleans != null && merge.Primitives.Booleans != null) {
				foreach (string key in merge.Primitives.Booleans.Keys) {
					bool val; 
					merge.Primitives.Booleans.TryGetValue (key, out val);
					Primitives.Booleans.Add (key, val);
				}
			}

			if (Primitives.Text != null && merge.Primitives.Text != null) {
				foreach (string key in merge.Primitives.Text.Keys) {
					string val; 
					merge.Primitives.Text.TryGetValue (key, out val);
					Primitives.Text.Add (key, val);
				}
			}

			//copy structures
			if (Generics != null && merge.Generics != null) {
				foreach (string key in merge.Generics.Keys) {
					GenericFrame oval; 
					merge.Generics.TryGetValue (key, out oval);
					//TODO Preserve the heirarchy

					GenericFrame cval = GenericFrame.CopyAndPreserveHeirarchy (oval);//new GenericFrame (oval);
					Generics.Add (key, cval);
				}
			}

			//copy functions
			if (Functions != null && merge.Functions != null) {
				foreach (string key in merge.Functions.Keys) {
					FunctionFrame oval; 
					merge.Functions.TryGetValue (key, out oval);
					FunctionFrame cval = new FunctionFrame (oval);
					Functions.Add (key, cval);
				}
			}
		}

		public static GenericFrame CopyAndPreserveHeirarchy(GenericFrame oval)
		{
			ScopeFrame SCF = new ScopeFrame ();
			StructureFrame SF = new StructureFrame ();
			ArrayFrame AF = new ArrayFrame ();
			GenericFrame GF = new GenericFrame ();

			if (GF.GetType () == oval.GetType ()) {
				return oval;
			} else if (AF.GetType () == oval.GetType ()) {
				AF = (ArrayFrame)oval;
				return AF;
			} else if (SF.GetType () == oval.GetType ()) {
				SF = (StructureFrame)oval;
				return SF;
			} else {
				SCF = (ScopeFrame)oval;
				return SCF;
			}
		}
	}

	public class ArrayFrame : GenericFrame
	{
		public IdentityType ResolvedType;
		public int[] Dimensions;

		public ArrayFrame() {}

		public ArrayFrame(IdentityType[] types) : base(types) {}

		public ArrayFrame(IdentityType type, int[] Dimens) : base(new IdentityType[] { type })  {
			ResolvedType = type;
			Dimensions = Dimens;
		}

		public ArrayFrame(ArrayFrame AF) {
			/*if(AF.ResolvedType != ResolvedType)
				Error(String.Format("Attempting to copy values of {0} array into {1} array", AF.ResolvedType, ResolvedType));
			if(Dimensions.Length == AF.Dimensions.Length) {
				int i = 0;
				foreach(int d in Dimensions) {
					if(d != AF.Dimensions[i++])
						Error("Dimensions of arrays are not the same");
				}
			} else {
				Error("Dimensions of arrays are not the same");
			}*/
			ResolvedType =  AF.ResolvedType;
			Dimensions = AF.Dimensions;
			base.Merge(AF);
		}

		public ArrayFrame(GenericFrame original) : base(original) {}
	}

	public class StructureFrame : ArrayFrame
	{

		public StructureFrame() : 
		base(new IdentityType[] { IdentityType.Number, IdentityType.Boolean, IdentityType.Text, IdentityType.Structure, IdentityType.Function }) {}

		public StructureFrame(StructureFrame original): 
		base(new IdentityType[] { IdentityType.Number, IdentityType.Boolean, IdentityType.Text, IdentityType.Structure, IdentityType.Function })
		{
			Merge(original);
		}

		public StructureFrame(ArrayFrame original) : base(original) {}

		public static ScopeFrame Appropriate(string name, GenericFrame gframe)
		{
			ScopeFrame scope = new ScopeFrame ();
			scope.Type = ScopeFrame.FrameTypes.Structure;
			scope.Name = name;

			scope.Primitives = gframe.Primitives;
			scope.Generics = gframe.Generics;
			scope.Functions = gframe.Functions;

			return scope;
		}

		public void MergeForScope(string StructureName, StructureFrame original)
		{
			Generics.Remove (StructureName);
			Merge (original);
		}

		protected StructureFrame UpdateOriginal(StructureFrame original) {
			List<string> keys;

			keys = new List<string>(original.Primitives.Numbers.Keys);
			foreach (string key in keys) {
				if(Primitives.Numbers.ContainsKey(key))
					original.Primitives.Numbers [key] = Primitives.Numbers [key];
			}

			keys = new List<string>(original.Primitives.Booleans.Keys);
			foreach (string key in keys) {
				/*bool val; 
				this.Primitives.Booleans.TryGetValue (key, out val);
				original.Primitives.Booleans[key] = val;*/
				if(Primitives.Booleans.ContainsKey(key))
					original.Primitives.Booleans [key] = Primitives.Booleans [key];
			}

			keys = new List<string>(original.Primitives.Text.Keys);
			foreach (string key in keys) {
				/*string val; 
				this.Primitives.Text.TryGetValue (key, out val);
				original.Primitives.Text[key] = val;*/
				if(Primitives.Text.ContainsKey(key))
					original.Primitives.Text [key] = Primitives.Text [key];
			}

			keys = new List<string>(original.Generics.Keys);
			foreach (string key in keys) {
				if(Generics.ContainsKey(key))
					original.Generics [key] = Generics [key];
			}

			keys = new List<string>(original.Functions.Keys);
			foreach (string key in keys) {
				if(Functions.ContainsKey(key))
					original.Functions [key] = Functions [key];
			}

			return original;
		}
	}

	public class ScopeFrame : StructureFrame
	{
		public string Name;
		public FrameTypes Type;

		public enum FrameTypes { None, Structure, Function, Loop, Check }


		public ScopeFrame() : base()
		{
			Name = "";
			Type = FrameTypes.None;
		}

		public ScopeFrame(string Name, FrameTypes Type) : base()
		{
			this.Name = Name;
			this.Type = Type;
		}

		public ScopeFrame(ScopeFrame original) : base((StructureFrame) original)
		{
			Name = original.Name;
			Type = original.Type;
		}

		public ScopeFrame(ArrayFrame original, string Name, FrameTypes Type) : base(original)
		{
			this.Name = Name;
			this.Type = Type;
		}

		public ScopeFrame(StructureFrame original, string Name, FrameTypes Type) : base(original)
		{
			this.Name = Name;
			this.Type = Type;
		}

		public void Merge(ScopeFrame merge){
			base.Merge ((StructureFrame)merge);
		}

		public ScopeFrame UpdateOriginal(ScopeFrame original) {
			return (ScopeFrame)base.UpdateOriginal ((StructureFrame)original);
		}
	}  

	void RegisterStructure (string name, StructureFrame stype)
	{
		if (!IdentityExists (name))
			current_scope.Generics.Add (name, stype);
		else
			Error ("Structure with name " + name + " already exists.");
	}

	/*public bool TryGetStructure(string name, out StructureType structure)
	{
		return current_scope.Structures.TryGetValue (name, out structure);
	}*/

	public bool TryGetStructureChildIdentityType(StructureFrame scope, string name, ref IdentityType itype)
	{
		if (scope.Primitives.Numbers.ContainsKey (name)) {
			itype = IdentityType.Number;
			return true;
		} else if (scope.Primitives.Text.ContainsKey (name)) {
			itype = IdentityType.Text;
			return true;
		} else if (scope.Primitives.Booleans.ContainsKey (name)) {
			itype = IdentityType.Boolean;
			return true;
		} else if (scope.Functions.ContainsKey (name)) {
			itype = IdentityType.Function;
			return true;
		} else if (scope.Generics.ContainsKey (name)) {
			itype = IdentityType.Structure;
			return true;
		} else {
			return false;
		}
	}

	public class FunctionFrame
	{
		public IdentityType Returns;
		public int[] ReturnDimensions;
		public List<FunctionParameter> Parameters;
		public int Start;
		public YSParseNode Block;

		public FunctionFrame()
		{
			Parameters = new List<FunctionParameter> ();
		}

		public FunctionFrame(FunctionFrame original)
		{
			Start = original.Start;
			Returns = original.Returns;
			Parameters = new List<FunctionParameter> ();

			foreach (FunctionParameter ofp in original.Parameters) {
				FunctionParameter copyfp = new FunctionParameter(ofp);
				Parameters.Add (copyfp);
			}
		}
	}

	public class FunctionParameter
	{
		public string Name;
		public IdentityType Type;
		public int[] TypeDimensions;

		public FunctionParameter() 
		{
			this.TypeDimensions = new int[0];
		}

		public FunctionParameter(string Name, IdentityType Type)
		{
			this.Name = Name;
			this.Type = Type;
			this.TypeDimensions = new int[0];
		}

		public FunctionParameter(FunctionParameter original)
		{
			this.Name = original.Name;
			this.Type = original.Type;
			this.TypeDimensions = original.TypeDimensions;
		}
	}

	void RegisterFunction(string name, FunctionFrame ftype)
	{
		if (!IdentityExists (name))
			current_scope.Functions.Add (name, ftype);
		else
			Error ("Function with name " + name + " already exists.");
	}

	public bool TryGetFunction(string name, ref FunctionFrame function)
	{
		return current_scope.Functions.TryGetValue (name, out function);
	}

	public ScopeFrame CreateParseFunctionScope(FunctionFrame ftype, string FunctionName)
	{
		ScopeFrame fscope = new ScopeFrame (current_scope, FunctionName, ScopeFrame.FrameTypes.Function);

		fscope.Name = FunctionName;
		fscope.Type = ScopeFrame.FrameTypes.Function;
		PushScope (fscope);
		foreach (FunctionParameter fp in ftype.Parameters) {
			if (IsPrimitive (fp.Type)) {
				CreateParsePrimitive (fp.Name, fp.Type);
			} else if (fp.Type == IdentityType.Structure) {
				StructureFrame trash = new StructureFrame();
				PutParseStructure (fp.Name, trash);
			} else if (fp.Type == IdentityType.Function) {
				FunctionFrame trash = new FunctionFrame();
				PutParseFunction (fp.Name, trash);
			} else {
				Error ("Unknown parameter type, cannot create function scope");
			}
		}
		return PopScopeNoSave ();
	}

	public ScopeFrame CreateFunctionScope(FunctionFrame ftype, string FunctionName, List<IDPacket> Bindings)
	{
		ScopeFrame fscope = CreateParseFunctionScope (ftype, FunctionName);

		//delete function frame inside scope
		fscope.Functions.Remove (FunctionName);

		PushScope (fscope);
		int BINDCNT = 0;
		foreach (FunctionParameter fp in ftype.Parameters) {
			IDPacket Binding = Bindings [BINDCNT];
			IDPacket Address = IDPacket.CreateIDPacket (this, fp.Name, Binding.Type);
			if (Binding.Type != fp.Type)
				Error ("Binding Error: Parameter type mismatch, expected: " + fp.Type + " got " + Binding.Type);
			if (Binding.ArrayType != IdentityType.Unknown) {
				if (fp.TypeDimensions != null) {
					if (Binding.ArrayType != fp.Type) {
						Error ("Binding Error: Parameter type mismatch, expected: Array of " + fp.Type + " got Array of " + Binding.ArrayType);
					}
					int dcnt = 0;
					foreach (int dim in fp.TypeDimensions) {
						if (dim == -1)
							continue;
						if (dim != Binding.TypeDimensions [dcnt++]) {
							Error (String.Format ("Binding Error: Dimension Mismatch, expecting {0} got {1}", fp.TypeDimensions,
								Binding.TypeDimensions));
						}
					}
				} else {
					Error(String.Format("Binding Error: Expecting a Primitive {0} got Array of {1}", fp.Type, Binding.ArrayType));
				}
			} else if (fp.TypeDimensions != null) {
				Error(String.Format("Binding Error: Expecting an Array of {0} got Primitive {1}", fp.Type, Binding.ArrayType));
			}

			COPY (Address, Binding);
			Debug ("After copy");
		}
		return PopScopeNoSave ();
	}

	public bool IdentityExists(string name)
	{
		return current_scope.Primitives.Numbers.ContainsKey (name)
		|| current_scope.Primitives.Booleans.ContainsKey (name)
		|| current_scope.Primitives.Text.ContainsKey (name)
		|| current_scope.Generics.ContainsKey (name)
		|| current_scope.Functions.ContainsKey (name);
	}

	public enum IdentityType { Unknown, Boolean, Number, Text, List, GameObject, Structure, Function };

	public bool IsPrimitive(IdentityType i)
	{
		return i == IdentityType.Boolean
			|| i == IdentityType.Number
			|| i == IdentityType.Text;
	}

	public IdentityType ResolveIdentityType(Token token)
	{
		string name = token.Content;

		if (current_scope.Primitives.Numbers.ContainsKey (name)) {
			return IdentityType.Number;
		} 

		if (current_scope.Primitives.Text.ContainsKey (name)) {
			return IdentityType.Text;
		} 

		if (current_scope.Primitives.Booleans.ContainsKey (name)) {
			return IdentityType.Boolean;
		} 

		if (current_scope.Functions.ContainsKey (name)) {
			return IdentityType.Function;
		} 

		if (current_scope.Generics.ContainsKey (name)) {
			return IdentityType.Structure;
		} 

		Debug ("Cannot resolve identity type of identity " + name);
		return IdentityType.Unknown;
	}

	ScopeFrame Global_Scope;
	ScopeFrame TemporaryStorage;
	Stack<ScopeFrame> Scope_Stack;

	public static long SEED = 0;

	public ScopeFrame current_scope {
		get {
			if (Scope_Stack.Count == 0)
				return Global_Scope;
			else
				return Scope_Stack.Peek ();
		}
	}

	public ScopeFrame.FrameTypes CurrentFrameType() {
		return current_scope.Type;
	}

	public string CurrentFrameName() {
		return current_scope.Name;
	}

	public void PushScope(ScopeFrame push)
	{
		if (push.Name == "")
			Error ("Scope above Level 0 cannot have no name");
		Scope_Stack.Push (push);
	}
	/*
	public void PushStructureScope(ScopeFrame push, string Name)
	{
		ScopeFrame nsf = new ScopeFrame (push);
		nsf.Name = Name;
		nsf.Type = ScopeFrame.FrameTypes.Structure;
		Scope_Stack.Push (nsf);
	}

	public void PushFunctionScope(ScopeFrame push, string Name)
	{
		ScopeFrame nsf = new ScopeFrame (push);
		nsf.Name = Name;
		nsf.Type = ScopeFrame.FrameTypes.Function;
		Scope_Stack.Push (nsf);
	}
	*/
	public ScopeFrame PopScope()
	{
		if (Scope_Stack.Count > 0) {
			ScopeFrame pframe = Scope_Stack.Pop ();
			//update new values
			ScopeFrame current = (Scope_Stack.Count < 1) ? Global_Scope : Scope_Stack.Pop ();
			bool global_used = (Scope_Stack.Count < 1) ? true : false;
			Debug ("Popping.. Previous frame " + pframe.Name + " Current Frame " + current.Name);
			current = pframe.UpdateOriginal (current);
			if(!global_used)
				Scope_Stack.Push (current);
			return pframe;
		} else {
			Error ("Cannot pop empty stack");
			return null;
		}
	}

	public ScopeFrame PopScopeNoSave()
	{
		return Scope_Stack.Pop ();
	}

	public class IDPacket {
		public string Name { get; set; }
		public IdentityType Type { get; set; }
		public IdentityType ArrayType;
		public int[] TypeDimensions;
		public string Address { get; set; }

		static long SYSTEM_SEED = 0;
		static string RETURN_NAME = "_RTX0";
		public static string PARENT_TYPE = "..";

		public IDPacket(IDPacket copy)
		{
			Name = copy.Name;
			Type = copy.Type;
			Address = copy.Address;
			ArrayType = copy.ArrayType;
			TypeDimensions = copy.TypeDimensions;
		}

		public static IDPacket CreateIDPacket(YSStateModule state, string name, IdentityType type)
		{
			ScopeFrame[] scope_stack = state.Scope_Stack.ToArray ();
			string path = "/";
			for (int i = scope_stack.Length - 1; i >= 0; i--) {
				if (scope_stack [i].Type == ScopeFrame.FrameTypes.Structure)
					path += "s:";
				else if (scope_stack [i].Type == ScopeFrame.FrameTypes.Function)
					path += "f:";
				else if (scope_stack [i].Type == ScopeFrame.FrameTypes.Loop)
					path += "l:";
				else
					path += "u:";
				path += scope_stack [i].Name + "/";
			}
			state.Debug ("Created IDPacket with path: " + path.Trim() + " name: " + name);
			return new IDPacket (name, type, path.Trim());
		}

		public static IDPacket CreateReturnPacket(IdentityType Type)
		{
			return new IDPacket (RETURN_NAME, Type, "");
		}

		public static IDPacket CreateSystemPacket(string TempPacketName, IdentityType Type)
		{
			return new IDPacket (TempPacketName + (SYSTEM_SEED++), Type, "");
		}

		IDPacket(string name, IdentityType type, string address)
		{
			this.Name = name;
			this.Type = type;
			this.Address = address;
			this.ArrayType = IdentityType.Unknown;
			this.TypeDimensions = new int[0];
		}
	}

	//methods for actual operations

	public void OUTPUT(IDPacket OP1)
	{
		if (OP1.Type == IdentityType.Number) {
			double d;
			TryGetNumber (OP1, out d);
			Output ("" + d);
		} else if (OP1.Type == IdentityType.Text) {
			string t;
			TryGetText (OP1, out t);
			Output (t);
		} else if (OP1.Type == IdentityType.Boolean) {
			bool b;
			TryGetBoolean (OP1, out b);
			Output ("" + b);
		} else {
			Output (String.Format("Data Packet Name {0} @ {1}, Type {2}", OP1.Name, OP1.Address, OP1.Type));
		}
	}

	//Logical Operations

	public IDPacket LOGICAL_OP(IDPacket OP1, IDPacket OP2, Token Operator)
	{
		if (Operator.Type == Type.And) {
			return LOGICAL_AND (OP1, OP2);
		} else if (Operator.Type == Type.Or) {
			return LOGICAL_OR (OP1, OP2);
		} else {
			Error ("Operator not recognized");
			return null;
		}
	}

	IDPacket LOGICAL_AND(IDPacket OP1, IDPacket OP2)
	{
		bool op1, op2;
		TryGetBoolean (OP1, out op1);
		TryGetBoolean (OP2, out op2);
		Debug ("Operation AND, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("ANDOP", IdentityType.Boolean);
		PutBoolean (RES, op1 && op2);
		return RES;
	}

	IDPacket LOGICAL_OR(IDPacket OP1, IDPacket OP2)
	{
		bool op1, op2;
		TryGetBoolean (OP1, out op1);
		TryGetBoolean (OP2, out op2);
		Debug ("Operation OR, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("OROP", IdentityType.Boolean);
		PutBoolean (RES, op1 || op2);
		return RES;
	}

	public IDPacket LOGICAL_NOT(IDPacket OP1)
	{
		bool op1;
		TryGetBoolean (OP1, out op1);
		Debug ("Operation OR, op1: " + op1);
		IDPacket RES = IDPacket.CreateSystemPacket ("NOTOP", IdentityType.Boolean);
		PutBoolean (RES, !op1);
		return RES;
	}

	//Comparisions
	public IDPacket COMP_OP(IDPacket OP1, IDPacket OP2, Token Operator)
	{
		if (Operator.Type == Type.GreaterThan) {
			return COMP_GT (OP1, OP2);
		} else if (Operator.Type == Type.LessThan) {
			return COMP_LT (OP1, OP2);
		} else if (Operator.Type == Type.Equals) {
			return COMP_EQ (OP1, OP2);
		} else if (Operator.Type == Type.GreaterThanEqual) {
			return LOGICAL_OR(COMP_GT (OP1, OP2), COMP_EQ(OP1, OP2));
		} else if (Operator.Type == Type.LessThanEqual) {
			return LOGICAL_OR(COMP_LT (OP1, OP2), COMP_EQ(OP1, OP2));
		} else {
			Error ("Operator not recognized");
			return null;
		}
	}

	IDPacket COMP_GT(IDPacket OP1, IDPacket OP2)
	{
		double op1, op2;
		TryGetNumber (OP1, out op1);
		TryGetNumber (OP2, out op2);
		Debug ("Operation >, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket (">OP", IdentityType.Boolean);
		PutBoolean (RES, op1 > op2);
		return RES;
	}

	IDPacket COMP_LT(IDPacket OP1, IDPacket OP2)
	{
		double op1, op2;
		TryGetNumber (OP1, out op1);
		TryGetNumber (OP2, out op2);
		Debug ("Operation <, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("<OP", IdentityType.Boolean);
		PutBoolean (RES, op1 < op2);
		return RES;
	}

	public IDPacket COMP_EQ(IDPacket OP1, IDPacket OP2)
	{
		IDPacket RES = null;
		bool VAL = false;
		if (OP1.Type == IdentityType.Number) {
			double op1, op2;
			TryGetNumber (OP1, out op1);
			TryGetNumber (OP2, out op2);
			Debug ("Operation(Number) ==, op1: " + op1 + ", " + op2);
			RES = IDPacket.CreateSystemPacket ("==OP", IdentityType.Boolean);
			VAL = op1 == op2;
		} else if (OP1.Type == IdentityType.Boolean) {
			double op1, op2;
			TryGetNumber (OP1, out op1);
			TryGetNumber (OP2, out op2);
			Debug ("Operation(Boolean) ==, op1: " + op1 + ", " + op2);
			RES = IDPacket.CreateSystemPacket ("==OP", IdentityType.Boolean);
			VAL = op1 == op2;
		} else if (OP1.Type == IdentityType.Text) {
			string op1, op2;
			TryGetText (OP1, out op1);
			TryGetText (OP2, out op2);
			Debug ("Operation(Text) ==, op1: " + op1 + ", " + op2);
			RES = IDPacket.CreateSystemPacket ("==OP", IdentityType.Boolean);
			VAL = op1 == op2;
		} else {
			Error ("Comparing incompatible types");
			return null;
		}
		PutBoolean (RES, VAL);
		return RES;
	}

	//Arithmetic
	public IDPacket MATH_OP(IDPacket OP1, IDPacket OP2, Token Operator)
	{
		if (Operator.Type == Type.Plus) {
			return MATH_PLUS (OP1, OP2);
		} else if (Operator.Type == Type.Minus) {
			return MATH_MINUS (OP1, OP2);
		} else if (Operator.Type == Type.Asterisk) {
			return MATH_MUL (OP1, OP2);
		} else if (Operator.Type == Type.Slash) {
			return MATH_DIV (OP1, OP2);
		} else if (Operator.Type == Type.Percentage) {
			return MATH_MOD (OP1, OP2);
		} else {
			Error ("Operator not recognized");
			return null;
		}
	}

	IDPacket MATH_PLUS(IDPacket OP1, IDPacket OP2)
	{
		double op1, op2;
		TryGetNumber (OP1, out op1);
		TryGetNumber (OP2, out op2);
		Debug ("Operation +, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("+OP", IdentityType.Number);
		PutNumber (RES, op1 + op2);
		return RES;
	}

	public IDPacket MATH_MINUS(IDPacket OP1, IDPacket OP2)
	{
		double op1, op2;
		TryGetNumber (OP1, out op1);
		TryGetNumber (OP2, out op2);
		Debug ("Operation -, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("-OP", IdentityType.Number);
		PutNumber (RES, op1 - op2);
		return RES;
	}

	IDPacket MATH_MUL(IDPacket OP1, IDPacket OP2)
	{
		double op1, op2;
		TryGetNumber (OP1, out op1);
		TryGetNumber (OP2, out op2);
		Debug ("Operation *, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("*OP", IdentityType.Number);
		PutNumber (RES, op1 * op2);
		return RES;
	}

	IDPacket MATH_DIV(IDPacket OP1, IDPacket OP2)
	{
		double op1, op2;
		TryGetNumber (OP1, out op1);
		TryGetNumber (OP2, out op2);
		Debug ("Operation /, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("DIVOP", IdentityType.Number);
		PutNumber (RES, op1 / op2);
		return RES;
	}

	IDPacket MATH_MOD(IDPacket OP1, IDPacket OP2)
	{
		double op1, op2;
		TryGetNumber (OP1, out op1);
		TryGetNumber (OP2, out op2);
		Debug ("Operation %, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("%OP", IdentityType.Number);
		PutNumber (RES, op1 % op2);
		return RES;
	}

	//string
	public IDPacket STR_CONCAT(IDPacket OP1, IDPacket OP2)
	{
		string op1, op2;
		TryGetText (OP1, out op1);
		TryGetText (OP2, out op2);
		Debug ("Operation +, op1: " + op1 + ", " + op2);
		IDPacket RES = IDPacket.CreateSystemPacket ("CONCATOP", IdentityType.Text);
		PutText (RES, op1 + op2);
		return RES;
	}

	//(text haystack, number at)
	public IDPacket EXTERNAL(string MethodName, IDPacket[] PARAMS)
	{
		IDPacket ret = null;
		MethodInfo minfo = this.GetType ().GetMethod (MethodName);
		try {
			int PCNT = 0;
			List<object> parameters = new List<object>();
			foreach(ParameterInfo pinfo in minfo.GetParameters()) {
				IDPacket PARAM = PARAMS[PCNT++];
				System.Type ptype = pinfo.ParameterType;
				if(ptype == typeof(int) || ptype == typeof(double) || ptype == typeof(short) || ptype == typeof(long))
				{
					if(PARAM.Type == IdentityType.Number) {
						double d;
						TryGetNumber(PARAM, out d);
						parameters.Add(d);
					} else {
						Error(String.Format("Type mismatch in external function call, expecting {0} in parameter {1} got {2}", 
							ptype, PCNT, PARAM.Type));
					}
				} else if(ptype == typeof(string)) {
					if(PARAM.Type == IdentityType.Number) {
						string s;
						TryGetText(PARAM, out s);
						parameters.Add(s);
					} else {
						Error(String.Format("Type mismatch in external function call, expecting {0} in parameter {1} got {2}",
							ptype, PCNT, PARAM.Type));
					}
				} else if(ptype == typeof(bool)) {
					if(PARAM.Type == IdentityType.Boolean) {
						bool b;
						TryGetBoolean(PARAM, out b);
						parameters.Add(b);
					} else {
						Error(String.Format("Type mismatch in external function call, expecting {0} in parameter {1} got {2}", 
							ptype, PCNT, PARAM.Type));
					}
				} else {
					Error(String.Format("The external function {0} cannot be called from the YSInterpreter", minfo.Name));
				}
			}
			object returnValue = minfo.Invoke(minfo, parameters.ToArray());
			System.Type rtype = minfo.ReturnType;
			if(rtype == typeof(int) || rtype == typeof(double) || rtype == typeof(short) || rtype == typeof(long))
			{
				ret = IDPacket.CreateReturnPacket(IdentityType.Number);
				PutNumber(ret, (double) returnValue);
			} else if(rtype == typeof(string)) {
				ret = IDPacket.CreateReturnPacket(IdentityType.Text);
				PutText(ret, (string) returnValue);
			} else if(rtype == typeof(bool)) {
				ret = IDPacket.CreateReturnPacket(IdentityType.Boolean);
				PutBoolean(ret, (bool) returnValue);
			} else {
				Debug(String.Format("[!] The external function {0}'s returning value of type {1} cannot be used by YSInterpreter", 
					minfo.Name, minfo.ReturnType));
			}
		} catch(TargetInvocationException) {
			Error ("Failed to invoke the external method");
			return null;
		} catch(Exception) {
			Error ("Failed to invoke the external method");
			return null;
		}
		return ret;
	}
	/*
	public void Error(string s)
	{
		Console.WriteLine("[State System Error] " + s);
		throw new YSRDParser.ParseException ("Interpreter (Semantic) Exception");
	}

	public void Debug(string s)
	{
		if(DEBUG)
			Console.WriteLine("[Debug] " + s);
	}*/
	/*
	void Verbose(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		string name = (Context.Current.Token != null) ? Context.Current.Token.Content : "" + Context.Current.Type;
		string location = (Context.Current.Token != null) ? "" + Context.Current.Token.Position : "Unknown";
		if(VERBOSE)
			Console.WriteLine (String.Format("[Verbose near {0} @ {1}] {2} ", name, location, s));
	}

	void Debug(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		string name = (Context.Current.Token != null) ? Context.Current.Token.Content : "Unknown";
		string location = (Context.Current.Token != null) ? "" + Context.Current.Token.Position : "Unknown";
		if(DEBUG)
			Console.WriteLine (String.Format("[Debug near {0} @ {1}] {2} ", name, location, s));
	}

	void Error(String s)
	{
		string name = (Context.Current.Token != null) ? Context.Current.Token.Content : "" + Context.Current.Type;
		string location = (Context.Current.Token != null) ? "" + Context.Current.Token.Position : "Unknown";
		Console.WriteLine (String.Format("[Error near {0} @ {1}] {2} ", name, location, s));
		throw new Exception ("State Exception");
	}*/

	void Verbose(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		string name = Context.Current.Token.Content;
		int location = Context.Current.Token.Position;
		if(VERBOSE)
			Console.WriteLine (String.Format(CONSOLE_TAG + "[Verbose near {0} @ {1}] {2} ", name, location, s));
	}

	void Debug(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		string name = Context.Current.Token.Content;
		int location = Context.Current.Token.Position;
		if(DEBUG)
			Console.WriteLine (String.Format(CONSOLE_TAG + "[Debug near {0} @ {1}] {2} ", name, location, s));
	}

	void Error(String s)
	{
		string name = Context.Current.Token.Content;
		int location = Context.Current.Token.Position;
		Console.WriteLine (String.Format(CONSOLE_TAG + "[Error near {0} @ {1}] {2} ", name, location, s));
		throw new StateException ("State Exception");
	}

	public class StateException : Exception
	{
		public StateException(string msg) : base(msg) {
		}
	}

	void Output(string s)
	{
		Console.WriteLine ("[Output] " + s);
	}
}

