using System;
using System.IO;
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

		/*raw = "structure t { number one; text two; } " +
			"function square(number n) : number { return n*n+10; } " +
			//"square(10); " +
			"number n = 5; " +
			"number x = square(n); " +
			"set t.one = x; " +
			"set t.two = \"Eyy\";" +
			"output t.one;";*/

		try
		{   // Open the text file using a stream reader.
			using (StreamReader sr = new StreamReader("YSCode.ys"))
			{
				// Read the stream to a string, and write the string to the console.
				raw = sr.ReadToEnd();
				//Console.WriteLine(line);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("The file could not be read:");
			Console.WriteLine(e.Message);
		}

		//set debug mode
		YSLexer.DEBUG 		= false;
		YSRDParser.DEBUG	= false;
		YSInterpreter.DEBUG = false;
		YSStateModule.DEBUG	= false;

		YSRDParser parser = new YSRDParser (raw);
		YSInterpreter interpreter = new YSInterpreter (parser.PopLast());
		//interpreter.Program (interpreter.No);
		}
	}
