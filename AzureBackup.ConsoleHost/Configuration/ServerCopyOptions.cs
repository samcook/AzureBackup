// ReSharper disable InconsistentNaming
namespace AzureBackup.ConsoleHost.Configuration
{
	public class ServerCopyOptions
	{
		public string SourceConnectionString { get; set; }
		public string SourceStorageAccountName { get; set; }
		public string SourceSASToken { get; set; }
		public string SourceShareName { get; set; }

		public string TargetConnectionString { get; set; }
		public string TargetStorageAccountName { get; set; }
		public string TargetSASToken { get; set; }
		public string TargetShareName { get; set; }
	}
}