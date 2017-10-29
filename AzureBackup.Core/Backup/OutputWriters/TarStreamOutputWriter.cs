using System;
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
		private bool isClosed;

		public TarStreamOutputWriter(Stream outputStream, Compression compression = Compression.None)
		{
			switch (compression)
			{
				case Compression.None:
					break;
				case Compression.Gzip:
					outputStream = new GZipOutputStream(outputStream);
					break;
				case Compression.Bzip2:
					outputStream = new BZip2OutputStream(outputStream);
					break;
				default:
					throw new ArgumentException($"Unknown Compression type: {compression}");
			}

			this.tarOutputStream = new TarOutputStream(outputStream);
		}

		public async Task AddFileToArchiveAsync(SourceFileInfo fileInfo, CancellationToken cancellationToken)
		{
			if (isClosed)
			{
				throw new InvalidOperationException("Archive has been closed");
			}

			var fileName = fileInfo.GetNameWithPath("/");

			var tarHeader = new TarHeader
			{
				Name = fileName,
				Size = fileInfo.Length,
				ModTime = fileInfo.LastModified ?? DateTime.UtcNow
			};

			Log.Debug(() => $"Adding {fileName} ({fileInfo.Length} bytes) to archive");

			var tarEntry = new TarEntry(tarHeader);

			this.tarOutputStream.PutNextEntry(tarEntry);

			using (var inputStream = await fileInfo.GetStreamAsync(cancellationToken))
			{
				if (inputStream != null && inputStream.Length > 0)
				{
					await inputStream.CopyToAsync(tarOutputStream, 81920, cancellationToken);
				}
			}

			this.tarOutputStream.CloseEntry();
		}

		public void CloseArchive()
		{
			if (isClosed)
			{
				return;
			}

			this.isClosed = true;

			this.tarOutputStream.Finish();
			this.tarOutputStream.Close();
		}

		public void Dispose()
		{
			this.CloseArchive();

			this.tarOutputStream.Dispose();
		}
	}
}