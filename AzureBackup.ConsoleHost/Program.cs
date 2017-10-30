using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.ConsoleHost.Configuration;
using AzureBackup.Core.Backup;
using AzureBackup.Core.Backup.BackupProviders;
using AzureBackup.Core.Backup.FileInfoProviders;
using AzureBackup.Core.Backup.OutputWriters;
using AzureBackup.Core.Snapshot;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using NLog;

namespace AzureBackup.ConsoleHost
{
	public class Program
	{
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

		public static async Task<int> Main(string[] args)
		{
			ServiceHostDefaults.Apply();

			var cts = new CancellationTokenSource();

			Console.CancelKeyPress += (sender, eventArgs) =>
			{
				Log.Info(() => "Ctrl-C pressed, aborting...");
				eventArgs.Cancel = true;
				cts.Cancel();
			};

			var retval = 1;

			try
			{
				Log.Info(() => "AzureBackup.ConsoleHost starting");

				var config = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("config.json", true)
					.AddCommandLine(args)
					.Build();

				switch (config["mode"].ToLowerInvariant())
				{
					case "snapshot":

						var snapshotOptions = new SnapshotOptions();
						config.Bind("snapshot", snapshotOptions);

						retval = await SnapshotAsync(snapshotOptions, cts.Token);
						break;

					case "backuplocal":

						var backupLocalOptions = new BackupLocalArchiveOptions();
						config.Bind("backuplocal", backupLocalOptions);

						retval = await BackupLocalArchiveAsync(backupLocalOptions, cts.Token);
						break;

					case "backupblob":

						var backupBlobOptions = new BackupBlobStorageArchiveOptions();
						config.Bind("backupblob", backupBlobOptions);

						retval = await BackupBlobStorageArchiveAsync(backupBlobOptions, cts.Token);
						break;

					case "servercopy":

						var serverCopyOptions = new ServerCopyOptions();
						config.Bind("servercopy", serverCopyOptions);

						retval = await ServerCopyAsync(serverCopyOptions, cts.Token);
						break;

					default:
						Log.Fatal(() => $"Unknown mode '{config["mode"]}'");
						retval = 1;
						break;
				}
			}
			catch (TaskCanceledException)
			{
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, () => "Unhandled exception");
			}

			Log.Info(() => "AzureBackup.ConsoleHost finished");

			return retval;
		}

		private static CloudStorageAccount GetCloudStorageAccount(string connectionString, string sasToken, string storageAccountName)
		{
			if (!string.IsNullOrWhiteSpace(connectionString))
			{
				return CloudStorageAccount.Parse(connectionString);
			}

			if (!string.IsNullOrWhiteSpace(storageAccountName) && !string.IsNullOrWhiteSpace(sasToken))
			{
				return new CloudStorageAccount(new StorageCredentials(sasToken), storageAccountName, "core.windows.net", true);
			}

			throw new ApplicationException("No ConnectionString, or StorageAccount and SASToken specified");
		}

		private static async Task<int> SnapshotAsync(SnapshotOptions options, CancellationToken cancellationToken)
		{
			var cloudStorageAccount = GetCloudStorageAccount(options.ConnectionString, options.SASToken, options.StorageAccountName);

			var cloudFileClient = cloudStorageAccount.CreateCloudFileClient();

			var azureShareSnapshotManager = new AzureShareSnapshotManager(cloudFileClient, "AzureShareBackupSnapshotTime");

			await azureShareSnapshotManager.CreateManagedSnapshotAsync(options.ShareName, cancellationToken);

			if (options.RetainSnapshots.HasValue)
			{
				await azureShareSnapshotManager.PruneManagedSnapshotsAsync(options.ShareName, new RetainLatestSnapshotsPolicy(options.RetainSnapshots.Value), cancellationToken);
			}

			return 0;
		}

		private static async Task<int> BackupLocalArchiveAsync(BackupLocalArchiveOptions options, CancellationToken cancellationToken)
		{
			var sourceCloudStorageAccount = GetCloudStorageAccount(options.SourceConnectionString, options.SourceSASToken, options.SourceStorageAccountName);
			var sourceCloudFileClient = sourceCloudStorageAccount.CreateCloudFileClient();

			var azureShareSnapshotManager = new AzureShareSnapshotManager(sourceCloudFileClient, "AzureShareBackupZipSnapshot");

			var snapshotShare = await azureShareSnapshotManager.CreateManagedSnapshotAsync(options.SourceShareName, cancellationToken);

			try
			{
				var zipName = Path.Combine(options.TargetPath, $"{options.TargetFileNamePrefix}-{snapshotShare.SnapshotTime:yyyyMMdd-HHmmss}.{GetFileExtension(options.ArchiveType)}");

				Log.Info(() => $"Backup to local zip: {zipName}");

				var backupProvider = new PreBufferingLocalProcessBackupProvider(
					new AzureFileShareFileInfoProvider(sourceCloudFileClient, snapshotShare.Name, snapshotShare.SnapshotTime),
					GetOutputWriter(options.ArchiveType, new FileStream(zipName, FileMode.CreateNew)));

				await backupProvider.RunAsync(cancellationToken);
			}
			finally
			{
				// ReSharper disable once PossibleInvalidOperationException

				// we want this to happen regardless of the rest being cancelled
				await azureShareSnapshotManager.DeleteManagedSnapshotAsync(snapshotShare.Name, snapshotShare.SnapshotTime.Value, CancellationToken.None);
			}

			return 0;
		}

		private static async Task<int> BackupBlobStorageArchiveAsync(BackupBlobStorageArchiveOptions options, CancellationToken cancellationToken)
		{
			var sourceCloudStorageAccount = GetCloudStorageAccount(options.SourceConnectionString, options.SourceSASToken, options.SourceStorageAccountName);
			var sourceCloudFileClient = sourceCloudStorageAccount.CreateCloudFileClient();

			var azureShareSnapshotManager = new AzureShareSnapshotManager(sourceCloudFileClient, "AzureShareBackupZipSnapshot");

			var snapshotShare = await azureShareSnapshotManager.CreateManagedSnapshotAsync(options.SourceShareName, cancellationToken);

			try
			{
				var targetCloudStorageAccount = GetCloudStorageAccount(options.TargetConnectionString, options.TargetSASToken, options.TargetStorageAccountName);
				var targetCloudBlobClient = targetCloudStorageAccount.CreateCloudBlobClient();

				var blobName = $"{options.TargetBlobNamePrefix}-{snapshotShare.SnapshotTime:yyyyMMdd-HHmmss}.{GetFileExtension(options.ArchiveType)}";
				var blobContainer = targetCloudBlobClient.GetContainerReference(options.TargetBlobContainerName);

				Log.Info(() => $"Backup to blob zip: {blobContainer.Uri}/{blobName}");

				var backupProvider = new PreBufferingLocalProcessBackupProvider(
					new AzureFileShareFileInfoProvider(sourceCloudFileClient, snapshotShare.Name, snapshotShare.SnapshotTime),
					GetOutputWriter(options.ArchiveType, await AzureStreamHelpers.GetBlobOutputStreamAsync(blobContainer, blobName, false, cancellationToken)));

				await backupProvider.RunAsync(cancellationToken);
			}
			finally
			{
				// ReSharper disable once PossibleInvalidOperationException

				// we want this to happen regardless of the rest being cancelled
				await azureShareSnapshotManager.DeleteManagedSnapshotAsync(snapshotShare.Name, snapshotShare.SnapshotTime.Value, CancellationToken.None);
			}

			return 0;
		}

		private static async Task<int> ServerCopyAsync(ServerCopyOptions options, CancellationToken cancellationToken = default)
		{
			var sourceCloudStorageAccount = GetCloudStorageAccount(options.SourceConnectionString, options.SourceSASToken, options.SourceStorageAccountName);
			var sourceCloudFileClient = sourceCloudStorageAccount.CreateCloudFileClient();

			var azureShareSnapshotManager = new AzureShareSnapshotManager(sourceCloudFileClient, "AzureShareBackupServerCopySnapshot");

			var snapshotShare = await azureShareSnapshotManager.CreateManagedSnapshotAsync(options.SourceShareName, cancellationToken);

			try
			{
				var targetCloudStorageAccount = GetCloudStorageAccount(options.TargetConnectionString, options.TargetSASToken, options.TargetStorageAccountName);
				var targetCloudFileClient = targetCloudStorageAccount.CreateCloudFileClient();

				var targetFileShare = targetCloudFileClient.GetShareReference(options.TargetShareName);

				var targetDirectoryName = $"{snapshotShare.SnapshotTime:yyyyMMdd-HHmmss}";
				var targetDirectory = targetFileShare.GetRootDirectoryReference().GetDirectoryReference(targetDirectoryName);

				await targetDirectory.CreateAsync(cancellationToken);

				var backupProvider = new AzureServerSideCopyBackupProvider(snapshotShare.GetRootDirectoryReference(), targetDirectory);

				await backupProvider.RunAsync(cancellationToken);
			}
			finally
			{
				// ReSharper disable once PossibleInvalidOperationException

				// we want this to happen regardless of the rest being cancelled
				await azureShareSnapshotManager.DeleteManagedSnapshotAsync(snapshotShare.Name, snapshotShare.SnapshotTime.Value, CancellationToken.None);
			}

			return 0;
		}

		private static IOutputWriter GetOutputWriter(ArchiveType archiveType, Stream target)
		{
			switch (archiveType)
			{
				case ArchiveType.Zip:
					return new ZipStreamOutputWriter(target);
				case ArchiveType.Tar:
					return new TarStreamOutputWriter(target);
				case ArchiveType.TarGZip:
					return new TarStreamOutputWriter(target, TarStreamOutputWriter.Compression.Gzip);
				case ArchiveType.TarBZip2:
					return new TarStreamOutputWriter(target, TarStreamOutputWriter.Compression.Bzip2);
				default:
					throw new ArgumentException($"Unknown archive type '{archiveType}'", nameof(archiveType));
			}
		}

		private static string GetFileExtension(ArchiveType archiveType)
		{
			switch (archiveType)
			{
				case ArchiveType.Zip:
					return "zip";
				case ArchiveType.Tar:
					return "tar";
				case ArchiveType.TarGZip:
					return "tar.gz";
				case ArchiveType.TarBZip2:
					return "tar.bz2";
				default:
					throw new ArgumentException($"Unknown archive type '{archiveType}'", nameof(archiveType));
			}
		}
	}
}
