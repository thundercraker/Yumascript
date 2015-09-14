using System;
using System.Text;
using System.Collections.Generic;

	public class YSQuotedStringMatcher : YSMatcherBase
	{
		public static string DOUBLE_QUOTE = "\"";
		public static string SINGLE_QUOTE = "'";
		public HashSet<string> Delims = new HashSet<string>
		{
			DOUBLE_QUOTE, SINGLE_QUOTE
		};

		public YSQuotedStringMatcher ()
		{
			
		}

		protected override YSToken IsMatchImpl (YSTokenizer tokenizer)
		{
			//Console.WriteLine("Attempting to match Quoted String");
			var str = new StringBuilder ();
			bool completeString = false;
			int location = -1;

			//Console.WriteLine (Delims.Contains (tokenizer.Current));
			if (Delims.Contains (tokenizer.Current)) {
				String current_delim = tokenizer.Current;
				location = tokenizer.Location;

				tokenizer.Consume ();

				while (!tokenizer.End() && !tokenizer.Current.Equals (current_delim)) {
					//Console.WriteLine (tokenizer.Current + " " + tokenizer.End());
					str.Append (tokenizer.Current);
					tokenizer.Consume();
				}

				//Console.WriteLine (str.ToString ());

				if (tokenizer.End ())
					return null;
				
				if(tokenizer.Current.Equals(current_delim))
				{
					tokenizer.Consume();
					completeString = true;
				}
			}

			if (completeString) {
				return new YSToken(location, YSToken.TokenType.TextData, str.ToString());
			}

			return null;
		}
	public override string Identifier()
	{
		return "Matching Quoted String";
	}
	}


