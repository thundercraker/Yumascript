using System;
using System.Linq;
using System.Text;

	public class YSIdentityMatcher : YSMatcherBase
	{
		protected override YSToken IsMatchImpl (YSTokenizer tokenizer)
		{
			int location = tokenizer.Location;

			StringBuilder str = new StringBuilder ();

			//first character of an identity cannot be a number
			int intval = 0;
			if(int.TryParse(tokenizer.Current, out intval))
				return null;

			while (!tokenizer.End ()
			      && !String.IsNullOrWhiteSpace (tokenizer.Current)
			      && !YSToken.SpecialCharacters.Any (character => character.Word == tokenizer.Current)) {
				str.Append (tokenizer.Current);
				tokenizer.Consume ();
			}

			if(str.Length > 0)
				return new YSToken (location, YSToken.TokenType.Identity, str.ToString ());
			return null;
		}

	public override string Identifier()
	{
		return "Matching identity ";
	}
	}

