using System;

namespace Golden.Data.ObjectDatabase.Resources
{
	internal class ResourceManager : Golden.EmbeddedResourceManager
	{
		public static readonly ResourceManager Default = new ResourceManager();

		private ResourceManager() : base(typeof(ResourceManager), typeof(ResourceManager).Namespace)
		{
		}
		public string GetDbScript(string sqlScriptFileName)
		{
			return this.GetFileAsText(string.Concat("DbScripts.", sqlScriptFileName, ".sql"));
		}
	}
}
