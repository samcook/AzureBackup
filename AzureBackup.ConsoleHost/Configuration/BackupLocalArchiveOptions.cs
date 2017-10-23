// ReSharper disable InconsistentNaming
namespace AzureBackup.ConsoleHost.Configuration
{
	public class BackupLocalArchiveOptions
	{
		public string SourceConnectionString { get; set; }
		public string SourceStorageAccountName { get; set; }
		public string SourceSASToken { get; set; }
		public string SourceShareName { get; set; }

		public string TargetPath { get; set; }
		public string TargetFileNamePrefix { get; set; }

		public ArchiveType ArchiveType { get; set; }
	}
}