using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;

namespace AzureBackup.Core.Backup.OutputWriters
{
	/// <summary>
	/// A class that ignores explicit calls to Flush.
	/// Used to prevent writes to archives in Azure Blob Storage ending up containing many small blocks.
	/// </summary>
	/// <inheritdoc />
	internal class NonFlushingStream : Stream
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		private readonly Stream innerStream;

		public NonFlushingStream(Stream innerStream)
		{
			this.innerStream = innerStream;
		}

		public override void Close()
		{
			Log.Trace(() => "Flushing on close");
			innerStream.Flush();

			innerStream.Close();
		}

		/// <summary>
		/// Warning: this is a no-op for NonFlushingStream
		/// </summary>
		public override void Flush()
		{
			Log.Trace(() => "Flush ignored");
		}

		/// <summary>
		/// Warning: this is a no-op for NonFlushingStream
		/// </summary>
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			Log.Trace(() => "FlushAsync ignored");

			return Task.CompletedTask;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return innerStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			innerStream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return innerStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			//Log.Trace(() => $"Write {count} bytes");
			innerStream.Write(buffer, offset, count);
		}

		public override bool CanRead => innerStream.CanRead;

		public override bool CanSeek => innerStream.CanSeek;

		public override bool CanWrite => innerStream.CanWrite;

		public override long Length => innerStream.Length;

		public override long Position
		{
			get => innerStream.Position;
			set => innerStream.Position = value;
		}
	}
}