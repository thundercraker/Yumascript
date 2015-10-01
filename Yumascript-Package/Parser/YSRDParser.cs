﻿using System;
using System.Collections.Generic;
using Token 		= YSToken;
using Type 			= YSToken.TokenType;
//using DataType = YSInterpretModule.DataType;
using IdentityType 	= YSStateModule.IdentityType;
using StructureFrame = YSStateModule.StructureFrame;
using FunctionFrame 	= YSStateModule.FunctionFrame;
using FunctionParamater = YSStateModule.FunctionParameter;
using ScopeFrame = YSStateModule.ScopeFrame;
using Primitives 	= YSStateModule.Primitives;
using IDPacket = YSStateModule.IDPacket;
using ParseNodeType = YSParseNode.NodeType;

public class YSRDParser
{
	public static bool DEBUG = false;

	YSStateModule STATE;
	readonly List<Token> program_tokens;
	Stack<YSParseNode> parse_nodes;
	int PC = 0;
	IdentityType ReturnType = IdentityType.Unknown;

	Token previous
	{
		get{
			return program_tokens [PC - 1];
		}
	}

	Token current
	{
		get {
 			return program_tokens[PC];
		}
	}

	Token next
	{
		get {
			return program_tokens [PC + 1];
		}
	}
		
	YSParseNode current_node {
		get {
			return parse_nodes.Peek();
		}
	}

	void PopParseNode()
	{
		YSParseNode previous_node = parse_nodes.Pop();
		Debug ("Only Popped " + previous_node.Type + " Stack height " + parse_nodes.Count);
	}

	void PopAndInsertParseNode() {
		
		YSParseNode previous_node = parse_nodes.Pop();
		Debug ("Popped " + previous_node.Type + " Stack height " + parse_nodes.Count + " Children of Current Before " + current_node.Children.Count);
		current_node.Children.Add (previous_node);
		Debug ("Children of current node after " + current_node.Children.Count);
	}

	public YSParseNode PopLast()
	{
		YSParseNode node = new YSParseNode();
		int c = 0;
		while (parse_nodes.Count > 0) {
			node = parse_nodes.Pop ();
			c++;
		}
		Debug ("Popped " + c + " nodes");
		return node;
	}
 
	void PushParseNode(ParseNodeType Type)
	{
		Debug ("Pushing " + Type + " Stack height " + parse_nodes.Count); 
		YSParseNode n = new YSParseNode (Type);
		parse_nodes.Push (n);
	}

	void CreateTerminalChild(YSToken Token)
	{
		YSParseNode t = new YSParseNode (Token);
		current_node.Children.Add (t);
	}

	/*void TryResolveDataType(out DataType type)
	{
		if (interpreter.TryResolveDataType (current, out type)) {
			PC++;
			return true;
		}
		return false;
	}

	void ExpectResolveDataType(out DataType type)
	{
		if (!TryResolveDataType (out type))
			Error ("Expecting a data type");
	}*/

	public YSRDParser (string raw)
	{
		Debug ("Creating interpreter...");
		STATE = new YSStateModule (true);
		Debug ("Begin parser....");
		var Lexer = new YSLexer (raw);
		program_tokens = Lexer.GetTokenList ();
		PC = 0;
		parse_nodes = new Stack<YSParseNode> ();
		Debug ("Beginning parsing from Program");
		Program ();
	}

	bool EOF
	{
		get{
			return PC >= program_tokens.Count;
		}
	}

	bool Accept(Type t)
	{
		if (current.Is (t)) {
			Debug ("Accepted " + t + " Content: " + current.Content);
			PC++;
			Debug (PC + "");
			return true;
		}
		return false;
	}

	bool Expect(Type t)
	{
		if (Accept (t)) {
			return true;
		} else {
			Error("Unexpected Token: " + current.Content + " expecting " + t.ToString());
		}
		return false;
	}

	bool ExpectTerminal(Type t)
	{
		if (Expect (t)) {
			CreateTerminalChild (previous);
			return true;
		}
		return false;
	}

	/*bool AcceptType(DataType accept, DataType type)
	{
		if (accept == type) {
			return true;
		}
		return false;
	}

	bool ExpectType(DataType expect, DataType type)
	{
		if (AcceptType (expect, type)) {
			return true;
		} else {
			Error("Unexpected Type: " + type + " expecting " + expect);
		}
		return false;
	}*/

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

	bool DataType(bool Expect_Sub)
	{
		Debug ("Checking if " + current.Type + " is a datatype"); 
		Type t = current.Type;
		if (DataTypeSimple()) {
			PC++;
			return true;
		} else if (t == Type.Array) {
			Accept (Type.Array);
			Expect (Type.Of);
			if (!DataTypeSimple ())
				Error ("Expecting a data type after array of");
			PC++;
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a data type");
			return false;
		}
	}

	bool DataTypeSimple()
	{
		Type t = current.Type;
		if (t == Type.Number || t == Type.Boolean || t == Type.Text || t == Type.Structure) {
			Debug ("Is datatype");
			return true;
		} else {
			return false;
		}
	}

	void Debug(String s)
	{
		//Console.WriteLine ("Debug " + DEBUG);
		if(DEBUG)
			Console.WriteLine ("[Debug] " + s);
	}

	void Error(String s)
	{
		Console.WriteLine ("[Parse Error near " + current.Content + " @ " + current.Position + "] " + s);
		throw new ParseException ("Parser Exception");
	}

	//TODO
	//1) Expression List Parsing

	/* programs		:= { statement }
	 * 
	 * 
	 * 
	 * statement 	:= set identity "=" expression ;
	 * 				| var-create ;
	 *				| condition
	 *				| loop
	 *				| call identity
	 *				| function
	 *				| output expression
	 *
	 *	var-create	:= primitive-type identity [ "=" expression ] { , identity [ "=" expression ] }
	 *				| array of type identity "=" array "(" array-exp ")" ;
	 *
	 *	condition	:= "if" expression "then" block
	 *
	 *	loop		:= "while" expression "do" block
	 *
	 *	function	:= "function" ident "(" type ident { , type ident } ")" : type "{" { ( var-create| condition | loop | structure | 
	 *				| ( return expression ; ) ) } "}" 
	 *
	 *	structure	:= "structure" identity [ "child" "of" identity ] "{" var-create ; { var-create ; } "}"
	 *	
	 *	block 		:= "{" { statement } "}"
	 *
	 *
	 *	-Expressions-
	 *
	 *	expression 	:= expr-list
	 *				| expr-logic
	 *	
	 *	expr-list 	:= "[" [ expression { , expression } ] "]"
	 *
	 *  expr-logic	:= [not] expr-bool { (and|or) [not] expr-bool }
	 *
	 *	expr-bool	:= expr-num { compopr expr-num }
	 *
	 *	expr-num	:= [addoppr] expr-term { addoppr expr-term }
	 *
	 *	expr-term 	:= expr-factor { ("*"|"/") expr-factor }
	 *
	 *	expr-factor := ident
     * 				| numberdata
     * 				| textdata
     * 				| "(" expression ")"
     * 
     * 	ident		:= [(global|parent{.parent}).]identity | ident-arr | ident-str | ident-fun
     * 
     * 	ident-arr	:= ident : structure { "[" expression "]" }
     * 	
     * 	ident-str	:= ident : structure { "." ident }
     * 
     * 	ident-fun	:= ident : function "(" expression { , expression } ")"
	 *
	 *	-Operators-
	 *
	 *	addoppr 	:= "+" | "-"
	 *	muloppr 	:= "*" | "/"
	 *	compopr		:= "equals" | "<" | "<=" | ">" | ">="
	 *
	 *	primitive-type 		:= Number | Text | List | GameObject | Boolean #| "<" identity ">" allow users to create objects/their own datatypes
	 */

	void Program()
	{
		YSParseNode n = new YSParseNode (ParseNodeType.Program);
		parse_nodes.Push (n);

		Debug ("Program");
		try {
			Debug ("Beginning parsing of " + program_tokens.Count + " tokens");
			while (!EOF) {
				Statement ();
			}
			Debug ("End of parsing");
		} catch(ParseException) {
			Debug ("Parsing aborted");
		}
	}


	void Statement()
	{
		PushParseNode (ParseNodeType.Statement);

		IdentityType type = new IdentityType ();
		IdentityType id_type = new IdentityType ();
		if (Accept (Type.Set)) {
			PushParseNode (ParseNodeType.Set);
			Identity (true, ref type, ref id_type);
			Debug ("Identity returned " + type);

			Expect (Type.Assign);
			IdentityType X1 = IdentityType.Unknown;
			Expression (ref X1);
			if (X1 != type)
				Error ("Cannot assign type " + X1 + " to variable that is of type " + type);
			Expect (Type.Semicolon);
			PopAndInsertParseNode ();
		} else if (Accept (Type.Call)) {
			PushParseNode (ParseNodeType.Call);
			Identity (true, ref type, ref id_type);
			if (id_type != IdentityType.Function)
				Error ("Expecting function identity after call keyword");
			PopAndInsertParseNode ();
			Expect (Type.Semicolon);
		} else if (Accept(Type.Output)) {
			//Print statement
			PushParseNode (ParseNodeType.Output);
			Expression (ref type);
			if (!STATE.IsPrimitive (type))
				Error ("Output can only send primitive types");
			PopAndInsertParseNode ();
			Expect (Type.Semicolon);
		} else if (VarCreate (false)) {
			Expect (Type.Semicolon);
		} else if (Condition (false)) {
		} else if (Structure (false)) {
		} else if (Function (false)) {
		} else if (Loop (false)) {
		} else if (Accept (Type.Return)) {
			if (ReturnType != IdentityType.Unknown) {
				PopParseNode ();
				PushParseNode (ParseNodeType.FunctionReturn);

				IdentityType X1 = IdentityType.Unknown;
				Expression (ref X1);
				if (ReturnType != X1)
					Error ("Function " + STATE.CurrentFrameName () + " must return " + ReturnType);
				Expect (Type.Semicolon);

				PopAndInsertParseNode ();
			} else {
				Error ("Cannot have a return statement inside a non-function block");
			}
		} else {
			Error ("Unrecognized statement");
		}
		if(ReturnType == IdentityType.Unknown)
			PopAndInsertParseNode ();
	}

	bool VarCreate(bool Expect_Sub)
	{
		Debug ("Creation...(" + current.Content + " type " + current.Type + ")");
		PushParseNode (ParseNodeType.VarCreate);

		IdentityType itype = STATE.TranslateTokenTypeToIdentityType (current.Type);

		if (itype != IdentityType.Function && itype != IdentityType.Structure && itype != IdentityType.Unknown) {
			CreateTerminalChild (current);
			PC++;
			VarCreatePrimitiveHelper (itype);
			while (Accept (Type.Comma)) {
				VarCreatePrimitiveHelper (itype);
			}

			PopAndInsertParseNode ();
			return true;
		} else if (Accept (Type.Array)) {
			Expect (Type.Of);

			IdentityType array_type = STATE.ResolveIdentityType (current);
			if (array_type == IdentityType.Unknown)
				Error ("Expecting a valid data type");
			PC++;

			Expect (Type.Identity);
			if (STATE.IdentityExists (previous.Content))
				Error ("Variable named " + previous.Content + " already exists in this scope");
			string arrayName = previous.Content;
			Expect (Type.Assign);
			if (Accept (Type.Array)) {
				Expect (Type.LParen);
				IdentityType XL1 = IdentityType.Unknown;
				ExpressionList (ref XL1);
				Expect (Type.RParen);
			} else {
				IdentityType X1 = IdentityType.Unknown;
				Expression (ref X1);

				if (X1 != array_type) {
					Error ("Expression type does not match array type");
				}
			}

			PopAndInsertParseNode ();
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a Data member creation");
			PopParseNode ();
			return false;
		}
	}

	void VarCreatePrimitiveHelper(IdentityType type)
	{
		Debug ("Primitive Creation...(" + current.Content + " type " + current.Type + ")");
		PushParseNode (ParseNodeType.VarPrimitive);

		Expect (Type.Identity);
		CreateTerminalChild (previous);

		string variableName = previous.Content;
		if (STATE.IdentityExists (variableName))
			Error ("Variable named " + variableName + " already exists in this scope");
		if (Accept (Type.Assign)) {
			IdentityType X1 = IdentityType.Unknown;
			Expression (ref X1);
			if (X1 != type)
				Error ("Cannot assign value of type " + X1 + " to variable " + variableName + " which expects " + type);
			else {
				Debug ("Creating a primitive variable with assignment");
				STATE.CreateParsePrimitive (variableName, type);
			}

		} else {
			Debug ("Creating a primitive variable without assignment");
			STATE.CreateParsePrimitive(variableName, type);
		}
		PopAndInsertParseNode ();
		/*
		PrimitiveType ptype = new PrimitiveType();
		ptype.type = type;
		Global_Scope.Primitives.Add (variableName, ptype);*/
	}

	bool Condition(bool Expect_Sub)
	{
		Debug ("Condition...(" + current.Content + " type " + current.Type + ")");

		IdentityType X1 = IdentityType.Unknown;
		if (Accept (Type.If)) {
			PushParseNode (ParseNodeType.Condition);
			Expect (Type.LParen);
			Expression (ref X1);
			Expect (Type.RParen);
			Expect (Type.Then);
			Block (true);

			PopAndInsertParseNode ();
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a conditional statement");
			return false;
		}
	}

	bool Loop(bool Expect_Sub)
	{
		Debug ("Loop...(" + current.Content + " type " + current.Type + ")");

		IdentityType X1 = IdentityType.Unknown;
		if (Accept (Type.While)) {
			/*
			 * 
		} else if(Accept(Type.While)){
			PushParseNode (ParseNodeType.Loop);
			CreateTerminalChild (previous);
			Expression (ref type);
			ExpectType (IdentityType.Boolean, type);
			Expect (Type.Do);
			Block ();
			PopAndInsertParseNode();
			 */
			PushParseNode (ParseNodeType.Loop);
			CreateTerminalChild (previous);
			Expect (Type.LParen);
			Expression (ref X1);
			Expect (Type.RParen);
			ExpectType (IdentityType.Boolean, X1);
			Expect (Type.Do);
			Block (true);

			PopAndInsertParseNode ();
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a loop statement");
			return false;
		}
	}

	bool Structure(bool Expect_Sub)
	{
		Debug ("Structure...(" + current.Content + " type " + current.Type + ")");

		if (Accept (Type.Structure)) {
			PushParseNode (ParseNodeType.Structure);
			StructureFrame sframe = new StructureFrame ();
			StructureFrame parent = null;

			Expect (Type.Identity);
			string structureName = previous.Content;
			CreateTerminalChild (previous);

			if (STATE.IdentityExists (structureName))
				Error ("Variable named " + structureName + " already exists in this scope");

			string parentName = "";
			if (Accept (Type.Child)) {
				CreateTerminalChild (previous);
				Expect (Type.Of);
				Expect (Type.Identity);
				parentName = previous.Content;
				CreateTerminalChild (previous);
				if (STATE.TryGetParseStructure(previous.Content, out parent)) {
					Debug ("Added as a parent of " + parentName);
				} else {
					Error ("A structure can only be a child of another structure");
				}
			}
			STATE.PushScope (new ScopeFrame(sframe, structureName, ScopeFrame.FrameTypes.Structure));
			Expect (Type.LCBraket);
			//TODO implement Structure inside structure
			VarCreate (true);
			Expect (Type.Semicolon);
			while (VarCreate (false)) {
				Expect (Type.Semicolon);
			}
			Expect (Type.RCBraket);
			sframe = STATE.PopScope ();
			if (parent != null) {
				//parent.Structures.Add (structureName, stype);
				STATE.PushScope(new ScopeFrame(parent, parentName, ScopeFrame.FrameTypes.Structure));
				STATE.PutParseStructure (structureName, sframe);
				STATE.PopScope ();
			} else {
				STATE.PutParseStructure (structureName, sframe);
			}
			//STATE.PopScope ();
			PopAndInsertParseNode ();
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a structure statement");
			return false;
		}
	}

	bool Function(bool Expect_Sub)
	{
		//( var-create| condition | loop | structure | ( return expression ; ) )
		Debug ("Function...(" + current.Content + " type " + current.Type + ")");

		IdentityType type = IdentityType.Unknown;

		if (Accept (Type.Function)) {
			PushParseNode (ParseNodeType.Function);

			Expect (Type.Identity);
			CreateTerminalChild (previous);
			string functionName = previous.Content;

			if (STATE.IdentityExists (functionName))
				Error ("Variable named " + functionName + " already exists in this scope");

			FunctionFrame fframe = new FunctionFrame ();

			PushParseNode (ParseNodeType.FunctionParamList);

			Expect (Type.LParen);
			if (!Accept (Type.RParen)) {
				FunctionParamater fp = new FunctionParamater ();
				Debug (current.Content);
				if (DataType (false)) {
					//Debug ("check");
					CreateTerminalChild (previous);
					fp.Type = STATE.TranslateTokenTypeToIdentityType (previous.Type);
					//Debug ("check2");
					Expect (Type.Identity);
					CreateTerminalChild (previous);
					fp.Name = previous.Content;
					fframe.Parameters.Add (fp);
				}
				while (Accept (Type.Comma)) {
					fp = new FunctionParamater ();
					DataType (true);
					CreateTerminalChild (previous);
					fp.Type = STATE.TranslateTokenTypeToIdentityType (previous.Type);
					Expect (Type.Identity);
					CreateTerminalChild (previous);
					fp.Name = previous.Content;

					fframe.Parameters.Add (fp);
				}
			}
			Expect (Type.RParen);
			PopAndInsertParseNode ();
			
			Expect (Type.Colon);
			DataType (true);
			fframe.Returns = STATE.TranslateTokenTypeToIdentityType(previous.Type);
			CreateTerminalChild (previous);
			fframe.Start = PC;

			STATE.PutParseFunction (functionName, fframe);

			Debug ("Check");

			STATE.PushScope(STATE.CreateParseFunctionScope (fframe, functionName));

			Debug ("Check");

			ReturnType = fframe.Returns;
			Block (true);
			ReturnType = IdentityType.Unknown;

			STATE.PopScope ();
			PopAndInsertParseNode ();
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a function statement");
			return false;
		}
	}

	void Block(bool Expect_Sub)
	{
		PushParseNode (ParseNodeType.Block);
		if (Expect (Type.LCBraket)) {
			while (!Accept (Type.RCBraket)) {
				Statement ();
			}
			PopAndInsertParseNode ();
		} else {
			if (Expect_Sub)
				Error ("Expecting a block");
		}
	}

	void Expression (ref IdentityType type)
	{
		if (ExpressionList (ref type)) {
			type = IdentityType.List;
		} else {
			PushParseNode (ParseNodeType.Expression);
			ExprLogic  (ref type);
			Debug ("Expression type " + type);
			PopAndInsertParseNode ();
		}
	}

	bool ExpressionList(ref IdentityType type)
	{
		if (Accept (Type.LSBraket)) {
			PushParseNode (ParseNodeType.ExpressionList);
			type = IdentityType.List;
			if (!Accept (Type.RSBraket)) {
				Expression (ref type);
				while (Accept (Type.Comma)) {
					Expression (ref type);
				}
			}
			Expect (Type.RSBraket);
			PopAndInsertParseNode ();
			return true;
		} else {
			return false;
		}
	}

	void ExprLogic (ref IdentityType type)
	{
		PushParseNode (ParseNodeType.ExpressionLogic);

		bool not = false;
		if (Accept (Type.Not)) {
			CreateTerminalChild (previous);
			not = true;
		}

		IdentityType X1 = IdentityType.Unknown;
		ExprBool (ref X1);

		if (not) {
			ExpectType (IdentityType.Boolean, X1);
		}

		int stackCount = 0;
		while (Accept (Type.And) || Accept (Type.Or)) {
			CreateTerminalChild (previous);
			PushParseNode (ParseNodeType.ExpressionLogic);
			if (Accept (Type.Not)) {
				CreateTerminalChild (previous);
			}
			IdentityType X2 = IdentityType.Unknown;
			ExprBool (ref  X2);
			ExpectType (IdentityType.Boolean, X2);
			not = true;

			stackCount++;
			//PopAndInsertParseNode ();
		}
		for (int i = 0; i < stackCount; i++)
			PopAndInsertParseNode ();

		type = (not) ? IdentityType.Boolean : X1;

		PopAndInsertParseNode ();
	}

	void ExprBool (ref IdentityType type)
	{
		PushParseNode (ParseNodeType.ExpressionBoolean);
		IdentityType X1 = IdentityType.Unknown;
		ExprNum  (ref X1);

		bool comp = false;
		if (CompOpr (false)) {
			CreateTerminalChild (previous);
			Token operation = previous;
			PushParseNode (ParseNodeType.ExpressionBoolean);
			IdentityType X2 = IdentityType.Unknown;
			ExprNum  (ref  X2);
			if (previous.Type == Type.Equals) {
				ExpectType (X1, X2);
			} else {
				ExpectType (IdentityType.Number, X1);
				ExpectType (IdentityType.Number, X2);
			}

			PopAndInsertParseNode ();
			comp = true;
		}
		/*for (int i = 0; i < stackCount; i++)
			PopAndInsertParseNode ();*/
		if (comp)
			Debug ("Opr");
		type = (!comp) ? X1 : IdentityType.Boolean;

		PopAndInsertParseNode ();
	}

	void ExprNum (ref IdentityType type)
	{
		PushParseNode (ParseNodeType.ExpressionNumber);

		bool sign = false;
		if (Accept (Type.Minus)) {
			CreateTerminalChild (previous);
			sign = true;
		}
		
		IdentityType X1 = IdentityType.Unknown;
		ExprTerm  (ref X1);
		if (sign)
			ExpectType (IdentityType.Number, X1);
		type = X1;
		int stackCount = 0;
		while (Accept (Type.Plus) || Accept(Type.Minus)) {
			Token optok = previous;
			CreateTerminalChild (optok);
			PushParseNode (ParseNodeType.ExpressionNumber);

			IdentityType X2 = IdentityType.Unknown;
			ExprTerm (ref X2);
			if (optok.Type == Type.Plus && (X1 == IdentityType.Text || X2 == IdentityType.Text))
				//ExpectType (IdentityType.Text, X1);
				type = IdentityType.Text;
			else {
				ExpectType (IdentityType.Number, X1);
				type = IdentityType.Number;
			}
			ExpectType (X1, X2);

			//PopAndInsertParseNode ();
			stackCount++;
		}
		for (int i = 0; i < stackCount; i++)
			PopAndInsertParseNode ();

		PopAndInsertParseNode ();
	}

	void ExprTerm (ref  IdentityType type)
	{
		PushParseNode (ParseNodeType.ExpressionTerm);

		Debug ("Term - token: " + current.Content);
		IdentityType X1 = IdentityType.Unknown;
		ExprFactor  (ref X1);
		bool once = false;
		int stackCount = 0;
		while (Accept (Type.Asterisk) || Accept(Type.Slash) || Accept(Type.Percentage)) {
			CreateTerminalChild (previous);
			ExpectType (IdentityType.Number, X1);
			PushParseNode (ParseNodeType.ExpressionTerm);

			if (!once)
				ExpectType (IdentityType.Number, X1);
			else
				once = true;
			
			IdentityType X2 = IdentityType.Unknown;
			ExprFactor (ref X2);
			ExpectType (IdentityType.Number, X2);

			//PopAndInsertParseNode ();
			stackCount++;
		}

		for (int i = 0; i < stackCount; i++)
			PopAndInsertParseNode ();
		if (!once)
			type = X1;
		else
			type = IdentityType.Number;

		PopAndInsertParseNode ();
	}

	void ExprFactor (ref IdentityType type)
	{
		PushParseNode (ParseNodeType.ExpressionFactor);

		Debug ("Factor - token: " + current.Content);
		IdentityType id_type = IdentityType.Unknown;
		if (Identity(false, ref type, ref id_type)) {
		} else if (Accept(Type.NumberData)) {
			PushParseNode (ParseNodeType.Number);
			CreateTerminalChild (previous);
			PopAndInsertParseNode ();
			type = IdentityType.Number;
			/*double numberValue;
			if (Double.TryParse (previous.Content, out numberValue))
				STATE.PutNumber (value, numberValue);
			else
				Error ("Expecting a number, found " + previous.Content);*/
		} else if (Accept(Type.TextData)) {
			PushParseNode (ParseNodeType.Text);
			CreateTerminalChild (previous);
			PopAndInsertParseNode ();
			type = IdentityType.Text;
			//STATE.PutText (value, previous.Content);
		} else if (Accept (Type.LParen)) {
			PushParseNode (ParseNodeType.Expression);
			Expression  (ref type);
			Expect (Type.RParen);
			PopAndInsertParseNode ();
		} else {
			Error ("Expecting a data, a number, operator or other identity of an expression");
		}

		PopAndInsertParseNode ();
	}

	bool CompOpr(bool Expect_Sub)
	{
		if (Accept (Type.Equals) || Accept (Type.GreaterThan) || Accept (Type.LessThan)) {
			return true;
		} else {
			if(Expect_Sub)
				Error ("Expecting a comparision operator");
			return false;
		}
	}

	YSToken GlobalIdentity;
	List<YSToken> ParentIdentity;
	void PushGlobalAndParentKW()
	{
		if (GlobalIdentity != null) {
			CreateTerminalChild (GlobalIdentity);
		} else if (ParentIdentity.Count > 0) {
			PushParseNode (ParseNodeType.Parent);
			for (int i = 0; i < ParentIdentity.Count; i++) {
				CreateTerminalChild (ParentIdentity [i]);
			}
			PopAndInsertParseNode ();
		}
	}

	bool Identity(bool Expect_Sub, ref IdentityType type, ref IdentityType identity_type)
	{
		Debug ("Identity - token: " + current.Content);
		//Reset Global and Parent Nodes
		GlobalIdentity = null;
		ParentIdentity = new List<Token> ();
		if (Accept (Type.Global)) {
			GlobalIdentity = previous;
		} else if (Accept (Type.Parent)) {
			ParentIdentity.Add (previous);
			while (Accept (Type.Period)) {
				if (Accept (Type.Parent)) {
					ParentIdentity.Add (previous);
				} else if (current.Type == Type.Global) {
					Error ("Global keyword cannot follow or come after Parent keyword");
				}
			}
		}
		if (Accept (Type.Identity)) {
			Token identityToken = previous;
			if (IdentityPrimitive (false, ref type)) {
				identity_type = type;
			} else if (IdentityFunction (false, ref type)) {
				identity_type = IdentityType.Function;
			} else if (IdentityStructure (false, ref type)) {
				identity_type = IdentityType.Structure;
			} else {
				Error ("Could not resolve the identity " + identityToken.Content);
			}
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a resolvable identity/variable");
			return false;
		}
	}


	bool IdentityPrimitive(bool Expect_Sub, ref IdentityType type)
	{
		Debug ("Identity Primitive - token: " + previous.Content);

		Token identityToken = previous;

		IdentityType itype = STATE.ResolveIdentityType (identityToken);
		if (STATE.IsPrimitive(itype)) {
			if (Accept (Type.LSBraket)) {
				//indentity[number]
				PushParseNode(ParseNodeType.IdentityArray);
				PushGlobalAndParentKW ();
				CreateTerminalChild (identityToken);

				IdentityType _type = new IdentityType();
				Expression (ref  _type);
				ExpectType (IdentityType.Number, _type);
				Expect (Type.RSBraket);
				type = itype;
				Debug ("Resolved primitive value from array " + identityToken.Content + " of type " + type);

				PopAndInsertParseNode ();
				return true;
			} else {
				PushParseNode (ParseNodeType.Identity);

				CreateTerminalChild (identityToken);
				type = itype;
				Debug ("Resolved primitive value from variable " + identityToken.Content + " of type " + type);
				PopAndInsertParseNode ();
				return true;
			}
		} else {
			Debug ("Could not resolve " + identityToken.Content + " as a primitive");
			if (Expect_Sub)
				Error ("Expecting a primitive variable");
			type = itype;
			return false;
		}
	}

	bool IdentityStructure(bool Expect_Sub, ref IdentityType type)
	{
		Debug ("Idenitity Structure...");
		Token identityToken = previous;
		IdentityType itype = STATE.ResolveIdentityType (identityToken);
		if (itype == IdentityType.Structure)
		{
			PushParseNode (ParseNodeType.IdentityStructure);
			PushGlobalAndParentKW ();
			CreateTerminalChild (identityToken);

			StructureFrame child;
			//switch to nested structure context
			if (!STATE.TryGetParseStructure (identityToken.Content, out child))
				Error ("Internal Structure loading error...could not load the frame of structure " + identityToken.Content);

			ScopeFrame sf = new ScopeFrame (STATE.current_scope, identityToken.Content, ScopeFrame.FrameTypes.Structure);
			sf.Merge (child);

			STATE.PushScope (sf);
			Debug ("Pushed scope: " + identityToken.Content);

			while(Accept(Type.Period)){
				IdentityType I1 = IdentityType.Unknown, I2 = IdentityType.Unknown;

				Identity (true, ref I1, ref I2);
				itype = I1;
				type = itype;
			}

			Debug ("Popped Scope");
			STATE.PopScope ();
			PopAndInsertParseNode ();
			return true;
		} else {
			Debug ("Could not resolve " + identityToken.Content + " as a Structure");
			if (Expect_Sub)
				Error ("Expecting a structure");
			return false;
		}
	}

	bool IdentityFunction(bool Expect_Sub, ref IdentityType type)
	{
		Debug ("Idenitity Function...");
		Token identityToken = previous;
		IdentityType itype = STATE.ResolveIdentityType (identityToken);

		if (itype == IdentityType.Function) {
			PushParseNode (ParseNodeType.IdentityFunction);
			PushGlobalAndParentKW ();
			CreateTerminalChild (identityToken);

			string functionName = identityToken.Content;

			Expect (Type.LParen);
			FunctionFrame ftype;
			if (!STATE.TryGetParseFunction (functionName, out ftype))
				Error ("No function with the name " + identityToken.Content);
			type = ftype.Returns;
			int pcount = 0;
			PushParseNode (ParseNodeType.FunctionArgList);
			if (!Accept (Type.RParen)) {
				IdentityType P1 = IdentityType.Unknown;
				Expression (ref  P1);
				ExpectType (ftype.Parameters [pcount++].Type, P1);
				while (Accept (Type.Comma)) {
					Expression (ref  P1);
					ExpectType (ftype.Parameters [pcount++].Type, P1);
				}
				Expect (Type.RParen);
				PopAndInsertParseNode ();
			}
			PopAndInsertParseNode ();
			return true;
		} else {
			Debug ("Could not resolve " + identityToken.Content + " as a Function");
			if (Expect_Sub)
				Error ("Expecting function call");
			return false;
		}
	}
		
	void Stop()
	{
		program_tokens.Clear ();
	}

	public class ParseException : Exception
	{
		public ParseException(string msg) : base(msg) {
		}
	}
}

