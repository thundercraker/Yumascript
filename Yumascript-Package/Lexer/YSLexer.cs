using System;
using System.Collections.Generic;
using System.Linq;

public class YSLexer
{
	public static bool DEBUG = false;

	public YSTokenizer Tokenizer;
	List<YSMatcherBase> Matchers;
	List<YSToken> Tokens;

	public YSLexer (string source)
	{
		Tokens = new List<YSToken> ();
		Tokenizer = new YSTokenizer (() => source.ToCharArray ().Select (i => i.ToString ()).ToList ());
		Matchers = new List<YSMatcherBase> ();
		Matchers.Add (new YSQuotedStringMatcher ());
		Matchers.Add (new YSCommentMatcher ());
		Matchers.AddRange (YSToken.SpecialCharacters);
		Matchers.AddRange (YSToken.KeywordCharacters);
		Matchers.Add (new YSNumberMatcher ());
		Matchers.Add (new YSWhiteSpaceMatcher());
		Matchers.Add (new YSIdentityMatcher ());

		Debug(((YSMatcherBase) Matchers[0]).Identifier());

		int safety = 0;
		while (!Tokenizer.End () && ++safety < 1000) {
			int location = Tokenizer.Location;
			foreach (YSMatcherBase Matcher in Matchers) {
				YSToken token = Matcher.IsMatch (Tokenizer);
				if (token != null) {
					Debug (token.Type + " @ " + token.Position + " Content: " + token.Content);
					if(token.Type != YSToken.TokenType.WhiteSpace && token.Type != YSToken.TokenType.Newline)
						Tokens.Add (token);
					break;
				}
			}
			if (Tokenizer.Location == location) {
				Debug ("Unrecognized character " + Tokenizer.Current + " @ " + Tokenizer.Location);
				break;
			}
		}
		Debug ("Finished lexing");
	}

	public static void Debug(String s)
	{
		if(DEBUG)
			Console.WriteLine ("[Debug] " + s);
	}

	public void Error(String s)
	{
		Console.WriteLine ("[Error] " + s);
		throw new Exception ("Lexer Exception");
	}

	public List<YSToken> GetTokenList()
	{
		return Tokens;
	}
}

