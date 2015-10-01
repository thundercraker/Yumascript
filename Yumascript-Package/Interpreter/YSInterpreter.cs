using System;
using System.Collections.Generic;
using NType = YSParseNode.NodeType;
using IdentityType 	= YSStateModule.IdentityType;
using ScopeFrame = YSStateModule.ScopeFrame;
using StructureFrame = YSStateModule.StructureFrame;
using FunctionFrame 	= YSStateModule.FunctionFrame;
using FunctionParamater = YSStateModule.FunctionParameter;
using Primitives 	= YSStateModule.Primitives;
using IDPacket = YSStateModule.IDPacket;

public class YSInterpreter
{
	public static bool DEBUG = false;

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
		STATE = new YSStateModule (false);
		Program (Node);
	}

	void ExpectType(NType Type)
	{
		if (CType != Type) {
			Error ("Attempting to resolve " + Type + " on " + CType);	
		}
	}

	public void Program(YSParseNode ProgramNode)
	{
		try{
			int SCNT = 0;
			while (SCNT < ProgramNode.Children.Count) {
				Statement (ProgramNode.Children[SCNT++]);
			}
			Debug ("Finished Interpretation");
		}catch(Exception e){
			Debug ("Exiting Program..");
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
		case NType.Function:
			Function (StatementChild);
			break;
		case NType.Set:
			Debug ("Set var statement");
			YSParseNode SetNode = StatementNode.Children [0];
			IDPacket ID = Identity (SetNode.Children [0]);
			IDPacket EXP = Expression (SetNode.Children [1]);
			STATE.COPY (ID, EXP);
			break;
		case NType.Call:
			Debug ("Call statement");
			YSParseNode CallNode = StatementNode.Children [0];
			IdentityFunction (CallNode);
			break;
		case NType.Condition:
			Condition (StatementNode.Children [0]);
			break;
		case NType.Loop:
			Loop (StatementNode.Children [0]);
			break;
		case NType.Output:
			Debug ("Output Statement");
			YSParseNode OutputNode = StatementNode.Children [0];
			IDPacket OID = Expression (OutputNode.Children [0]);
			if (OID.Type == IdentityType.Number) {
				double d;
				STATE.TryGetNumber (OID, out d);
				Output ("" + d);
			} else if (OID.Type == IdentityType.Text) {
				string t;
				STATE.TryGetText (OID, out t);
				Output (t);
			} else if (OID.Type == IdentityType.Boolean) {
				bool b;
				STATE.TryGetBoolean (OID, out b);
				Output ("" + b);
			}
			break;
		default:
			Error ("Unknown Node Type " + StatementChild.Type);
			break;
		}
		Debug ("Exit Statement");
	}

	void Block(YSParseNode BlockNode)
	{
		Debug ("Entering a block");
		foreach (YSParseNode StatementNode in BlockNode.Children) {
			if (StatementNode.Type == NType.Statement) {
				Statement (StatementNode);
			} else if (StatementNode.Type == NType.FunctionReturn) {
				//TODO handle function returned
				if (StatementNode.Children.Count == 1) {
					IDPacket EXP = Expression (StatementNode.Children [0]);
					IDPacket RETURN = IDPacket.CreateReturnPacket (EXP.Type);
					STATE.COPY (RETURN, EXP);
				}
			} else {
				Error ("Unknown Node Type " + StatementNode.Type);
			}
		}
		Debug ("Exiting block");
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
			IDPacket PEXP = Expression (VarPrimitiveNode.Children [1]);
			STATE.COPY (primitive, PEXP);
			Debug ("Assignment complete");
		} else {
			switch (IType) {
			case IdentityType.Number:
				STATE.PutNumber (primitive, 0);
				break;
			case IdentityType.Boolean:
				STATE.PutBoolean (primitive, false);
				break;
			case IdentityType.Text:
				STATE.PutText (primitive, "");
				break;
			}
		}
		Debug ("Exit VarPrimitive");
	}

	void Loop(YSParseNode LoopNode)
	{
		YSParseNode ConditionNode = LoopNode.Children [1];
		ScopeFrame LoopFrame = new ScopeFrame (STATE.current_scope, "Loop@" + LoopNode.Children [0].Token.Position, ScopeFrame.FrameTypes.Loop);
		STATE.PushScope (LoopFrame);

		IDPacket EVAL_COND = Expression (ConditionNode);
		bool EVAL_VAL;
		STATE.TryGetBoolean (EVAL_COND, out EVAL_VAL);
		while (EVAL_VAL) {
			Block (LoopNode.Children [2]);
			EVAL_COND = Expression (ConditionNode);
			STATE.TryGetBoolean (EVAL_COND, out EVAL_VAL);
		}

		STATE.PopScope ();
	}

	void Condition(YSParseNode ConditionNode)
	{
		IDPacket EVAL_COND = Expression (ConditionNode.Children [0]);
		bool EVAL_VAL;
		STATE.TryGetBoolean (EVAL_COND, out EVAL_VAL);
		if (EVAL_VAL) {
			Block (ConditionNode.Children [1]);
		}
	}

	//identity
	void Structure(YSParseNode StructureNode)
	{
		Debug ("Initializing Structure..");
		string StructureName = StructureNode.Children [0].Token.Content;
		StructureFrame structure = new StructureFrame ();

		int CINDEX = 0;
		//find parent
		string ParentName = "";
		if (StructureNode.Children.Count > 1) {
			if (StructureNode.Children [++CINDEX].Type == NType.Terminal) {
				ParentName = StructureNode.Children [++CINDEX].Token.Content;
			}
			//TODO register as child of parent
			IDPacket SID = IDPacket.CreateIDPacket (STATE, StructureName, IdentityType.Structure);
			STATE.PutStructure (SID, structure);

			STATE.PushScope (new ScopeFrame(structure, StructureName, ScopeFrame.FrameTypes.Structure));
			while (CINDEX < StructureNode.Children.Count) {
				//Debug ("CINDEX " + CINDEX);
				VarCreate (StructureNode.Children[CINDEX++]);
			}
			STATE.PopScopeNoSave ();
			IDPacket newStructure = IDPacket.CreateIDPacket (STATE, StructureName, IdentityType.Structure);
			STATE.PutStructure (newStructure, structure);
		}


		Debug ("Exit Structure");
	}

	void Function(YSParseNode FunctionNode)
	{
		Debug ("Beginning a function definition");
		string FunctionName = FunctionNode.Children [0].Token.Content;
		FunctionFrame FunctionFrame = new FunctionFrame ();
		FunctionParamList (FunctionNode.Children [1], ref FunctionFrame);
		FunctionFrame.Returns = STATE.TranslateTokenTypeToIdentityType (FunctionNode.Children [2].Token.Type);
		FunctionFrame.Block = FunctionNode.Children [3];

		IDPacket newFunction = IDPacket.CreateIDPacket (STATE, FunctionName, IdentityType.Function);
		STATE.PutFunction (newFunction, FunctionFrame);

		Debug ("Finished function definition");
	}

	void FunctionParamList(YSParseNode FunctionParamListNode, ref FunctionFrame Frame)
	{
		Debug ("Reading param list..");
		if (FunctionParamListNode.Children.Count > 0) {
			int FPC = 0;
			while (FPC < FunctionParamListNode.Children.Count) {
				FunctionParamater fp = new FunctionParamater ();
				fp.Type = STATE.TranslateTokenTypeToIdentityType (FunctionParamListNode.Children [FPC++].Token.Type);
				//Debug ("Type " + fp.Type + " Token " + FunctionParamListNode.Children [FPC].Token.Type);
				fp.Name = FunctionParamListNode.Children [FPC++].Token.Content;
				Frame.Parameters.Add (fp);
			}
		}
		Debug ("Read param list...");
	}
		
	//IDPacket id - IDPacket is the address to the location where the resolved
	//value of the Expression will be stored
	IDPacket Expression(YSParseNode ExpressionNode)
	{
		Debug ("Expression Resolving...");
		IDPacket output = ExpressionLogic (ExpressionNode.Children[0]);
		Debug ("Exit Expression, final type: " + output.Type);
		return output;
	}

	IDPacket ExpressionLogic(YSParseNode ExpressionLogicNode)
	{
		Debug ("ExpressionLogic Resolving...");
		int CCNT = 0;
		if (ExpressionLogicNode.Children [CCNT].Type == NType.Terminal) {
			CCNT++;
		}
		IDPacket LOG1 = ExpressionBoolean (ExpressionLogicNode.Children[CCNT]);
		if (CCNT == 1) {
			LOG1 = STATE.LOGICAL_NOT (LOG1);
		}
		CCNT++;
		if (CCNT < ExpressionLogicNode.Children.Count) {
			YSToken LogicalOperator = ExpressionLogicNode.Children [CCNT++].Token;
			IDPacket LOG2 = ExpressionLogic (ExpressionLogicNode.Children[CCNT++]);
			LOG1 = STATE.LOGICAL_OP (LOG1, LOG2, LogicalOperator);
		}
		Debug ("Exit ExpressionLogic, final type: " + LOG1.Type);
		return LOG1;
	}

	IDPacket ExpressionBoolean(YSParseNode ExpressionBooleanNode)
	{
		Debug ("ExpressionBoolean Resolving...");
		int CCNT = 0;
		IDPacket NUM1 = ExpressionNumber (ExpressionBooleanNode.Children [CCNT++]);
		if (CCNT < ExpressionBooleanNode.Children.Count) {
			YSToken CompOperator = ExpressionBooleanNode.Children [CCNT++].Token;
			IDPacket NUM2 = ExpressionBoolean (ExpressionBooleanNode.Children [CCNT++]);
			NUM1 = STATE.COMP_OP (NUM1, NUM2, CompOperator);
		}

		Debug ("Exit ExpressionBoolean, final type: " + NUM1.Type);
		//TODO Operation
		return NUM1;
	}

	IDPacket ExpressionNumber(YSParseNode ExpressionNumberNode)
	{
		Debug ("ExpressionNumber Resolving...");
		int CCNT = 0;
		if (ExpressionNumberNode.Children [CCNT].Type == NType.Terminal) {
			CCNT++;
		}
		IDPacket TERM1 = ExpressionTerm (ExpressionNumberNode.Children [CCNT]);
		if (CCNT == 1) {
			IDPacket TEMP = IDPacket.CreateSystemPacket ("TEMP_ZERO", IdentityType.Number);
			STATE.PutNumber (TEMP, 0);
			TERM1 = STATE.MATH_MINUS (TEMP, TERM1);
		}
		CCNT++;

		while (CCNT < ExpressionNumberNode.Children.Count) {
			YSToken NumOperator = ExpressionNumberNode.Children [CCNT++].Token;
			IDPacket TERM2 = ExpressionNumber (ExpressionNumberNode.Children [CCNT++]);
			if (TERM1.Type == IdentityType.Text && TERM2.Type == IdentityType.Text && NumOperator.Type == YSToken.TokenType.Plus) {
				TERM1 = STATE.STR_CONCAT (TERM1, TERM2);
			} else {
				TERM1 = STATE.MATH_OP (TERM1, TERM2, NumOperator);
			}
		}
		Debug ("Exit ExpressionTerm, final type: " + TERM1.Type);
		return TERM1;
	}

	IDPacket ExpressionTerm(YSParseNode ExpressionTermNode)
	{
		Debug ("ExpressionTerm Resolving...");
		IDPacket FAC1 = ExpressionFactor (ExpressionTermNode.Children [0]);
		int CCNT = 1;
		while (CCNT < ExpressionTermNode.Children.Count) {
			YSToken FacOperator = ExpressionTermNode.Children [CCNT++].Token;
			IDPacket FAC2 = ExpressionTerm (ExpressionTermNode.Children [CCNT++]);
			FAC1 = STATE.MATH_OP (FAC1, FAC2, FacOperator);
			//Debug ("Address " + FAC1.Name);
		}
		//Debug ("Address " + FAC1.Name);
		if (FAC1.Type == IdentityType.Unknown)
			Error ("Expression Factor Type is unresolved");
		Debug ("Exit ExpressionBoolean, final type: " + FAC1.Type);
		return FAC1;
	}

	void ErrorIfUnknown(string tag, IDPacket ID)
	{
		if (ID.Type == IdentityType.Unknown)
			Error ("Could not resolve identity type for identity " + ID.Name + " @ " + ID.Address);
		else
			Debug ("Resolved identity type " + ID.Name + " @ " + ID.Address);
	}

	IDPacket ExpressionFactor(YSParseNode ExpressionFactorNode)
	{
		Debug ("ExpressionFactor Resolving...");
		switch (ExpressionFactorNode.Children [0].Type) {
		case NType.Identity:
		case NType.IdentityFunction:
		case NType.IdentityStructure:
			IDPacket ID = Identity (ExpressionFactorNode.Children[0]);
			return ID;
		case NType.ExpressionFactor:
			IDPacket EFID = Expression (ExpressionFactorNode.Children [0]);
			ErrorIfUnknown ("Exit ExpressionFactor (ExpressionFactor)", EFID);
			return EFID;
		case NType.Text:
			IDPacket TEMP1 = IDPacket.CreateSystemPacket ("TEMP", IdentityType.Text);
			STATE.PutText (TEMP1, ExpressionFactorNode.Children [0].Children [0].Token.Content);
			ErrorIfUnknown ("Exit ExpressionFactor (Text)", TEMP1);
			return TEMP1;
		case NType.Number:
			IDPacket TEMP2 = IDPacket.CreateSystemPacket ("TEMP", IdentityType.Number);
			double d;
			if (!double.TryParse (ExpressionFactorNode.Children [0].Children [0].Token.Content, out d))
				Error ("Could not convert token to number");
			STATE.PutNumber (TEMP2, d);
			ErrorIfUnknown ("Exit ExpressionFactor (Number)", TEMP2);
			return TEMP2;

		default:
			Error ("Could not resolve Identity");
			return IDPacket.CreateSystemPacket("", IdentityType.Unknown);
		}
	}

	IDPacket Identity(YSParseNode INode)
	{
		Debug ("Resolving basic identity");
		switch(INode.Type) {
		case NType.Identity:
			YSParseNode IdentityNode = INode;
			YSParseNode TerminalNode = IdentityNode.Children [0];
			IdentityType IdentityType = STATE.ResolveIdentityType (TerminalNode.Token);
			string IdentityName = TerminalNode.Token.Content;
			//get the IDPacket
			IDPacket ID = IDPacket.CreateIDPacket (STATE, IdentityName, IdentityType);
			ErrorIfUnknown ("Exit ExpressionFactor (Identity)", ID);
			return ID;
		case NType.IdentityFunction:
			IDPacket RID = IdentityFunction (INode);
			ErrorIfUnknown ("Exit ExpressionFactor (Function)", RID);
			return RID;
		case NType.IdentityStructure:
			//string StructureName = INode.Token.Content;
			//IDPacket SID = IDPacket.CreateIDPacket (STATE, StructureName, IdentityType.Structure);
			IDPacket SID = IdentityStructure (INode);
			ErrorIfUnknown ("Exit ExpressionFactor (Structure)", SID);
			return SID;
		default:
			Error ("Identity could not be resolved");
			return IDPacket.CreateSystemPacket ("", IdentityType.Unknown);
		}
		Debug("Resolved Identity");
	}

	IDPacket IdentityStructure(YSParseNode IStructureNode)
	{
		Debug ("Resolving structure identity");
		string StructureName = IStructureNode.Children [0].Token.Content;
		StructureFrame Frame;
		Debug ("Attempting to find structure " + StructureName + " in " + STATE.current_scope.Name);
		IDPacket SID = IDPacket.CreateIDPacket (STATE, StructureName, IdentityType.Structure);
		STATE.TryGetStructure (SID, out Frame);

		ScopeFrame SF = new ScopeFrame (STATE.current_scope, StructureName, ScopeFrame.FrameTypes.Structure);
		SF.MergeForScope (StructureName, Frame);
		STATE.PushScope (SF);

		IDPacket ReturnPacket = null;
		if (IStructureNode.Children.Count > 1) {
			//child Identity
			YSParseNode INode = IStructureNode.Children [1];
			if (INode.Type != NType.Identity
			    && INode.Type != NType.IdentityStructure
			    && INode.Type != NType.IdentityFunction)
				Error ("Structure child is not an Identity");
			Debug ("Attempting to find identity " + INode.Children [0].Token.Content + " in " + STATE.current_scope.Name);
			ReturnPacket = Identity (INode);
		} else {
			Error ("Structure chain has no children");
		}

		STATE.PopScopeNoSave ();

		if (ReturnPacket == null)
			Error ("Attempting to resolve a structure chain that ends improperly");

		return ReturnPacket;
		Debug ("Finished Resolving");
	}

	IDPacket IdentityFunction(YSParseNode IFunctionNode)
	{
		Debug ("Attempting to execute function");
		string FunctionName = IFunctionNode.Children [0].Token.Content;
		IDPacket FunctionPacket = IDPacket.CreateIDPacket (STATE, FunctionName, IdentityType.Function);

		//execute the function
		FunctionFrame FT;
		if (!STATE.TryGetFunction (FunctionPacket, out FT))
			Error ("Could not retreive function frame.");

		//Create bindings
		List<IDPacket> Bindings = new List<IDPacket> ();
		foreach (YSParseNode arg in IFunctionNode.Children[1].Children) {
			IDPacket argexp = Expression (arg);
			Bindings.Add (argexp);
		}
		ScopeFrame FunctionScope = STATE.CreateFunctionScope (FT, FunctionName, Bindings);
		FunctionScope.Name = FunctionName;
		FunctionScope.Type = ScopeFrame.FrameTypes.Function;
		STATE.PushScope (FunctionScope);

		YSParseNode FunctionBlockNode = FT.Block;
		Block (FunctionBlockNode);

		STATE.PopScope ();
		Debug ("Finished execution");
		return IDPacket.CreateReturnPacket (FT.Returns);
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

	void Output(string s)
	{
		Console.WriteLine ("[Output] " + s);
	}

	void Debug(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		string name = (Current.Token != null) ? Current.Token.Content : "" + Current.Type;
		string location = (Current.Token != null) ? "" + Current.Token.Position : "Unknown";
		if(DEBUG)
			Console.WriteLine ("[Debug] " + s);
	}

	void Error(String s)
	{
		Console.WriteLine ("[Interpret Error near \" + name + \" @ \" + location + \"] " + s);

		throw new InterpreterException ("Interpreter Exception");
	}

	public class InterpreterException : Exception
	{
		public InterpreterException(string msg) : base(msg) {
		}
	}
}

