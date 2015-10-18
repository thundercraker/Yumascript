using System;
using System.Collections.Generic;

	public class YSTokenStream<T> where T : class
	{
		public List<T> Items;
		protected int Index { get; set; }
		private Stack<int> TokenIndices { get; set; }

		public YSTokenStream (Func<List<T>> itemExtractor)
		{
			Index = 0;
			Items = itemExtractor ();
			TokenIndices = new Stack<int> ();
		}

		public int CurrentPosition()
		{
			return Index;
		}

		public virtual T Current
		{
			get {
				if (End()) {
					return null;
				}
				return Items [Index];
			}
		}

		public void Consume()
		{
			Index++;
		}

		private Boolean EOF(int lookahead)
		{
			if (Index + lookahead >= Items.Count) {
				return true;
			}
			return false;
		}

		public Boolean End()
		{
			return EOF (0);
		}

		public void Commit()
		{
			TokenIndices.Push (Index);
		}

		public void Rollback()
		{
			Index = TokenIndices.Pop ();
		}

		public void Final()
		{
			TokenIndices.Pop ();
		}
	}
