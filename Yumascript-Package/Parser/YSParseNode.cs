using System;
using System.Collections.Generic;

	public class YSParseNode
	{
		public enum NodeType { Program, Statement, VarCreate, VarPrimitive, Condition, Loop, Function, FunctionParamList, FunctionReturn, Structure, Block, Expression, ExpressionList,
			ExpressionLogic, ExpressionBoolean, ExpressionNumber, ExpressionTerm, ExpressionFactor, Identity, IdentityArray, IdentityFunction,
			IdentityStructure, Terminal, TypeName };

		public readonly NodeType Type;
		public List<YSParseNode> Children;
		public readonly YSToken Token;

		public YSParseNode ()
		{
			Type = NodeType.Program;
			Children = new List<YSParseNode> ();
		}

		public YSParseNode(NodeType Type)
		{
			this.Type = Type;
			Children = new List<YSParseNode> ();
		}

		public YSParseNode(YSToken Token)
		{
			this.Token = Token;
			this.Type = NodeType.Terminal;
			Children = new List<YSParseNode> ();
		}

	}
