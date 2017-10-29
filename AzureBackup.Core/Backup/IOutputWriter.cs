using System.Threading;
using System.Threading.Tasks;

namespace AzureBackup.Core.Backup
{
	public interface IOutputWriter
	{
		Task AddFileToArchiveAsync(SourceFileInfo fileInfo, CancellationToken cancellationToken = default(CancellationToken));
		void CloseArchive();
	}
}