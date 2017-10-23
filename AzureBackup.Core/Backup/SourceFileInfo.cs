using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;

namespace AzureBackup.Core.Backup
{
	public class SourceFileInfo
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		public string Name { get; }
		public long Length { get; }
		public DateTime? LastModified { get; }
		public IReadOnlyList<string> ParentDirectories { get; }

		private readonly Func<Stream, CancellationToken, Task> copyToStreamAsync;

		public SourceFileInfo(
			string name,
			long length,
			IReadOnlyList<string> parentDirectories,
			Func<Stream, CancellationToken, Task> copyToStreamAsync,
			DateTime? lastModified = null)
		{
			this.Name = name;
			this.Length = length;
			this.LastModified = lastModified;
			this.ParentDirectories = parentDirectories ?? new List<string>();
			this.copyToStreamAsync = copyToStreamAsync;
		}

		public async Task CopyToStreamAsync(Stream target, CancellationToken cancellationToken = default(CancellationToken))
		{
			//Log.Trace(() => $"Copying {Path.Combine(Path.Combine(this.ParentDirectories.ToArray()), this.Name)} to stream");

			if (this.copyToStreamAsync != null)
			{
				await this.copyToStreamAsync(target, cancellationToken);
			}
		}

		public string GetNameWithPath(string separator)
		{
			var pathBuilder = new StringBuilder();

			foreach (var dir in this.ParentDirectories)
			{
				pathBuilder.Append(dir);
				pathBuilder.Append(separator);
			}

			pathBuilder.Append(this.Name);

			return pathBuilder.ToString();
		}
	}
}