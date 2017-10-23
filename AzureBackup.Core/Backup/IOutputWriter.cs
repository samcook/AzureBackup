using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzureBackup.Core.Backup
{
	public interface IOutputWriter
	{
		Task WriteOutputAsync(IEnumerable<SourceFileInfo> files, CancellationToken cancellationToken = default(CancellationToken));
	}
}