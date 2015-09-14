using System;
using System.Collections.Generic;
using NType = YSParseNode.NodeType;
using IdentityType 	= YSStateModule.IdentityType;
using StructureType = YSStateModule.StructureType;
using FunctionType 	= YSStateModule.FunctionType;
using FunctionParamater = YSStateModule.FunctionParameter;
using Primitives 	= YSStateModule.Primitives;
using IDPacket = YSStateModule.IDPacket;

public class YSInterpreter
{
	struct NodeIndex {
		public YSParseNode Node;
		public int Index;
	}
	Stack<NodeIndex> Memory;

	YSParseNode Node;
	int CINDEX = 0;
	YSStateModule STATE;

	YSParseNode Current {
		get {

			return Node.Children [CINDEX];
		}
	}

	NType CType {
		get {
			return Current.Type;
		}
	}

	bool EOF {
		get {
			return CINDEX == -1;
		}
	}

	int LastChange = 0;

	bool ToChild
	{
		get{
			return LastChange == 1;
		}
	}

	void MEMPUSH()
	{
		NodeIndex cur = new NodeIndex ();
		cur.Node = Node;
		cur.Index = CINDEX;
		Memory.Push (cur);
	}

	void MEMPOP()
	{
		NodeIndex pop = Memory.Pop();
		CINDEX = pop.Index;
		Node = pop.Node;
	}

	HashSet<YSParseNode> Explored;

	int Next()
	{
		//if the current has children: next is the first child
		if (Current.Children.Count > 0 && !Explored.Contains(Current )) {
			int OCINDEX = CINDEX;
			MEMPUSH ();
			YSParseNode parent = Node;
			Node = Current;
			CINDEX = 0;
			Console.WriteLine ("Digging from {0} Index {2} into {1} Index {3}", parent.Type, Node.Type, OCINDEX, CINDEX);
			return 1;
		} else if (CINDEX + 1 < Node.Children.Count) {
			CINDEX++;
			Console.WriteLine ("Continue @ {0}, Index {1}", Node.Type, CINDEX);
			return 0;
		} else if (CINDEX + 1 >= Node.Children.Count) {
			while (CINDEX + 1 >= Node.Children.Count) {
				Console.WriteLine ("{0} >= {1}", (CINDEX + 1), Node.Children.Count);
				if (Memory.Count > 0) {
					Explored.Add (Node);
					YSParseNode child = Node;
					MEMPOP();
					Console.WriteLine ("Exiting from {0} to {1}, Index {2}", child.Type, Node.Type, CINDEX);
				} else {
					CINDEX = -1;
					return -2;
				}
			}
			CINDEX++;
			return -1;
		}
		return -2;
	}

	public YSInterpreter (YSParseNode ParseTree)
	{
		Node = ParseTree;
		Memory = new Stack<NodeIndex> ();
		Explored = new HashSet<YSParseNode> ();
		STATE = new YSStateModule ();
	}

	void ExpectType(NType Type)
	{
		if (CType != Type) {
			Error ("Attempting to resolve " + Type + " on " + CType);	
		}
	}

	public void Program()
	{
		int SCNT = 0;
		while (SCNT < Node.Children.Count) {
			Statement (Node.Children[SCNT++]);
		}
	}

	delegate void Resolver();

	void Statement(YSParseNode StatementNode)
	{
		Debug ("Statement: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
		YSParseNode StatementChild = StatementNode.Children [0];
		switch (StatementChild.Type) {
		case NType.VarCreate:
			VarCreate (StatementChild);
			break;
		case NType.Structure:
			Debug ("Found Structure");
			Structure (StatementChild);
			break;
		default:
			Error ("Unknown Node Type " + CType);
			break;
		}
		Debug ("Exit Statement");
	}

	void VarCreate(YSParseNode VarCreateNode)
	{
		//Debug ("VarCreate: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
		YSToken DataTypeToken = VarCreateNode.Children[0].Token;
		IdentityType IType = STATE.TranslateTokenTypeToIdentityType (DataTypeToken.Type);

		for (int i = 1; i < VarCreateNode.Children.Count; i++) {
			VarPrimitive (VarCreateNode.Children[i], IType);
		}
		Debug ("Exit VarCreate");
	}

	void VarPrimitive(YSParseNode VarPrimitiveNode, IdentityType IType)
	{
		//Debug ("VarPrimitive: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
		YSToken NameToken = VarPrimitiveNode.Children[0].Token;
		IDPacket primitive = IDPacket.CreateIDPacket (STATE, NameToken.Content, IType);
		Debug ("Creating variable " + NameToken.Content + " of type " + IType);
		if (VarPrimitiveNode.Children.Count > 1) {
			//Expression ();
			if (CType == NType.Expression)
				Debug ("Resolve Expression");
			else
				Error ("No assignment Expression found");
		}
		switch (IType) {
		case IdentityType.Number:
			//STATE.PutNumber (primitive, 0);
			break;
		}
		Debug ("Exit VarPrimitive");
	}

	void Structure(YSParseNode StructureNode)
	{
		//Debug ("Structure: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
		StructureType structure = STATE.PrepareEmptyStructure ();
		STATE.PushScope (structure);
		VarCreate ();
		Debug ("Structure: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
		while (CINDEX + 1 < Node.Children.Count) {
			Debug ("More Structure Variables");
			Next ();
			VarCreate ();
		}
		Debug ("Exit Structure");
	}

	void Expression()
	{
		Next ();
		Debug ("Expression: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
		ExpressionLogic ();
	}

	void ExpressionLogic()
	{
		Next ();
		Debug ("ExpressionLogic: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
		ExpressionBoolean ();
		while (Next () == 0) {
			ExpressionBoolean ();
		}
	}

	void ExpressionBoolean()
	{
		Next ();
		Debug ("ExpressionBoolean: " + Node.Type + " Index " + CINDEX + " Current " + Current.Type);
	}

	bool Terminal(out YSToken token)
	{
		token = Current.Token;
		return (CType == NType.Terminal);
	}

	public void Traverse()
	{
		int count = 0;
		while (!EOF) {
			PrintNodeData (Current);
			count++;
			Next ();
		}
		Console.WriteLine ("Proccessed {0} nodes.", count);
		throw new Exception ("Debug");
	}

	public void PrintNodeData(YSParseNode n)
	{
		if (n != null) {
			int ccount = 0;
			if (n.Children != null)
				ccount = n.Children.Count;
			string tokenc = "";
			if (n.Token != null)
				tokenc = n.Token.Content;
			Console.WriteLine ("Current Type: {0}, with {1} children, token data: {2}", n.Type, ccount, tokenc);
		} else
			Console.WriteLine ("Current is null. {0}", n);
	}

	void Debug(String s)
	{
		Console.WriteLine (s);
	}

	void Error(String s)
	{
		Console.WriteLine (s);

		throw new InterpreterException ("Interpreter Exception");
	}

	public class InterpreterException : Exception
	{
		public InterpreterException(string msg) : base(msg) {
		}
	}
}

