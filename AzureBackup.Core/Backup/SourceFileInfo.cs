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

		private readonly Func<CancellationToken, Task<Stream>> getStreamAsync;

		public SourceFileInfo(
			string name,
			long length,
			IReadOnlyList<string> parentDirectories,
			Func<CancellationToken, Task<Stream>> getStreamAsync,
			DateTime? lastModified = null)
		{
			this.Name = name;
			this.Length = length;
			this.LastModified = lastModified;
			this.ParentDirectories = parentDirectories ?? new List<string>();
			this.getStreamAsync = getStreamAsync;
		}

		public async Task<Stream> GetStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (this.getStreamAsync != null)
			{
				return await this.getStreamAsync(cancellationToken);
			}

			return null;
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