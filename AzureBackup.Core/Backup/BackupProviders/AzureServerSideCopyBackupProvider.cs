using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;
using Microsoft.WindowsAzure.Storage.DataMovement;
using Microsoft.WindowsAzure.Storage.File;

namespace AzureBackup.Core.Backup.BackupProviders
{
	public class AzureServerSideCopyBackupProvider : IBackupProvider
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		private readonly CloudFileDirectory sourceDirectory;
		private readonly CloudFileDirectory targetDirectory;

		public AzureServerSideCopyBackupProvider(CloudFileDirectory sourceDirectory, CloudFileDirectory targetDirectory)
		{
			this.sourceDirectory = sourceDirectory;
			this.targetDirectory = targetDirectory;
		}

		public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			var copyDirectoryOptions = new CopyDirectoryOptions
			{
				Recursive = true
			};

			var directoryTransferContext = new DirectoryTransferContext
			{
				//ProgressHandler = new Progress<TransferStatus>(progress => Log.Trace(() => $"Progress: transferred: {progress.NumberOfFilesTransferred}, failed: {progress.NumberOfFilesFailed}, skipped: {progress.NumberOfFilesSkipped}, bytes transferred: {progress.BytesTransferred}"))
			};

			directoryTransferContext.FileTransferred += (sender, args) => Log.Trace(() => $"Transferred {(args.Source as CloudFile)?.Name} -> {(args.Destination as CloudFile)?.Name}");
			directoryTransferContext.FileSkipped += (sender, args) => Log.Trace(() => $"Skipped {(args.Source as CloudFile)?.Name} -> {(args.Destination as CloudFile)?.Name}");
			directoryTransferContext.FileFailed += (sender, args) => Log.Error(() => $"Failed {(args.Source as CloudFile)?.Name} -> {(args.Destination as CloudFile)?.Name}");

			Log.Info(() => $"Starting server side copy from {this.sourceDirectory.Uri} to {this.targetDirectory.Uri}");

			var status = await TransferManager.CopyDirectoryAsync(sourceDirectory, targetDirectory, true, copyDirectoryOptions, directoryTransferContext, cancellationToken);

			Log.Info(() => $"Finished server side copy, transferred: {status.NumberOfFilesTransferred}, failed: {status.NumberOfFilesFailed}, skipped: {status.NumberOfFilesSkipped}, bytes transferred: {status.BytesTransferred}");
		}
	}
}