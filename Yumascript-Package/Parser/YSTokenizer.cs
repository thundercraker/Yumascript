using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

	public class YSTokenizer
	{
		public List<int> TokenIndices;

		private int Index{ get; set; }
		public int ConsumeCount{ get; set; }
		private List<string> Characters;
		private Stack<int> RollbackIndex;
		private Stack<List<int>> RollbackList;

		public YSTokenizer(Func<List<string>> source)
		{
			Index = 0;
			Characters = source ();
			TokenIndices = new List<int> ();
			RollbackIndex = new Stack<int> ();
			RollbackList = new Stack<List<int>> ();
			for (int i = 0; i < Characters.Count; i++) {
				TokenIndices.Add (i);
			}
		}

		public bool EOF(int lookahead)
		{
			if (Index + lookahead >= TokenIndices.Count) {
				return true;
			}
			return false;
		}

		public bool End()
		{
			return EOF (0);
		}

		public string Current
		{
			get{
				if (End ())
					return null;
				return Characters [Index];
			}
		}

		public int Location
		{
			get{
				return Index;
			}
		}

		public bool Merge(int begin, int end)
		{
			bool merged = false;
			if (begin < end && end < Characters.Count) {
				int color = TokenIndices [begin];
				for(int j = begin; j < end; j++)
				{
					TokenIndices[j] = color;
				}
				merged = true;
			}
			return merged;
		}

		public void Consume()
		{
		//Console.WriteLine ("Consuming @ index " + Index + " RBList Count " + RollbackList.Count);
			RollbackIndex.Push (Index);
			RollbackList.Push (TokenIndices);

			ConsumeCount++;
			Index++;
		//Console.WriteLine ("New index " + Index + " RBList Count " + RollbackList.Count);
		}

		public void Rollback()
		{
		//Console.WriteLine ("Rolling back: " + RollbackList.Count);
			for (int i = 0; i < ConsumeCount; i++) {
				TokenIndices = RollbackList.Pop ();
				Index = RollbackIndex.Pop ();
			}
			ConsumeCount = 0;
		}

		public void Commit ()
		{
			RollbackList.Clear ();
			RollbackIndex.Clear ();
			Merge (Index - ConsumeCount, Index);
			ConsumeCount = 0;
		}

		public void PrintCharacters()
		{
			Console.WriteLine (string.Join(",", Characters));
		}

		public void PrintTokenIndices()
		{
			Console.WriteLine (string.Join(",", TokenIndices));
		}
	}

