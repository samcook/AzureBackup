using System;
using System.Collections.Generic;
using System.Linq;
using AzureBackup.Core.Logging;
using Microsoft.WindowsAzure.Storage.File;

namespace AzureBackup.Core.Backup.FileInfoProviders
{
	public class AzureFileShareFileInfoProvider : ISourceFileInfoProvider
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		private readonly CloudFileClient cloudFileClient;
		private readonly string shareName;
		private readonly DateTimeOffset? snapshot;

		public AzureFileShareFileInfoProvider(CloudFileClient cloudFileClient, string shareName, DateTimeOffset? snapshot = null)
		{
			this.cloudFileClient = cloudFileClient;
			this.shareName = shareName;
			this.snapshot = snapshot;
		}

		public IEnumerable<SourceFileInfo> GetInputFiles()
		{
			var share = this.cloudFileClient.GetShareReference(shareName, snapshot);

			var rootDirectory = share.GetRootDirectoryReference();

			return ProcessDirectory(rootDirectory);
		}

		private static IEnumerable<SourceFileInfo> ProcessDirectory(CloudFileDirectory directory, IReadOnlyList<string> parentDirectories = null)
		{
			parentDirectories = parentDirectories ?? new List<string>();

			Log.Trace(() => $"Processing directory /{string.Join("/", parentDirectories)}");

			FileContinuationToken fileContinuationToken = null;

			do
			{
				var fileResultSegment = directory.ListFilesAndDirectoriesSegmented(fileContinuationToken);

				foreach (var item in fileResultSegment.Results)
				{
					switch (item)
					{
						case CloudFile file:
							file.FetchAttributes(); // LastModified won't be populated unless we do this

							yield return new SourceFileInfo(
								file.Name,
								file.Properties.Length,
								parentDirectories,
								async (stream, cancellationToken) => await file.DownloadToStreamAsync(stream, cancellationToken),
								file.Properties.LastModified?.DateTime);

							break;

						case CloudFileDirectory subDirectory:

							var subDirectoryAndParents = parentDirectories.ToList();
							subDirectoryAndParents.Add(subDirectory.Name);

							yield return new SourceDirectoryInfo(subDirectoryAndParents);

							foreach (var file in ProcessDirectory(subDirectory, subDirectoryAndParents))
							{
								yield return file;
							}

							break;
					}
				}

				fileContinuationToken = fileResultSegment.ContinuationToken;
			} while (fileContinuationToken != null);
		}
	}
}