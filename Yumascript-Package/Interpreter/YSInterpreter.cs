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

	public void Program(YSParseNode ProgramNode)
	{
		int SCNT = 0;
		while (SCNT < ProgramNode.Children.Count) {
			Statement (ProgramNode.Children[SCNT++]);
		}
	}

	void Statement(YSParseNode StatementNode)
	{
		//Statements have only one child
		YSParseNode StatementChild = StatementNode.Children [0];
		switch (StatementChild.Type) {
		case NType.VarCreate:
			VarCreate (StatementChild);
			break;
		case NType.Structure:
			Structure (StatementChild);
			break;
		default:
			Error ("Unknown Node Type " + StatementChild.Type);
			break;
		}
		Debug ("Exit Statement");
	}

	//type VarPrimitive(identity expression) { VarPrimitive(identity expression) }
	void VarCreate(YSParseNode VarCreateNode)
	{
		YSToken DataTypeToken = VarCreateNode.Children[0].Token;
		IdentityType IType = STATE.TranslateTokenTypeToIdentityType (DataTypeToken.Type);

		for (int i = 1; i < VarCreateNode.Children.Count; i++) {
			VarPrimitive (VarCreateNode.Children[i], IType);
		}
		Debug ("Exit VarCreate");
	}

	//identity expression
	void VarPrimitive(YSParseNode VarPrimitiveNode, IdentityType IType)
	{
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
			//STATE.PutNumber (primitive, 0);1`
			break;
		}
		Debug ("Exit VarPrimitive");
	}

	//identity
	void Structure(YSParseNode StructureNode)
	{
		string StructureName = StructureNode.Children [0].Token.Content;

		//find parent
		string ParentName = "";
		if (StructureNode.Children.Count > 1) {
			if (StructureNode.Children [1].Token.Type = YSToken.TokenType.Child) {
				ParentName = StructureNode.Children [2].Token.Content;
			}
		}

		StructureType structure = STATE.PrepareEmptyStructure ();

		//TODO register as child of parent

		STATE.PushScope (structure);
		VarCreate (StructureNode.Children[3]);
		int CCNT = 4;
		while (CCNT < Node.Children.Count) {
			VarCreate (StructureNode.Children[CCNT++]);
		}
		structure = STATE.PopScope ();

		IDPacket newStructure = IDPacket.CreateIDPacket (STATE, StructureName, IdentityType.Structure);
		STATE.PutStructure (newStructure, structure);

		Debug ("Exit Structure");
	}

	void Expression(YSParseNode ExpressionNode,ref IDPacket id)
	{
		ExpressionLogic (ExpressionNode.Children[0], ref id);

		Debug ("Exit Expression, final type: " + id.Type);
	}

	void ExpressionLogic(YSParseNode ExpressionLogicNode, ref IDPacket id)
	{
		ExpressionBoolean (ExpressionLogicNode.Children[0], ref id);
		int CCNT = 1;
		while (CCNT < ExpressionLogicNode.Children.Count) {
			YSToken LogicalOperator = ExpressionLogicNode.Children [CCNT++];
			ExpressionLogic (ExpressionLogicNode.Children[CCNT++], ref id);
		}

		Debug ("Exit ExpressionLogic, final type: " + value.Type);
	}

	void ExpressionBoolean(YSParseNode ExpressionBooleanNode, ref IDPacket id)
	{
		ExpressionNumber (ExpressionBooleanNode.Children [0], ref id);
		int CCNT = 1;
		while (CCNT < ExpressionBooleanNode.Children.Count) {
			YSToken CompOperator = ExpressionBooleanNode.Children [CCNT++];
			ExpressionNumber (ExpressionBooleanNode.Children [CCNT++]);
		}

		Debug ("Exit ExpressionBoolean, final type: " + id.Type);
	}

	void ExpressionNumber(YSParseNode ExpressionNumberNode, ref IDPacket id)
	{
		ExpressionTerm (ExpressionNumberNode.Children [0], ref id);
		int CCNT = 1;
		while (CCNT < ExpressionNumberNode.Children.Count) {
			YSToken CompOperator = ExpressionNumberNode.Children [CCNT++];
			ExpressionTerm (ExpressionNumberNode.Children [CCNT++]);
		}

		Debug ("Exit ExpressionTerm, final type: " + id.Type);
	}

	void ExpressionTerm(YSParseNode ExpressionTermNode, ref IDPacket id)
	{
		ExpressionFactor (ExpressionTermNode.Children [0], ref id);
		int CCNT = 1;
		while (CCNT < ExpressionTermNode.Children.Count) {
			YSToken CompOperator = ExpressionTermNode.Children [CCNT++];
			ExpressionFactor (ExpressionTermNode.Children [CCNT++]);
		}

		Debug ("Exit ExpressionBoolean, final type: " + id.Type);
	}

	void ExpressionFactor(YSParseNode ExpressionFactorNode, ref IDPacket id)
	{
		switch (ExpressionFactorNode.Children [0].Type) {
		case NType.Identity:
			IdentityType IdentityType = STATE.ResolveIdentityType (ExpressionFactorNode.Children [0].Token);
			string IdentityName = ExpressionFactorNode.Children [0].Token.Content;
			//get the IDPacket
			id = IDPacket.CreateIDPacket (STATE, IdentityName, IdentityType);
			break;
		case NType.IdentityFunction:
			IdentityFunction (ExpressionFactorNode, ref id);
			break;
		case NType.IdentityStructure:
			string StructureName = ExpressionFactorNode.Children [0].Token.Content;
			id = IDPacket.CreateIDPacket (STATE, StructureName, IdentityType.Structure);
			break;
		case NType.ExpressionFactor:
			ExpressionFactor(ExpressionFactorNode.Children[0]);
			break;
		case NType.Text:
			STATE.PutText (id, ExpressionFactorNode.Children [0].Children [0].Token.Content);
			break;
		case NType.Number:
			double d;
			if (!double.TryParse (ExpressionFactorNode.Children [0].Children [0].Token.Content, out d))
				Error ("Could not convert token to number");
			STATE.PutNumber (id, d);
			break;
		default:
			break;
		}
	}

	void IdentityStructure(YSParseNode StructureNode, ref IDPacket id)
	{

	}

	void IdentityFunction(YSParseNode FunctionNode, ref IDPacket id)
	{
		string FunctionName = FunctionNode.Children [0].Token.Content;
		IDPacket FunctionPacket = IDPacket.CreateIDPacket (STATE, IdentityName, IdentityType.Function);

		//execute the function
		FunctionType FT;
		if (!STATE.TryGetFunction (FunctionPacket, out FT))
			Error ("Could not retreive function frame.");
		STATE.PushScope (STATE.GetFunctionScope (FT));

		//get params
		int PCNT = 1;
		while (PCNT < FunctionNode.Children.Count) {
			FunctionParamater FP = FT.Parameters [PCNT - 1];
			IDPacket FPPacket = IDPacket.CreateIDPacket (STATE, FP.Name, FP.Type);
			Expression (FunctionNode.Children [PCNT++], FPPacket);
		}

		YSParseNode FunctionBlockNode = FT.Block;
		foreach (YSParseNode FunctionStatementNode in FunctionBlockNode) {
			if (FunctionStatementNode.Type == NType.Statement) {
				Statement (FunctionStatementNode);
			} else {
				//return statement
				IDPacket ReturnPacket = IDPacket.CreateReturnPacket(FT.Returns);
				Expression(FunctionStatementNode, ref ReturnPacket);
				id = ReturnPacket;
			}
		}
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

