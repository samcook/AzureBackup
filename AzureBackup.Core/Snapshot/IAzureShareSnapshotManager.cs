using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.File;

namespace AzureBackup.Core.Snapshot
{
	public interface IAzureShareSnapshotManager
	{
		string ManagedSnapshotMetadataKey { get; }

		Task<CloudFileShare> CreateManagedSnapshotAsync(string shareName, CancellationToken cancellationToken = default(CancellationToken));
		Task<IEnumerable<CloudFileShare>> GetManagedSnapshotsAsync(string shareName, CancellationToken cancellationToken = default(CancellationToken));
		Task DeleteManagedSnapshotAsync(string shareName, DateTimeOffset snapshotTime, CancellationToken cancellationToken = default(CancellationToken));
		Task PruneManagedSnapshotsAsync(string shareName, ISnapshotRetentionPolicy retentionPolicy, CancellationToken cancellationToken = default(CancellationToken));
	}
}