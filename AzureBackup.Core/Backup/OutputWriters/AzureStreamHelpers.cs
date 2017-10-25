using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureBackup.Core.Backup.OutputWriters
{
	public static class AzureStreamHelpers
	{
		public static async Task<Stream> GetBlobOutputStreamAsync(CloudBlobContainer cloudBlobContainer, string blobName, bool allowOverwriteExisting = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			var blob = cloudBlobContainer.GetBlockBlobReference(blobName);
			var accessCondition = allowOverwriteExisting
				? AccessCondition.GenerateEmptyCondition()
				: AccessCondition.GenerateIfNotExistsCondition();

			return new NonFlushingStream(await blob.OpenWriteAsync(accessCondition, null, null, cancellationToken));
		}
	}
}