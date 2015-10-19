using System;
using System.IO;

public class Yumascript
{
	YSStateModule State;

	public Yumascript (bool DEBUG_MODE)
	{
		YSLexer.DEBUG 		= DEBUG_MODE;
		YSRDParser.DEBUG	= DEBUG_MODE;
		YSInterpreter.DEBUG = DEBUG_MODE;
		YSStateModule.DEBUG	= DEBUG_MODE;
		State =  new YSStateModule ();
	}

	public void Run(string ProgramPath)
	{
		try
		{   
			string raw;
			using (StreamReader sr = new StreamReader(ProgramPath))
			{
				raw = sr.ReadToEnd();
			}
			bool status = Yumascript.LPIProcess(ref State, raw);
			Console.WriteLine("Program has finished with status " + status);
		}
		catch (IOException e)
		{
			Console.WriteLine("The file could not be read:");
			Console.WriteLine(e.Message);
		}
	}

	public static bool LPIProcess(ref YSStateModule state, string raw)
	{
		bool result = false;
		YSLexer lexer = new YSLexer(raw);
		YSRDParser parser = new YSRDParser (lexer.GetTokenList());
		if(parser.Parse() <= YSInterpreter.ERR_ACCEPT){
			YSInterpreter interpreter = new YSInterpreter (parser.PopLast());
			result = interpreter.Interpret(ref state);
		} else
			Console.WriteLine("Interpreter not started, parsing was aborted");
		return result;
	}
}

