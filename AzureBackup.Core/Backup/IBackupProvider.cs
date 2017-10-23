using System.Threading;
using System.Threading.Tasks;

namespace AzureBackup.Core.Backup
{
	public interface IBackupProvider
	{
		Task RunAsync(CancellationToken cancellationToken = default(CancellationToken));
	}
}