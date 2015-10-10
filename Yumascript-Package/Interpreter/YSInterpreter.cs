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
using StateException = YSStateModule.StateException;

public class YSInterpreter
{
	public static bool DEBUG = false;
	public static bool VERBOSE = false;
	public static int ERR_ACCEPT = 1;

	struct NodeIndex {
		public YSParseNode Node;
		public int Index;
	}
	//Stack<NodeIndex> Memory;
	public YSParseNode Current;
	YSParseNode Node;
	//int CINDEX = 0;
	YSStateModule STATE;

	public YSInterpreter (YSParseNode ParseTree)
	{
		Console.WriteLine ("Begining Interpreter");
		Node = ParseTree;
		//Memory = new Stack<NodeIndex> ();
		STATE = new YSStateModule (this, false);
	}

	public bool Interpret()
	{
		Console.WriteLine ("Beginning Program...");
		return Program (Node);
	}

	bool AcceptType(IdentityType accept, IdentityType type)
	{
		if (accept == type) {
			return true;
		}
		return false;
	}

	bool ExpectType(IdentityType expect, IdentityType type)
	{
		if (AcceptType (expect, type)) {
			return true;
		} else {
			Error("Unexpected Type: " + type + " expecting " + expect);
		}
		return false;
	}

	void ExpectNonExistance(string name)
	{
		if (STATE.IdentityExists (name))
			Error (String.Format ("Identity Name {0} alread exists", name));
	}

	bool Program(YSParseNode ProgramNode)
	{
		Current = ProgramNode;
		Debug(String.Format("Program Node with {0} children", ProgramNode.Children.Count));

		int SCNT = 0;
		while (SCNT < ProgramNode.Children.Count) {
			try{
				Statement (ProgramNode.Children[SCNT++]);
			}catch(InterpreterException){
				Debug ("[Interpreter Exception] Exiting Program..");
				return false;
			}catch(StateException){
				Debug ("[State Exception] Exiting Program..");
				return false;
			}
		}
		Debug ("Finished Interpretation");
		return true;
	}

	void Statement(YSParseNode StatementNode)
	{
		//Statements have only one child
		Current = StatementNode;
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
			STATE.OUTPUT (OID);
			break;
		default:
			Error ("Unknown Statement Type " + StatementChild.Type);
			break;
		}
		Debug ("Exit Statement");
	}

	void Block(YSParseNode BlockNode)
	{
		Debug ("Entering a block");
		Current = BlockNode;
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
		Current = VarCreateNode;
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
		Current = VarPrimitiveNode;
		YSToken NameToken = VarPrimitiveNode.Children[0].Children[0].Token;
		ExpectNonExistance (NameToken.Content);
		IDPacket primitive = IDPacket.CreateIDPacket (STATE, NameToken.Content, IType);
		Debug ("Creating variable " + NameToken.Content + " of type " + IType);
		if (VarPrimitiveNode.Children.Count > 1) {
			IDPacket PEXP = Expression (VarPrimitiveNode.Children [1]);
			ExpectType (primitive.Type, PEXP.Type);
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
		Current = LoopNode;
		YSParseNode ConditionNode = LoopNode.Children [1];
		ScopeFrame LoopFrame = new ScopeFrame (STATE.current_scope, "Loop@" + LoopNode.Children [0].Token.Position, ScopeFrame.FrameTypes.Loop);
		STATE.PushScope (LoopFrame);

		IDPacket EVAL_COND = Expression (ConditionNode);
		ExpectType (IdentityType.Boolean, EVAL_COND.Type);
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
		Current = ConditionNode;
		IDPacket EVAL_COND = Expression (ConditionNode.Children [0]);
		ExpectType (IdentityType.Boolean, EVAL_COND.Type);
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
		Current = StructureNode;
		string StructureName = StructureNode.Children [0].Token.Content;
		ExpectNonExistance (StructureName);
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
		Current = FunctionNode;
		string FunctionName = FunctionNode.Children [0].Token.Content;
		ExpectNonExistance (FunctionName);
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
		Current = FunctionParamListNode;
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
		Current = ExpressionLogicNode;
		int CCNT = 0;
		if (ExpressionLogicNode.Children [CCNT].Type == NType.Terminal) {
			CCNT++;
		}
		IDPacket LOG1 = ExpressionBoolean (ExpressionLogicNode.Children[CCNT]);
		if (CCNT == 1) {
			LOG1 = STATE.LOGICAL_NOT (LOG1);
		}
		CCNT++;
		while (CCNT < ExpressionLogicNode.Children.Count) {
			YSToken LogicalOperator = ExpressionLogicNode.Children [CCNT++].Token;
			IDPacket LOG2 = ExpressionBoolean (ExpressionLogicNode.Children[CCNT++]);
			LOG1 = STATE.LOGICAL_OP (LOG1, LOG2, LogicalOperator);
		}
		Debug ("Exit ExpressionLogic, final type: " + LOG1.Type);
		return LOG1;
	}

	IDPacket ExpressionBoolean(YSParseNode ExpressionBooleanNode)
	{
		Debug ("ExpressionBoolean Resolving...");
		Current = ExpressionBooleanNode;
		int CCNT = 0;
		IDPacket NUM1 = ExpressionNumber (ExpressionBooleanNode.Children [CCNT++]);
		while (CCNT < ExpressionBooleanNode.Children.Count) {
			YSToken CompOperator = ExpressionBooleanNode.Children [CCNT++].Token;
			IDPacket NUM2 = ExpressionNumber (ExpressionBooleanNode.Children [CCNT++]);
			NUM1 = STATE.COMP_OP (NUM1, NUM2, CompOperator);
		}
		Debug ("Exit ExpressionBoolean, final type: " + NUM1.Type);
		//TODO Operation
		return NUM1;
	}

	IDPacket ExpressionNumber(YSParseNode ExpressionNumberNode)
	{
		/*
		ExpressionTerm (ExpressionNumberNode.Children [0], ref id);
		int CCNT = 1;
		while (CCNT < ExpressionNumberNode.Children.Count) {
			YSToken NumOperator = ExpressionNumberNode.Children [CCNT++];
			ExpressionTerm (ExpressionNumberNode.Children [CCNT++]);
		*/
		Debug ("ExpressionNumber Resolving...");
		Current = ExpressionNumberNode;
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
			IDPacket TERM2 = ExpressionTerm (ExpressionNumberNode.Children [CCNT++]);
			//TODO Plus Operation
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
		Current = ExpressionTermNode;
		IDPacket FAC1 = ExpressionFactor (ExpressionTermNode.Children [0]);
		int CCNT = 1;
		while (CCNT < ExpressionTermNode.Children.Count) {
			/*
			YSToken TermOperator = ExpressionTermNode.Children [CCNT++];
			ExpressionFactor (ExpressionTermNode.Children [CCNT++]);
			*/
			YSToken FacOperator = ExpressionTermNode.Children [CCNT++].Token;
			IDPacket FAC2 = ExpressionFactor (ExpressionTermNode.Children [CCNT++]);
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
		Current = ExpressionFactorNode;
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
		Current = INode;
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
	}

	IDPacket IdentityStructure(YSParseNode IStructureNode)
	{
		Debug ("Resolving structure identity");
		Current = IStructureNode;
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
	}

	IDPacket IdentityFunction(YSParseNode IFunctionNode)
	{
		Debug ("Attempting to execute function");
		Current = IFunctionNode;
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

		STATE.PopScopeNoSave ();
		Debug ("Finished execution");
		return IDPacket.CreateReturnPacket (FT.Returns);
	}
	/*
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
*/
	void Verbose(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		string name = Current.Token.Content;
		int location = Current.Token.Position;
		if(VERBOSE)
			Console.WriteLine (String.Format("[Verbose near {0} @ {1}] {2} ", name, location, s));
	}

	void Debug(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		string name = Current.Token.Content;
		int location = Current.Token.Position;
		if(DEBUG)
			Console.WriteLine (String.Format("[Debug near {0} @ {1}] {2} ", name, location, s));
	}

	void Error(String s)
	{
		string name = Current.Token.Content;
		int location = Current.Token.Position;
		Console.WriteLine (String.Format("[Error near {0} @ {1}] {2} ", name, location, s));
		throw new InterpreterException ("Interpreter Exception");
	}

	public class InterpreterException : Exception
	{
		public InterpreterException(string msg) : base(msg) {
		}
	}
}

