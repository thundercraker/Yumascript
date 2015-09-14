using System;
using System.Collections.Generic;


	public class YSToken
	{
		public enum TokenType
		{
			//Special Characters
			RImplies,
			LCBraket,
			RCBraket,
			LSBraket,
			RSBraket,
			LParen,
			RParen,
			Plus,
			Minus,
			Assign,
			Comma,
			Asterisk,
			Slash,
			Carat,
			GreaterThan,
			LessThan,
			GreaterThanEqual,
			LessThanEqual,
			Semicolon,
			Colon,
			Period,
			Hashtag,
			EOF,

			//Keywords
			Number,
			Text,
			Boolean,
			NumberData,
			TextData,
			Array,
			Child,
			Of,
			Call,
			If,
			Then,
			While,
			Function,
			Structure,
			Do,
			Nil,
			And,
			Or,
			Equals,
			Not,

			Return,
			Newline,
			WhiteSpace,

			//Helpers
			Identity,
			Partial,
			None
		}

		public static List<YSWordMatcher> SpecialCharacters = new List<YSWordMatcher>
		{
			new YSWordMatcher(TokenType.RImplies, 			"<-"),
			new YSWordMatcher(TokenType.LCBraket, 			"{"),
			new YSWordMatcher(TokenType.RCBraket, 			"}"),
			new YSWordMatcher(TokenType.LSBraket, 			"["),
			new YSWordMatcher(TokenType.RSBraket, 			"]"),
			new YSWordMatcher(TokenType.LParen, 			"("),
			new YSWordMatcher(TokenType.RParen, 			")"),
			new YSWordMatcher(TokenType.Plus, 				"+"),
			new YSWordMatcher(TokenType.Minus, 				"-"),
			new YSWordMatcher(TokenType.Comma, 				","),
			new YSWordMatcher(TokenType.Asterisk, 			"*"),
			new YSWordMatcher(TokenType.Slash, 				"/"),
			new YSWordMatcher(TokenType.Carat, 				"^"),
			new YSWordMatcher(TokenType.GreaterThan, 		">"),
			new YSWordMatcher(TokenType.LessThan, 			"<"),
			new YSWordMatcher(TokenType.GreaterThanEqual, 	">="),
			new YSWordMatcher(TokenType.LessThanEqual, 		"<="),
			new YSWordMatcher(TokenType.Assign, 			"="),
			new YSWordMatcher(TokenType.Semicolon, 			";"),
			new YSWordMatcher(TokenType.Colon, 				":"),
			new YSWordMatcher(TokenType.Period, 			"."),
			new YSWordMatcher(TokenType.Hashtag, 			"#"),
			new YSWordMatcher(TokenType.Return, 			"\r"),
			new YSWordMatcher(TokenType.Newline, 			"\n")
		};

		public static List<YSWordMatcher> KeywordCharacters = new List<YSWordMatcher>
		{
			new YSWordMatcher(TokenType.Number, 	"number"),
			new YSWordMatcher(TokenType.Text, 		"text"),
			new YSWordMatcher(TokenType.Boolean, 		"bool"),
			new YSWordMatcher(TokenType.Array, 		"array"),
			new YSWordMatcher(TokenType.Child, 		"child"),
			new YSWordMatcher(TokenType.Of, 		"of"),
			new YSWordMatcher(TokenType.Call, 		"call"),
			new YSWordMatcher(TokenType.If, 		"if"),
			new YSWordMatcher(TokenType.Then, 		"then"),
			new YSWordMatcher(TokenType.While, 		"while"),
			new YSWordMatcher(TokenType.Do, 		"do"),
			new YSWordMatcher(TokenType.Function, 	"function"),
			new YSWordMatcher(TokenType.Structure, 	"structure"),
			new YSWordMatcher(TokenType.Return, 	"return"),
			new YSWordMatcher(TokenType.Nil, 		"nil"),
			new YSWordMatcher(TokenType.And, 		"and"),
			new YSWordMatcher(TokenType.Or, 		"or"),
			new YSWordMatcher(TokenType.Equals, 	"equals"),
			new YSWordMatcher(TokenType.Not, 		"not")
		};

		public TokenType Type;
		public int Position;
		public String Content;

		public YSToken(int p, TokenType t, string c)
		{
			Type = t;
			Position = p;
			Content = c;
		}

		public bool Is(TokenType t)
		{
			return Type == t;
		}
	}
