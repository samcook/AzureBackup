// ReSharper disable InconsistentNaming
namespace AzureBackup.ConsoleHost.Configuration
{
	public class SnapshotOptions
	{
		public string ConnectionString { get; set; }
		public string StorageAccountName { get; set; }
		public string SASToken { get; set; }
		public string ShareName { get; set; }
		public int? RetainSnapshots { get; set; }
		public string MetadataKey { get; set; }
	}
}