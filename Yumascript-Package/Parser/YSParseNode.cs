using System;
using System.Collections.Generic;

public class YSParseNode
{
	public enum NodeType { Program, Header, Import, Statement, Set, Call, Output, Global, Parent, VarCreate, VarPrimitive, Condition, Loop, Function, FunctionParamList, FunctionArgList, FunctionReturn, FunctionBody, Structure, Block, Expression, ExpressionList, ArrayDimensions,
		ExpressionLogic, ExpressionBoolean, ExpressionNumber, ExpressionTerm, ExpressionFactor, Number, Text, Identity, IdentityArray, IdentityFunction,
		IdentityStructure, Terminal, TypeName, DataType, ArrayInit };

	public readonly NodeType Type;
	public List<YSParseNode> Children;
	public readonly YSToken Token;

	public YSParseNode ()
	{
		Type = NodeType.Program;
		Children = new List<YSParseNode> ();
	}

	public YSParseNode(NodeType Type, YSToken Token)
	{
		this.Type = Type;
		this.Token = Token;
		Children = new List<YSParseNode> ();
	}

	public YSParseNode(YSToken Token)
	{
		this.Token = Token;
		this.Type = NodeType.Terminal;
		Children = new List<YSParseNode> ();
	}

}
