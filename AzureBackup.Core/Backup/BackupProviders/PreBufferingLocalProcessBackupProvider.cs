using System.Collections.Async;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;

namespace AzureBackup.Core.Backup.BackupProviders
{
	public class PreBufferingLocalProcessBackupProvider : IBackupProvider
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		private readonly ISourceFileInfoProvider sourceFileInfoProvider;
		private readonly IOutputWriter outputWriter;

		private readonly BlockingCollection<SourceFileInfo> readyToProcess;

		public PreBufferingLocalProcessBackupProvider(ISourceFileInfoProvider sourceFileInfoProvider, IOutputWriter outputWriter, int prebufferQueueSize = 8)
		{
			this.sourceFileInfoProvider = sourceFileInfoProvider;
			this.outputWriter = outputWriter;

			this.readyToProcess = new BlockingCollection<SourceFileInfo>(prebufferQueueSize);
		}

		public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			var producer = Task.Run(async () => await ReadInputSequentialAsync(cancellationToken), cancellationToken);
			//var producer = Task.Run(async () => await ReadInputParallelAsync(cancellationToken), cancellationToken);

			while (!readyToProcess.IsCompleted)
			{
				var fileInfo = readyToProcess.Take(cancellationToken);

				await this.outputWriter.AddFileToArchiveAsync(fileInfo, cancellationToken);

				(await fileInfo.GetStreamAsync(cancellationToken))?.Dispose();
			}

			this.outputWriter.CloseArchive();

			await producer;
		}

		private async Task ReadInputSequentialAsync(CancellationToken cancellationToken)
		{
			var inputFiles = this.sourceFileInfoProvider.GetInputFiles();

			foreach (var fileInfo in inputFiles)
			{
				await AddMemoryStreamSourceFileInfoToReadyQueue(fileInfo, cancellationToken);
			}

			this.readyToProcess.CompleteAdding();
		}

		//private async Task ReadInputParallelAsync(CancellationToken cancellationToken)
		//{
		//	var inputFiles = this.sourceFileInfoProvider.GetInputFiles();

		//	await inputFiles.ParallelForEachAsync(async info =>
		//		{
		//			await AddMemoryStreamSourceFileInfoToReadyQueue(info, cancellationToken);
		//		},
		//		cancellationToken);

		//	this.readyToProcess.CompleteAdding();
		//}

		private async Task AddMemoryStreamSourceFileInfoToReadyQueue(SourceFileInfo fileInfo, CancellationToken cancellationToken)
		{
			var localFileInfo = fileInfo;
			Log.Debug(() => $"Reading input for {fileInfo.Name}");

			using (var sourceStream = await fileInfo.GetStreamAsync(cancellationToken))
			{
				if (sourceStream != null)
				{
					var tempStream = new MemoryStream();

					await sourceStream.CopyToAsync(tempStream, 81920, cancellationToken);

					tempStream.Seek(0, SeekOrigin.Begin);

					localFileInfo = new SourceFileInfo(
						fileInfo.Name,
						fileInfo.Length,
						fileInfo.ParentDirectories,
						token => Task.FromResult<Stream>(tempStream),
						fileInfo.LastModified);
				}
			}

			Log.Debug(() => $"Finished reading input for {fileInfo.Name}");

			this.readyToProcess.Add(localFileInfo, cancellationToken);
		}
	}
}