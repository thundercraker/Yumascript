using System;
using System.Collections.Generic;
using Token = YSToken;
using Type = YSToken.TokenType;

public class YSStateModule	{

	/*public enum DataType { Boolean, Number, Text, List, GameObject, Structure, Unknown };*/

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

	Primitives PrepareEmptyPrimitives()
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

	void FindNameScope(IDPacket packet, out StructureType scope)
	{
		if (packet.Address.Length == 0) {
			scope = TemporaryStorage;
			return;
		}
		string[] path = packet.Address.Split ('.');
		scope = Global_Scope;
		foreach (string str in path) {
			if (!scope.Structures.TryGetValue (str, out scope)) {
				throw new Exception ("IDPacket Address is corrupt: " + packet.Address);
			}
		}
	}

	public void PutNumber(IDPacket packet, double number)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		double trash;
		if (TryGetNumber (packet, out trash))
			scope.Primitives.Numbers [packet.Name] = number;
		else
			scope.Primitives.Numbers.Add (packet.Name, number);
	}

	public bool TryGetNumber(IDPacket packet, out double value)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		return scope.Primitives.Numbers.TryGetValue (packet.Name, out value);
	}

	public void PutBoolean(IDPacket packet, Boolean boolean)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		bool trash;
		if (TryGetBoolean (packet, out trash))
			scope.Primitives.Booleans [packet.Name] = boolean;
		else
			scope.Primitives.Booleans.Add (packet.Name, boolean);
	}

	public bool TryGetBoolean(IDPacket packet, out Boolean value)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		return scope.Primitives.Booleans.TryGetValue (packet.Name, out value);
	}

	public void PutText(IDPacket packet, string text)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		string trash;
		if (TryGetText (packet, out trash))
			scope.Primitives.Text [packet.Name] = text;
		else
			scope.Primitives.Text.Add (packet.Name, text);
	}

	public bool TryGetText(IDPacket packet, out string value)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		return scope.Primitives.Text.TryGetValue (packet.Name, out value);
	}

	public void PutStructure(IDPacket packet, StructureType structure)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		StructureType trash;
		if (TryGetStructure (packet, out trash))
			scope.Structures [packet.Name] = structure;
		else
			scope.Structures.Add (packet.Name, structure);
	}

	public void PutParseStructure(string name, StructureType value)
	{
		current_scope.Structures.Add (name, value);
	}

	public bool TryGetStructure(IDPacket packet, out StructureType value)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		return scope.Structures.TryGetValue (packet.Name, out value);
	}

	public bool TryGetParseStructure(string name, out StructureType value)
	{
		return current_scope.Structures.TryGetValue (name, out value);
	}

	public void PutFunction(IDPacket packet, FunctionType function)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		FunctionType trash;
		if (TryGetFunction (packet, out trash))
			scope.Functions [packet.Name] = function;
		else
			scope.Functions.Add (packet.Name, function);
	}

	public void PutParseFunction(string name, FunctionType value)
	{
		current_scope.Functions.Add (name, value);
	}

	public bool TryGetFunction(IDPacket packet, out FunctionType value)
	{
		StructureType scope;
		FindNameScope (packet, out scope);
		return scope.Functions.TryGetValue (packet.Name, out value);
	}

	public bool TryGetParseFunction (string name, out FunctionType value)
	{
		return current_scope.Functions.TryGetValue (name, out value);
	}

	public void CreateParserPrimitiveIdentity(string name, IdentityType type)
	{
		Debug ("Adding var name: " + name + " type: " + type + " in scope level " + Scope_Stack.Count);
		switch (type) {
		case IdentityType.Number:
			//scope.Primitives.Numbers.Add (name, 0);
			current_scope.Primitives.Numbers.Add(name,0);
			break;
		case IdentityType.Boolean:
			current_scope.Primitives.Booleans.Add (name, false);
			break;
		case IdentityType.Text:
			current_scope.Primitives.Text.Add (name, "");
			break;
		default:
			Console.WriteLine ("Not a primitive type");
			break;
		}
	}

	public void RemoveIdentity(IDPacket packet)
	{
		StructureType scope;
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
			scope.Structures.Remove (packet.Name);
			break;
		case IdentityType.Function:
			scope.Functions.Remove (packet.Name);
			break;
		}
	}

	public struct StructureType
	{
		public Primitives Primitives;
		public Dictionary<string, StructureType> Structures;
		public Dictionary<string, FunctionType> Functions;
	}

	public StructureType PrepareEmptyStructure()
	{
		StructureType stype = new StructureType();
		stype.Primitives = PrepareEmptyPrimitives ();
		stype.Structures = new Dictionary<string, StructureType> ();
		stype.Functions = new Dictionary<string, FunctionType> ();
		return stype;
	}

	StructureType CopyStructure(StructureType original)
	{
		StructureType copy = PrepareEmptyStructure ();

		//Copy primitives
		foreach (string key in original.Primitives.Numbers.Keys) {
			double val; 
			original.Primitives.Numbers.TryGetValue (key, out val);
			copy.Primitives.Numbers.Add (key, val);
		}

		foreach (string key in original.Primitives.Booleans.Keys) {
			bool val; 
			original.Primitives.Booleans.TryGetValue (key, out val);
			copy.Primitives.Booleans.Add (key, val);
		}

		foreach (string key in original.Primitives.Text.Keys) {
			string val; 
			original.Primitives.Text.TryGetValue (key, out val);
			copy.Primitives.Text.Add (key, val);
		}

		//copy structures
		foreach (string key in original.Structures.Keys) {
			StructureType oval; 
			original.Structures.TryGetValue (key, out oval);
			StructureType cval = CopyStructure (oval);
			copy.Structures.Add (key, cval);
		}

		//copy functions
		foreach (string key in original.Functions.Keys) {
			FunctionType oval; 
			original.Functions.TryGetValue (key, out oval);
			FunctionType cval = CopyFunction (oval);
			copy.Functions.Add (key, cval);
		}

		return copy;
	}

	void RegisterStructure (string name, StructureType stype)
	{
		if (!IdentityExists (name))
			current_scope.Structures.Add (name, stype);
		else
			Error ("Structure with name " + name + " already exists.");
	}

	/*public bool TryGetStructure(string name, out StructureType structure)
	{
		return current_scope.Structures.TryGetValue (name, out structure);
	}*/

	public bool TryGetStructureChildIdentityType(StructureType scope, string name, ref IdentityType itype)
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
		} else if (scope.Structures.ContainsKey (name)) {
			itype = IdentityType.Structure;
			return true;
		} else {
			return false;
		}
	}

	public struct FunctionType
	{
		public IdentityType Returns;
		public List<FunctionParameter> Parameters;
		public int Start;
		public YSParseNode Block;
	}

	public struct FunctionParameter
	{
		public string Name;
		public IdentityType Type;
	}

	public FunctionType PrepareEmptyFunction()
	{
		FunctionType ftype = new FunctionType();
		ftype.Parameters = new List<FunctionParameter> ();
		return ftype;
	}

	FunctionType CopyFunction(FunctionType original)
	{
		FunctionType copy = PrepareEmptyFunction ();
		copy.Start = original.Start;
		copy.Returns = original.Returns;

		foreach (FunctionParameter ofp in original.Parameters) {
			FunctionParameter copyfp = new FunctionParameter ();
			copyfp.Name = ofp.Name;
			copyfp.Type = ofp.Type;
			copy.Parameters.Add (copyfp);
		}
		return copy;
	}

	void RegisterFunction(string name, FunctionType ftype)
	{
		if (!IdentityExists (name))
			current_scope.Functions.Add (name, ftype);
		else
			Error ("Function with name " + name + " already exists.");
	}

	public bool TryGetFunction(string name, ref FunctionType function)
	{
		return current_scope.Functions.TryGetValue (name, out function);
	}

	public StructureType GetFunctionScope(FunctionType ftype)
	{
		StructureType fscope = PrepareEmptyStructure ();
		fscope = CopyStructure (current_scope);

		PushScope (fscope);
		foreach (FunctionParameter fp in ftype.Parameters) {
			if (IsPrimitive (fp.Type)) {
				CreateParserPrimitiveIdentity (fp.Name, fp.Type);
			} else if (fp.Type == IdentityType.Structure) {
				StructureType trash = PrepareEmptyStructure();
				PutParseStructure (fp.Name, trash);
			} else if (fp.Type == IdentityType.Function) {
				FunctionType trash = PrepareEmptyFunction();
				PutParseFunction (fp.Name, trash);
			} else {
				Error ("Unknown parameter type, cannot create function scope");
			}
		}
		PopScope ();
		return fscope;
	}

	public bool IdentityExists(string name)
	{
		return current_scope.Primitives.Numbers.ContainsKey (name)
		|| current_scope.Primitives.Booleans.ContainsKey (name)
		|| current_scope.Primitives.Text.ContainsKey (name)
		|| current_scope.Structures.ContainsKey (name)
		|| current_scope.Functions.ContainsKey (name);
	}

	public enum IdentityType { Boolean, Number, Text, List, GameObject, Structure, Function, Unknown };

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

		if (current_scope.Structures.ContainsKey (name)) {
			return IdentityType.Structure;
		} 

		Debug ("Cannot resolve identity type of identity " + name);
		return IdentityType.Unknown;
	}

	StructureType Global_Scope;
	StructureType TemporaryStorage;
	Stack<StructureType> Scope_Stack;

	public static long SEED = 0;

	StructureType current_scope
	{
		get {
			if (Scope_Stack.Count == 0)
				return Global_Scope;
			else
				return Scope_Stack.Peek ();
		}
	}

	public void PushScope(StructureType push)
	{
		Scope_Stack.Push (push);
	}

	public StructureType PopScope()
	{
		return Scope_Stack.Pop ();
	}

	public class IDPacket {
		public string Name { get; }
		public IdentityType Type { get; }
		public string Address { get; }

		static String RETURN_NAME = "_RTX0";

		public static IDPacket CreateIDPacket(YSStateModule state, string name, IdentityType type)
		{
			StructureType[] scope_stack = state.Scope_Stack.ToArray ();
			string path = ".";
			for (int i = scope_stack.Length - 1; i >= 0; i--) {
				path += scope_stack [i];
			}
			return new IDPacket (name, type, path);
		}

		public static IDPacket CreateReturnPacket(IdentityType Type)
		{
			return new IDPacket (RETURN_NAME, Type, "");
		}

		public static IDPacket CreateSystemPacket(string TempPacketName, IdentityType Type)
		{
			return new IDPacket (TempPacketName, Type, "");
		}

		IDPacket(string name, IdentityType type, string address)
		{
			this.Name = name;
			this.Type = type;
			this.Address = address;
		}
	}

	public YSStateModule ()
	{
		Debug ("Creating scope...");
		Global_Scope = PrepareEmptyStructure ();
		Scope_Stack = new Stack<StructureType> ();

		TemporaryStorage = PrepareEmptyStructure ();
	}

	public void Error(string s)
	{
		Console.WriteLine("[Error] " + s);
		throw new YSRDParser.ParseException ("Interpreter (Semantic) Exception");
	}

	public void Debug(string s)
	{
		Console.WriteLine("[Debug] " + s);
	}
}

