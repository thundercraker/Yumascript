using System;
using System.IO;
using System.Collections.Generic;

public class YSLinker
{
	public static string CONSOLE_TAG = "[Linker]";
	public static bool DEBUG = false;
	public static bool VERBOSE = false;

	string current_resource;

	public struct Resource {
		public string Name;
		public string Content;
	}

	public YSLinker ()
	{
		current_resource = "";
	}

	public Resource[] LoadResources(string[] resource_links)
	{
		List<Resource> Resources = new List<Resource> ();
		foreach (string resource_link in resource_links) {
			Resources.Add (GetRawFromResourceLocation (resource_link));
		}
		return Resources.ToArray ();
	}

	public Resource GetRawFromResourceLocation(string ResourceLocation)
	{
		current_resource = ResourceLocation;

		//simple implementation
		try
		{   
			string raw = "";
			using (StreamReader sr = new StreamReader(ResourceLocation + ".ys"))
			{
				raw = sr.ReadToEnd();
			}
			Resource R;
			R.Name = ResourceLocation;
			R.Content = raw;
			return R;
		}
		catch (IOException e)
		{
			Error ("File could not be read");
		}
		return new Resource();
	}

	void Verbose(String s)
	{
		if(VERBOSE)
			Console.WriteLine (String.Format(CONSOLE_TAG + "[Verbose for ({0})] {1} ", current_resource, s));
	}

	void Debug(String s)
	{
		if(DEBUG)
			Console.WriteLine (String.Format(CONSOLE_TAG + "[Debug for ({0})] {1} ", current_resource, s));
	}

	void Error(String s)
	{
		Console.WriteLine (String.Format(CONSOLE_TAG + "[Error for ({0})] {1} ", current_resource, s));
		throw new LinkerException ("Linker Exception");
	}

	public class LinkerException : Exception
	{
		public LinkerException(string msg) : base(msg) {
		}
	}
}

