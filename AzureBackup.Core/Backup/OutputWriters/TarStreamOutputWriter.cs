using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace AzureBackup.Core.Backup.OutputWriters
{
	public class TarStreamOutputWriter : IOutputWriter, IDisposable
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		public enum Compression
		{
			None,
			Gzip,
			Bzip2
		}

		private readonly TarOutputStream tarOutputStream;

		public TarStreamOutputWriter(Stream outputStream, Compression compression = Compression.None)
		{
			switch (compression)
			{
				case Compression.Gzip:
					outputStream = new GZipOutputStream(outputStream);
					break;
				case Compression.Bzip2:
					outputStream = new BZip2OutputStream(outputStream);
					break;
			}

			this.tarOutputStream = new TarOutputStream(outputStream);
		}

		public async Task WriteOutputAsync(IEnumerable<SourceFileInfo> fileInfos, CancellationToken cancellationToken = default(CancellationToken))
		{
			foreach (var fileInfo in fileInfos)
			{
				var fileName = fileInfo.GetNameWithPath("/");

				var tarHeader = new TarHeader
				{
					Name = fileName,
					Size = fileInfo.Length,
					ModTime = fileInfo.LastModified ?? new DateTime(1970, 1, 1)
				};

				Log.Debug(() => $"Adding {fileName} ({fileInfo.Length} bytes) to archive");

				var tarEntry = new TarEntry(tarHeader);

				this.tarOutputStream.PutNextEntry(tarEntry);

				await fileInfo.CopyToStreamAsync(this.tarOutputStream, cancellationToken);

				this.tarOutputStream.CloseEntry();
			}

			this.tarOutputStream.Finish();

			this.tarOutputStream.Close();
		}

		public void Dispose()
		{
			this.tarOutputStream.Dispose();
		}
	}
}