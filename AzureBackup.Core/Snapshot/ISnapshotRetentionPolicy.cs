using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.File;

namespace AzureBackup.Core.Snapshot
{
	public interface ISnapshotRetentionPolicy
	{
		string Description { get; }

		IEnumerable<CloudFileShare> GetSnapshotsToDelete(IEnumerable<CloudFileShare> snapshots);
	}
}