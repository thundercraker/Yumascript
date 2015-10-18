using System;
using System.IO;

class MainClass
{
	public static void Main (string[] args)
	{
		String raw = "";

		try
		{   
			using (StreamReader sr = new StreamReader("YSCode.ys"))
			{
				raw = sr.ReadToEnd();
			}
			YSLexer.DEBUG 		= true;
			YSRDParser.DEBUG	= true;
			YSInterpreter.DEBUG = true;
			YSStateModule.DEBUG	= true;

			YSRDParser parser = new YSRDParser (raw);
			if(parser.Parse() <= YSInterpreter.ERR_ACCEPT){
				YSInterpreter interpreter = new YSInterpreter (parser.PopLast());
				interpreter.Interpret();
			} else
				Console.WriteLine("Interpreter not started, parsing was aborted");
		}
		catch (IOException e)
		{
			Console.WriteLine("The file could not be read:");
			Console.WriteLine(e.Message);
		}
	}
}
