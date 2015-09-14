using System;
using System.Text;
using System.Linq;

	public class YSNumberMatcher : YSMatcherBase
	{
		public static string DOT = ".";

		protected override YSToken IsMatchImpl (YSTokenizer tokenizer)
		{
			int location = tokenizer.Location;
			bool dotfound = false;

			StringBuilder str = new StringBuilder ();

			//first character of an identity cannot be a number

			while (!tokenizer.End ()
				&& !String.IsNullOrWhiteSpace (tokenizer.Current)) {

				bool specialcharfound = YSToken.SpecialCharacters.Any (character => character.Word == tokenizer.Current);

				if (tokenizer.Current == DOT) {
					if (!dotfound) {
						dotfound = true;
						str.Append (tokenizer.Current);
						tokenizer.Consume();
						continue;
					} else if (dotfound) {
						break;
					}
				} else if (specialcharfound) {
					break;
				}

				int intval = 0;
				if (int.TryParse (tokenizer.Current, out intval)) {
					str.Append (tokenizer.Current);
					tokenizer.Consume ();
				} else {
					return null;
				}
			}

			if(str.Length > 0)
				return new YSToken (location, YSToken.TokenType.NumberData, str.ToString ());
			return null;
		}

	public override string Identifier()
	{
		return "Matching number";
	}
	}

