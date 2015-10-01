using System;

public abstract class YSMatcherBase
{
	public YSToken IsMatch(YSTokenizer tokenizer)
	{
		if (tokenizer.End())
		{
			return new YSToken(tokenizer.Location, YSToken.TokenType.EOF, null);
		}

		//YSLexer.Debug ("Current Token: " + tokenizer.Current + " Consume Count " + tokenizer.ConsumeCount);

		var match = IsMatchImpl(tokenizer);
		
		//YSLexer.Debug (tokenizer.Current + " Consume Count " + tokenizer.ConsumeCount);
		if (match == null)
			YSLexer.Debug ("No match for " + Identifier ());
		else
			YSLexer.Debug (Identifier() + " matched " + match.Content);
	
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

