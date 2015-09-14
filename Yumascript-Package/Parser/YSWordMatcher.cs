using System;
using System.Globalization;
using System.Linq;

	public class YSWordMatcher :YSMatcherBase
	{
		public YSToken.TokenType Type;
		public String Word;

		public YSWordMatcher(YSToken.TokenType t, String w)
		{
			Type = t;
			Word = w;
		}

		protected override YSToken IsMatchImpl(YSTokenizer tokenizer)
		{
			string compile = "";
			int location = tokenizer.Location;
			var ctok = tokenizer.Current;
			foreach (var character in Word)
			{
				if (tokenizer.Current == character.ToString(CultureInfo.InvariantCulture))
				{
					tokenizer.Consume();
					compile += tokenizer.Current;
				}
				else
				{
					return null;
				}
			}

			bool endOfToken, complete;

			var next = tokenizer.Current;
			
			endOfToken = String.IsNullOrWhiteSpace(next) 
			|| YSToken.SpecialCharacters.Any(character => character.Word == next)
			|| YSToken.SpecialCharacters.Any(character => character.Word == ctok);
			
			//complete = compile.Equals (Word);
			
			if (endOfToken)
			{
				return new YSToken(location, Type, Word);
			}

			return null;
		}

	public override string Identifier()
	{
		return "Matching: " + Word;
	}
	}

