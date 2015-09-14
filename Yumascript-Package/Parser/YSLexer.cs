using System;
using System.Collections.Generic;
using System.Linq;

	public class YSLexer
	{
		public YSTokenizer Tokenizer;
		List<YSMatcherBase> Matchers;
	List<YSToken> Tokens;

		public YSLexer (string source)
		{
		Tokens = new List<YSToken> ();
			Tokenizer = new YSTokenizer (() => source.ToCharArray ().Select (i => i.ToString ()).ToList ());
			Matchers = new List<YSMatcherBase> ();
			Matchers.AddRange (YSToken.SpecialCharacters);
			Matchers.AddRange (YSToken.KeywordCharacters);
			Matchers.Add (new YSQuotedStringMatcher ());
			Matchers.Add (new YSNumberMatcher ());
			Matchers.Add (new YSWhiteSpaceMatcher());
			Matchers.Add (new YSIdentityMatcher ());

		Console.WriteLine(((YSMatcherBase) Matchers[0]).Identifier());

			int safety = 0;
			while (!Tokenizer.End () && ++safety < 1000) {
				int location = Tokenizer.Location;
				foreach (YSMatcherBase Matcher in Matchers) {
					YSToken token = Matcher.IsMatch (Tokenizer);
					if (token != null) {
						Console.WriteLine (token.Type + " @ " + token.Position + " Content: " + token.Content);
						if(token.Type != YSToken.TokenType.WhiteSpace)
							Tokens.Add (token);
						break;
					}
				}
				if (Tokenizer.Location == location) {
					Console.WriteLine ("Unrecognized character " + Tokenizer.Current + " @ " + Tokenizer.Location);
					break;
				}
			}
		Console.WriteLine ("Finished lexing");
		}

	public List<YSToken> GetTokenList()
		{
		return Tokens;
		}
	}

