using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureBackup.Core.Logging;
using Microsoft.WindowsAzure.Storage.File;

namespace AzureBackup.Core.Snapshot
{
	public class AzureShareSnapshotManager : IAzureShareSnapshotManager
	{
		private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

		public string ManagedSnapshotMetadataKey { get; }

		private readonly CloudFileClient cloudFileClient;

		public AzureShareSnapshotManager(CloudFileClient cloudFileClient, string managedSnapshotMetadataKey)
		{
			if (string.IsNullOrWhiteSpace(managedSnapshotMetadataKey))
			{
				throw new ArgumentException("Managed Snapshot Metadata key must not be null or empty");
			}

			this.cloudFileClient = cloudFileClient;
			this.ManagedSnapshotMetadataKey = managedSnapshotMetadataKey;
		}

		public async Task<CloudFileShare> CreateManagedSnapshotAsync(string shareName, CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Info(() => $"Taking snapshot of '{shareName}'");

			var reference = this.cloudFileClient.GetShareReference(shareName);
			
			if (!await reference.ExistsAsync(cancellationToken))
			{
				throw new ApplicationException($"Share '{shareName}' does not exist");
			}

			var snapshot = await reference.SnapshotAsync(new Dictionary<string, string> {{this.ManagedSnapshotMetadataKey, DateTimeOffset.UtcNow.ToString()}}, null, null, null, cancellationToken);
			
			return snapshot;
		}

		public async Task<IEnumerable<CloudFileShare>> GetManagedSnapshotsAsync(string shareName, CancellationToken cancellationToken = default(CancellationToken))
		{
			var results = new List<CloudFileShare>();

			FileContinuationToken fileContinuationToken = null;

			do
			{
				var shareResultSegment = await this.cloudFileClient.ListSharesSegmentedAsync(shareName, ShareListingDetails.Snapshots | ShareListingDetails.Metadata, null, fileContinuationToken, null, null, cancellationToken);

				results.AddRange(shareResultSegment.Results.Where(x =>
					x.Name == shareName &&
					x.IsSnapshot &&
					x.Metadata.ContainsKey(this.ManagedSnapshotMetadataKey)));

				fileContinuationToken = shareResultSegment.ContinuationToken;
			} while (fileContinuationToken != null);

			return results;
		}

		public async Task DeleteManagedSnapshotAsync(string shareName, DateTimeOffset snapshotTime, CancellationToken cancellationToken = default(CancellationToken))
		{
			var share = this.cloudFileClient.GetShareReference(shareName, snapshotTime);

			if (!await share.ExistsAsync(cancellationToken))
			{
				throw new ApplicationException($"Snapshot '{shareName}' with timestamp '{snapshotTime}' does not exist");
			}

			await share.FetchAttributesAsync(cancellationToken);

			await this.DeleteManagedSnapshotAsync(share, cancellationToken);
		}

		private async Task DeleteManagedSnapshotAsync(CloudFileShare share, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!share.IsSnapshot)
			{
				throw new ApplicationException($"Share '{share.Name}' is not a snapshot");
			}

			if (!share.Metadata.ContainsKey(this.ManagedSnapshotMetadataKey))
			{
				throw new ApplicationException($"Snapshot '{share.Name}' with timestamp '{share.SnapshotTime}' is not managed by us (missing metadata with key '{this.ManagedSnapshotMetadataKey}')");
			}

			Log.Info(() => $"Deleting snapshot '{share.Name}' with timestamp '{share.SnapshotTime}'");

			await share.DeleteIfExistsAsync(cancellationToken);
		}

		public async Task PruneManagedSnapshotsAsync(string shareName, ISnapshotRetentionPolicy retentionPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Info(() => $"Pruning managed snapshots using policy '{retentionPolicy.Description}'");

			var allManagedSnapshots = await GetManagedSnapshotsAsync(shareName, cancellationToken);

			var snapshotsToDelete = retentionPolicy.GetSnapshotsToDelete(allManagedSnapshots);

			foreach (var snapshot in snapshotsToDelete)
			{
				await this.DeleteManagedSnapshotAsync(snapshot, cancellationToken);
			}
		}
	}
}