using System;
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

			var inputFiles = this.sourceFileInfoProvider.GetInputFiles();

			await this.outputWriter.WriteOutputAsync(inputFiles, cancellationToken);

			Log.Info(() => "Finished backup");

			//if (outputWriter is IDisposable disposable)
			//{
			//	disposable.Dispose();
			//}
		}
	}
}