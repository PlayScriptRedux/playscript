using System;

namespace PlayScript 
{

	public abstract class EmbedLoader 
	{
		public string source;
		public string mimeType;
		public string embedAsCFF;
		public string fontFamily;
		public string symbol;

		public EmbedLoader(string source, string mimeType, string embedAsCFF, string fontFamily, string symbol) 
		{
			this.source = source;
			this.mimeType = mimeType;
			this.embedAsCFF = embedAsCFF;
			this.fontFamily = fontFamily;
			this.symbol = symbol;
		}

		public object Load () {
			return Player.LoadResource(source, mimeType);
		}

	}

}