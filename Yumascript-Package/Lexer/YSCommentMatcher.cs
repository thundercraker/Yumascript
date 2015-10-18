using System;
using System.Text;
using System.Collections.Generic;

public class YSCommentMatcher : YSMatcherBase
{
	public static string COMMENT_START = "\\*";
	public static string COMMENT_END = "*\\";

	public YSCommentMatcher ()
	{

	}

	protected override YSToken IsMatchImpl (YSTokenizer tokenizer)
	{
		//Console.WriteLine("Attempting to match Comment String");
		var str = new StringBuilder ();
		int location = -1;

		//Match to comment start
		foreach (char character in COMMENT_START) {
			if (character.ToString().Equals (tokenizer.Current)) {
				tokenizer.Consume ();
			} else {
				return null;
			}
		}

		location = tokenizer.Location;

		bool ProperEnd = false;
		while (!tokenizer.End()) {
			//Console.WriteLine (tokenizer.Current + " " + tokenizer.End());
			bool MatchedEnd = true;
			foreach (char character in COMMENT_END) {
				if (character.ToString().Equals (tokenizer.Current)) {
					str.Append (tokenizer.Current);
					tokenizer.Consume ();
				} else {
					MatchedEnd = false;
					break;
				}
			}

			if (!MatchedEnd) {
				str.Append (tokenizer.Current);
				tokenizer.Consume ();
			} else {
				str.Remove (str.Length - COMMENT_END.Length, (COMMENT_END.Length));
				ProperEnd = true;
				break;
			}
		}

		if (tokenizer.End ())
			return null;

		if (ProperEnd) {
			return new YSToken(location, YSToken.TokenType.Comment, str.ToString());
		}

		return null;
	}

	public override string Identifier()
	{
		return "Matching Comment String";
	}
}


