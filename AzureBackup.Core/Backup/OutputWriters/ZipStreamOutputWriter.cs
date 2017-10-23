﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;
using ICSharpCode.SharpZipLib.Zip;

namespace AzureBackup.Core.Backup.OutputWriters
{
	public class ZipStreamOutputWriter : IOutputWriter, IDisposable
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		private readonly ZipOutputStream zipOutputStream;

		public ZipStreamOutputWriter(Stream outputStream)
		{
			this.zipOutputStream = new ZipOutputStream(outputStream);
		}

		public async Task WriteOutputAsync(IEnumerable<SourceFileInfo> fileInfos, CancellationToken cancellationToken)
		{
			foreach (var fileInfo in fileInfos)
			{
				var zipEntryName = fileInfo.GetNameWithPath("/");

				Log.Debug(() => $"Adding {zipEntryName} ({fileInfo.Length} bytes) to archive");

				var zipEntry = new ZipEntry(zipEntryName);

				this.zipOutputStream.PutNextEntry(zipEntry);

				await fileInfo.CopyToStreamAsync(this.zipOutputStream, cancellationToken);

				this.zipOutputStream.CloseEntry();
			}

			this.zipOutputStream.Finish();

			this.zipOutputStream.Close();
		}

		public void Dispose()
		{
			this.zipOutputStream.Dispose();
		}
	}
}