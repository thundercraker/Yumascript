/*using System;
using System.Collections.Generic;
using YumascriptPackage;
using Parser = YSRDParser;
using DataType = YSRDParser.DataType;

namespace YumascriptPackage
{
	public class YSVirtualMachine
	{
		private List<string> storage;
		int Word_Size;
		int STORAGE_INDEX;

		public YSVirtualMachine (long Storage_Size, int word_size)
		{
			storage = new List<string> (Storage_Size);
			this.Word_Size = word_size;

			STORAGE_INDEX = 0;
		}

		public void AddPrimitive(Parser.PrimitiveType data)
		{
			switch (data.type) {
			case DataType.Number:
				break;
			case DataType.Text:
				break;
			case DataType.Boolean:
				break;
			default:
				break;
			}
		}

		public void AddNumber(){}
	}
}
*/
