using System;

	public class YSWhiteSpaceMatcher : YSMatcherBase
	{
		protected override YSToken IsMatchImpl (YSTokenizer tokenizer)
		{
			bool ws = false;

			int location = tokenizer.Location;

			while (!tokenizer.End () && String.IsNullOrWhiteSpace (tokenizer.Current)) {
				ws = true;
				tokenizer.Consume ();
			}

			if (ws) {
				return new YSToken (location, YSToken.TokenType.WhiteSpace, null);
			}

			return null;
		}

	public override string Identifier()
	{
		return "Matching whitespace";
	}
	}