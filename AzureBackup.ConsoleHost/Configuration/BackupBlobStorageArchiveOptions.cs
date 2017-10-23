// ReSharper disable InconsistentNaming
namespace AzureBackup.ConsoleHost.Configuration
{
	public class BackupBlobStorageArchiveOptions
	{
		public string SourceConnectionString { get; set; }
		public string SourceStorageAccountName { get; set; }
		public string SourceSASToken { get; set; }
		public string SourceShareName { get; set; }

		public string TargetConnectionString { get; set; }
		public string TargetStorageAccountName { get; set; }
		public string TargetSASToken { get; set; }
		public string TargetBlobContainerName { get; set; }
		public string TargetBlobNamePrefix { get; set; }

		public ArchiveType ArchiveType { get; set; }
	}
}