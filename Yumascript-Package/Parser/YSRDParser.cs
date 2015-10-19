using System;
using System.Collections.Generic;
using Token 		= YSToken;
using Type 			= YSToken.TokenType;
//using DataType = YSInterpretModule.DataType;
using IdentityType 	= YSStateModule.IdentityType;
using StructureFrame = YSStateModule.GenericFrame;
using FunctionFrame 	= YSStateModule.FunctionFrame;
using FunctionParamater = YSStateModule.FunctionParameter;
using ScopeFrame = YSStateModule.ScopeFrame;
using Primitives 	= YSStateModule.Primitives;
using IDPacket = YSStateModule.IDPacket;
using ParseNodeType = YSParseNode.NodeType;

public class YSRDParser
{
	public static string CONSOLE_TAG = "[Parser]";
	public static bool DEBUG = false;
	public static bool VERBOSE = false;
	readonly List<Token> program_tokens;
	Stack<YSParseNode> parse_nodes;
	int PC = 0;
	int ERR_COUNT = 0;

	Token previous
	{
		get{
			return program_tokens [PC - 1];
		}
	}

	Token current
	{
		get {
			if (PC < program_tokens.Count)
				return program_tokens [PC];
			else
				return new Token (-1, YSToken.TokenType.EOF, "EOF");
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
		Debug ("Last node type " + node.Type);
		return node;
	}

	void PushParseNode(ParseNodeType Type)
	{
		Debug ("Pushing " + Type + " Stack height " + parse_nodes.Count); 
		YSParseNode n = new YSParseNode (Type, current);
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

	public YSRDParser (List<Token> tokens)
	{
		program_tokens = tokens;
		PC = 0;
		ERR_COUNT = 0;
		parse_nodes = new Stack<YSParseNode> ();
	}

	public int Parse()
	{
		return Program ();
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

	void ArrayDimensions()
	{
		if (Accept (Type.LSBraket)) {
			PushParseNode (ParseNodeType.ArrayDimensions);
			Expression ();
			while (Accept (Type.Comma)) {
				Expression ();
			}
			Expect (Type.RSBraket);
			PopAndInsertParseNode ();
		}
	}

	bool DataType (bool Expect_Sub, out bool IsArray)//, out YSToken TypeToken, out bool IsArray)
	{
		Debug ("Checking if " + current.Type + " is a datatype"); 

		Type t = current.Type;
		if (DataTypeSimple()) {
			PushParseNode (ParseNodeType.DataType);
			CreateTerminalChild (current);
			//TypeToken = current;
			PC++;
			IsArray = false;
			PopAndInsertParseNode ();
			return true;
		} else if (t == Type.Array) {
			PushParseNode (ParseNodeType.DataType);
			Accept (Type.Array);
			CreateTerminalChild (previous);
			Expect (Type.Of);
			if (!DataTypeSimple ())
				Error ("Expecting a data type after array of");
			CreateTerminalChild (current);
			//TypeToken = current;
			PC++;
			ArrayDimensions ();
			IsArray = true;
			PopAndInsertParseNode ();
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a data type");
			Debug ("Not a datatype");
			//TypeToken = new YSToken (-1, YSToken.TokenType.None, "");
			IsArray = false;
			return false;
		}
	}

	bool DataTypeSimple()
	{
		Type t = current.Type;
		if (t == Type.Number || t == Type.Boolean || t == Type.Text) {
			Debug ("Is datatype");
			return true;
		} else {
			return false;
		}
	}

	//TODO
	//1) Expression List Parsing

	/* programs		:= [ { header-line } ] { statement }
	 * 
	 * header-line	:= import textdata;
	 * 				| promise <planed feature>
	 * 
	 * statement 	:= set identity "=" expression ;
	 * 				| var-create ;
	 *				| condition
	 *				| loop
	 *				| call identity
	 *				| function
	 *				| output expression
	 *				| comment
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
     * 	ident		:= [ global .]identity | ident-arr | ident-str | ident-fun
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

	int Program()
	{
		YSParseNode n = new YSParseNode (ParseNodeType.Program, program_tokens[0]);
		parse_nodes.Push (n);

		//parse headers

		PushParseNode (ParseNodeType.Header);
		while (Header ())
			;
		PopAndInsertParseNode ();

		//bool FATAL = false;

		Debug ("Beginning parsing of " + program_tokens.Count + " tokens");
		while (!EOF) {
			try {
				Statement ();
			} catch(ParseException) {
				Panic ();
				ERR_COUNT++;
			}
		}
		Debug ("End of parsing, Error Count: " + ERR_COUNT);
		return ERR_COUNT;
	}

	void Panic()
	{
		//Panic Mode
		//skip tokens until a synchronizing token is found
		//sync token ";"
		//discard the current parsenode
		while (current_node.Type != ParseNodeType.Statement)
			PopParseNode ();
		PopParseNode ();
		while (!EOF && !Accept (Type.Semicolon)) {
			Debug(String.Format("[Panic Mode] Discarding token {0} [Type: {1}]", current.Content, current.Type));
			PC++;
		}
	}

	bool Header()
	{
		if (Accept (Type.Import)) {
			PushParseNode (ParseNodeType.Import);
			Expect (Type.TextData);
			CreateTerminalChild (previous);
			Expect (Type.Semicolon);
			PopAndInsertParseNode ();
			return true;
		} else {
			return false;
		}
	}

	void Statement()
	{
		PushParseNode (ParseNodeType.Statement);
		bool PAI = false;
		if (Accept (Type.Comment)) {
			PopParseNode ();
			PAI = true;
		} else if (Accept (Type.Set)) {
			PushParseNode (ParseNodeType.Set);
			Identity (true, true);
			Expect (Type.Assign);
			Expression ();
			Expect (Type.Semicolon);
			PopAndInsertParseNode ();
		} else if (Accept (Type.Call)) {
			PushParseNode (ParseNodeType.Call);
			Expect (Type.Identity);
			PopAndInsertParseNode ();
			Expect (Type.Semicolon);
		} else if (Accept(Type.Output)) {
			//Print statement
			PushParseNode (ParseNodeType.Output);
			Expression ();
			PopAndInsertParseNode ();
			Expect (Type.Semicolon);
		} else if (VarCreate (false)) {
			Expect (Type.Semicolon);
		} else if (Condition (false)) {
		} else if (Structure (false)) {
		} else if (Function (false)) {
		} else if (Loop (false)) {
		} else if (Accept (Type.Return)) {
			PopParseNode ();
			PushParseNode (ParseNodeType.FunctionReturn);
			Expression ();
			Expect (Type.Semicolon);
			PopAndInsertParseNode ();
			PAI = true;
		} else {
			Error ("Unrecognized statement");
		}
		if(!PAI)
			PopAndInsertParseNode ();
	}

	bool VarCreate(bool Expect_Sub)
	{
		Debug ("Creation...(" + current.Content + " type " + current.Type + ")");

		//IdentityType itype = STATE.TranslateTokenTypeToIdentityType (current.Type);

		//if (itype != IdentityType.Function && itype != IdentityType.Structure && itype != IdentityType.Unknown) {
		//bool IsArray;
		//Token TypeToken;
		PushParseNode (ParseNodeType.VarCreate);

		bool IsArray;
		if(DataType (Expect_Sub, out IsArray)){//, out TypeToken, out IsArray);
			if (!IsArray) {
				VarCreatePrimitiveHelper ();
				while (Accept (Type.Comma)) {
					VarCreatePrimitiveHelper ();
				}

				PopAndInsertParseNode ();
				return true;
			} else if (IsArray) {
				Expect (Type.Identity);
				CreateTerminalChild (previous);
				Expect (Type.Assign);
				if (ExpressionList ()) {
				} else {
					Expression ();
				}

				PopAndInsertParseNode ();
				return true;
			} else {
				Error ("Cannot initialize data type");
				PopParseNode ();
				return false;
			}
		} else {
			if (Expect_Sub)
				Error ("Expecting a Data member creation");
			PopParseNode ();
			return false;
		}
	}

	void VarCreatePrimitiveHelper()
	{
		Debug ("Primitive Creation...(" + current.Content + " type " + current.Type + ")");
		PushParseNode (ParseNodeType.VarPrimitive);

		Identity (true, true);
		//string variableName = previous.Content;
		if (Accept (Type.Assign)) {
			Expression ();
		} else {
			Verbose ("Creating a primitive variable without assignment");
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

		if (Accept (Type.If)) {
			PushParseNode (ParseNodeType.Condition);
			Expect (Type.LParen);
			Expression ();
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
			Expression ();
			Expect (Type.RParen);
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
			Expect (Type.Identity);
			CreateTerminalChild (previous);

			if (Accept (Type.Child)) {
				CreateTerminalChild (previous);
				Expect (Type.Of);
				Expect (Type.Identity);
				CreateTerminalChild (previous);
			}
			Expect (Type.LCBraket);
			//TODO implement Structure inside structure
			VarCreate (true);
			Expect (Type.Semicolon);
			while (VarCreate (false)) {
				Expect (Type.Semicolon);
			}
			Expect (Type.RCBraket);
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

		if (Accept (Type.Function)) {
			PushParseNode (ParseNodeType.Function);

			Expect (Type.Identity);
			CreateTerminalChild (previous);
			string functionName = previous.Content;
			PushParseNode (ParseNodeType.FunctionParamList);

			Expect (Type.LParen);
			if (!Accept (Type.RParen)) {
				Debug (current.Content);
				bool IsArray;
				if (DataType (false, out IsArray)) {
					//CreateTerminalChild (previous);
					Expect (Type.Identity);
					CreateTerminalChild (previous);
				}
				while (Accept (Type.Comma)) {
					DataType (true, out IsArray);
					//CreateTerminalChild (previous);
					Expect (Type.Identity);
					CreateTerminalChild (previous);
				}
			}
			Expect (Type.RParen);
			PopAndInsertParseNode ();

			Expect (Type.Colon);
			bool IsA;
			DataType (true, out IsA);
			//CreateTerminalChild (previous);
			Block (true);

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

	void Expression ()
	{
		PushParseNode (ParseNodeType.Expression);
		ExprLogic  ();
		PopAndInsertParseNode (); 
	}

	bool ExpressionList()
	{
		if (Accept (Type.LSBraket)) {
			PushParseNode (ParseNodeType.ExpressionList);
			if (!Accept (Type.RSBraket)) {
				if(!ExpressionList())
					Expression ();
				while (Accept (Type.Comma)) {
					if(!ExpressionList())
						Expression ();
				}
			}
			Expect (Type.RSBraket);
			PopAndInsertParseNode ();
			return true;
		} else {
			return false;
		}
	}

	void ExprLogic ()
	{
		PushParseNode (ParseNodeType.ExpressionLogic);
		if (Accept (Type.Not)) {
			CreateTerminalChild (previous);
		}
		ExprBool ();
		while (Accept (Type.And) || Accept (Type.Or)) {
			CreateTerminalChild (previous);
			//PushParseNode (ParseNodeType.ExpressionLogic);
			if (Accept (Type.Not)) {
				CreateTerminalChild (previous);
			}
			ExprBool ();
			//PopAndInsertParseNode ();
		}
		PopAndInsertParseNode ();
	}

	void ExprBool ()
	{
		PushParseNode (ParseNodeType.ExpressionBoolean);
		ExprNum  ();
		while (CompOpr (false)) {
			CreateTerminalChild (previous);
			//PushParseNode (ParseNodeType.ExpressionBoolean);
			ExprNum  ();
			//PopAndInsertParseNode ();
		}
		PopAndInsertParseNode ();
	}

	void ExprNum ()
	{
		PushParseNode (ParseNodeType.ExpressionNumber);
		if (Accept (Type.Minus)) {
			CreateTerminalChild (previous);
		}
		ExprTerm  ();
		while (Accept (Type.Plus) || Accept(Type.Minus)) {
			CreateTerminalChild (previous);
			//PushParseNode (ParseNodeType.ExpressionNumber);
			if (Accept (Type.Minus)) {
				CreateTerminalChild (previous);
			}
			ExprTerm ();
			//PopAndInsertParseNode ();
		}
		PopAndInsertParseNode ();
	}

	void ExprTerm ()
	{
		Debug ("Term - token: " + current.Content);
		PushParseNode (ParseNodeType.ExpressionTerm);
		ExprFactor ();
		while (Accept (Type.Asterisk) || Accept(Type.Slash) || Accept(Type.Percentage)) {
			CreateTerminalChild (previous);
			//PushParseNode (ParseNodeType.ExpressionTerm);
			ExprFactor ();
			//PopAndInsertParseNode ();
		}
		PopAndInsertParseNode ();
	}

	void ExprFactor ()
	{
		PushParseNode (ParseNodeType.ExpressionFactor);

		Debug ("Factor - token: " + current.Content);
		if (Identity(false)) {
		} else if (Accept(Type.NumberData)) {
			PushParseNode (ParseNodeType.Number);
			CreateTerminalChild (previous);
			PopAndInsertParseNode ();
		} else if (Accept(Type.TextData)) {
			PushParseNode (ParseNodeType.Text);
			CreateTerminalChild (previous);
			PopAndInsertParseNode ();
		} else if (Accept (Type.LParen)) {
			PushParseNode (ParseNodeType.Expression);
			Expression  ();
			Expect (Type.RParen);
			PopAndInsertParseNode ();
		} else {
			Error ("Expecting a data, a number, operator or other identity of an expression");
			PopParseNode ();
		}

		PopAndInsertParseNode ();
	}

	bool CompOpr(bool Expect_Sub)
	{
		if (Accept (Type.Equals) || Accept (Type.GreaterThan) || Accept (Type.LessThan) 
			|| Accept (Type.GreaterThanEqual) || Accept (Type.LessThanEqual)) {
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
		} /*else if (ParentIdentity.Count > 0) {
			PushParseNode (ParseNodeType.Parent);
			for (int i = 0; i < ParentIdentity.Count; i++) {
				CreateTerminalChild (ParentIdentity [i]);
			}
			PopAndInsertParseNode ();
		}*/
	}

	bool Identity(bool Expect_Sub)
	{
		return Identity (Expect_Sub, false);
	}

	bool Identity(bool Expect_Sub, bool dataType)
	{
		Debug ("Identity - token: " + current.Content);
		//Reset Global and Parent Nodes
		GlobalIdentity = null;
		ParentIdentity = new List<Token> ();
		Token identityToken = null;
		if (Accept (Type.Global)) {
			GlobalIdentity = previous;
			Expect (Type.Period);
		}/* else if (Accept (Type.Parent)) {
			ParentIdentity.Add (previous);
			while (Accept (Type.Period))
				if (Accept (Type.Parent)) {
					ParentIdentity.Add (previous);
				} else if (Accept (Type.Identity)) {
					identityToken = previous;
					break;
				} else {
					Error ("Parent keyword must be followed by another parent keyword or identity");
				}
		}*/
		if (identityToken != null || Accept (Type.Identity)) {
			identityToken = previous;
			if (Accept (Type.LParen)) {
				if (dataType)
					Error ("Expecting a data type, not a function");
				IdentityFunction (identityToken);
			} else if(Accept(Type.LSBraket)){
				IdentityArray (identityToken);
			} else if (Accept (Type.Period)) {
				IdentityStructure (identityToken);
			} else {
				IdentityPrimitive (identityToken);
			}
			return true;
		} else {
			if (Expect_Sub)
				Error ("Expecting a resolvable identity/variable");
			return false;
		}
	}

	void IdentityPrimitive(YSToken identityToken)
	{
		//Identity Simple
		PushParseNode (ParseNodeType.Identity);
		PushGlobalAndParentKW ();
		CreateTerminalChild (identityToken);
		PopAndInsertParseNode ();
	}

	void IdentityArray(YSToken identityToken)
	{
		PushParseNode (ParseNodeType.IdentityArray);
		PushGlobalAndParentKW ();
		CreateTerminalChild (identityToken);
		Expression ();
		Expect (Type.RSBraket);
		PopAndInsertParseNode ();
	}

	void IdentityStructure(YSToken identityToken)
	{
		//Identity Structure
		PushParseNode (ParseNodeType.IdentityStructure);
		PushGlobalAndParentKW ();
		CreateTerminalChild (identityToken);
		Identity (true);
		while (Accept (Type.Period))
			Identity (true);
		PopAndInsertParseNode ();
	}

	void IdentityFunction(YSToken identityToken)
	{
		//Identity Function
		PushParseNode (ParseNodeType.IdentityFunction);
		PushGlobalAndParentKW ();
		CreateTerminalChild (identityToken);
		if (Accept (Type.RParen)) {
			PopAndInsertParseNode ();
			return;
		}
		PushParseNode (ParseNodeType.FunctionParamList);
		Expression ();
		while (Accept(Type.Comma))
			Expression ();
		Expect (Type.RParen);
		PopAndInsertParseNode ();
		PopAndInsertParseNode ();
	}

	void Stop()
	{
		program_tokens.Clear ();
	}

	void Verbose(String s)
	{
		string name = current.Content;
		string location = "" + current.Position;
		if(VERBOSE)
			Console.WriteLine (String.Format(CONSOLE_TAG + "[Verbose near {0} @ {1}] {2} ", name, location, s));
	}

	void Debug(String s)
	{
		string name = current.Content;
		string location = "" + current.Position;
		if(DEBUG)
			Console.WriteLine (String.Format(CONSOLE_TAG + "[Debug near {0} @ {1}] {2} ", name, location, s));
	}

	void Error(String s)
	{
		string name = current.Content;
		string location = "" + current.Position;
		Console.WriteLine (String.Format(CONSOLE_TAG + "[Error near {0} @ {1}] {2} ", name, location, s));
		throw new ParseException ("Parse Exception");
	}

	public class ParseException : Exception
	{
		public ParseException(string msg) : base(msg) {
		}
	}
}
