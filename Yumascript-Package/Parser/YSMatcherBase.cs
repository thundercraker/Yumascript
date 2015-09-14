using System;

	public abstract class YSMatcherBase
	{
		public YSToken IsMatch(YSTokenizer tokenizer)
		{
			if (tokenizer.End())
			{
				return new YSToken(tokenizer.Location, YSToken.TokenType.EOF, null);
			}

		//Console.WriteLine ("Current Token: " + tokenizer.Current + " Consume Count " + tokenizer.ConsumeCount);

			var match = IsMatchImpl(tokenizer);
			
		//Console.WriteLine (tokenizer.Current + " Consume Count " + tokenizer.ConsumeCount);
		if (match == null)
			Console.WriteLine ("No match for " + Identifier ());
		else
			Console.WriteLine (Identifier() + " matched " + match.Content);
		
			if (match == null)
			{
				tokenizer.Rollback();
			}
			else
			{
				tokenizer.Commit();
			}

			return match;
		}

		protected abstract YSToken IsMatchImpl(YSTokenizer tokenizer);
	public abstract string Identifier();
	}

