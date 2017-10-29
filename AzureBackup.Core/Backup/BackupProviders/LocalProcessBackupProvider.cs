using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;

namespace AzureBackup.Core.Backup.BackupProviders
{
	public class LocalProcessBackupProvider : IBackupProvider
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		private readonly ISourceFileInfoProvider sourceFileInfoProvider;
		private readonly IOutputWriter outputWriter;

		public LocalProcessBackupProvider(ISourceFileInfoProvider sourceFileInfoProvider, IOutputWriter outputWriter)
		{
			this.sourceFileInfoProvider = sourceFileInfoProvider;
			this.outputWriter = outputWriter;
		}

		public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Info(() => "Starting backup");

			//await SequentialFetchAsync(cancellationToken);
			await ParallelFetchAsync(cancellationToken);

			Log.Info(() => "Finished backup");

			//if (outputWriter is IDisposable disposable)
			//{
			//	disposable.Dispose();
			//}
		}

		private async Task SequentialFetchAsync(CancellationToken cancellationToken)
		{
			var inputFiles = this.sourceFileInfoProvider.GetInputFiles();

			foreach (var fileInfo in inputFiles)
			{
				await this.outputWriter.AddFileToArchiveAsync(fileInfo, cancellationToken);
			}

			this.outputWriter.CloseArchive();
		}

		private async Task ParallelFetchAsync(CancellationToken cancellationToken)
		{
			var inputFiles = this.sourceFileInfoProvider.GetInputFiles();

			var inputFileStreams = inputFiles
				.AsParallel()
				.AsOrdered()
				//.WithDegreeOfParallelism(4)
				.WithMergeOptions(ParallelMergeOptions.NotBuffered)
				.Select(async x => await ReadInputToMemoryStreamAsync(x, cancellationToken))
				.Select(x => x.Result);

			foreach (var item in inputFileStreams)
			{
				await this.outputWriter.AddFileToArchiveAsync(item, cancellationToken);

				(await item.GetStreamAsync(cancellationToken))?.Dispose();
			}

			this.outputWriter.CloseArchive();
		}

		private static async Task<SourceFileInfo> ReadInputToMemoryStreamAsync(SourceFileInfo fileInfo, CancellationToken cancellationToken)
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

			return localFileInfo;
		}
	}
}