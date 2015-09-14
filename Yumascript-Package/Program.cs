using System;
using System.Linq;

	class MainClass
	{
		public static void Main (string[] args)
		{
			//YSLexer YSL = new YSLexer ();
			//var Tokenizer = new YSTokenizer(() => "\"number\" n = 1 + 3;".ToCharArray().Select(i => i.ToString()).ToList());

			/*var lexer = new YSLexer ("\"number\" n = 1 + 3;");

			Console.WriteLine ("Token Indices: ");
			//Tokenizer.Merge (0, 6);
			lexer.Tokenizer.PrintCharacters ();
			lexer.Tokenizer.PrintTokenIndices();*/
		String raw = "number n = 1; n = n * n; function n (number n) : number { return n * n; }";

		raw = "structure t { number one; text two; } " +
			"function square(number n) : number { return n*n; } " +
			"square(10); " +
			"number n = 5; " +
			"number x = square(n); " +
			"t.one = x; " +
			"t.two = \"Eyy\";";

		YSRDParser parser = new YSRDParser (raw);
		YSInterpreter interpreter = new YSInterpreter (parser.PopLast());
		interpreter.Program ();
		}
	}
