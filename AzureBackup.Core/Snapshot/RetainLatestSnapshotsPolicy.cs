using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.File;

namespace AzureBackup.Core.Snapshot
{
	public class RetainLatestSnapshotsPolicy : ISnapshotRetentionPolicy
	{
		public int NumberOfSnapshotsToRetain { get; }

		public string Description => $"Retain latest {NumberOfSnapshotsToRetain} snapshots";

		public RetainLatestSnapshotsPolicy(int numberOfSnapshotsToRetain)
		{
			if (numberOfSnapshotsToRetain < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(numberOfSnapshotsToRetain), "Number of snapshots to retain must be at least 0");
			}

			this.NumberOfSnapshotsToRetain = numberOfSnapshotsToRetain;
		}

		public IEnumerable<CloudFileShare> GetSnapshotsToDelete(IEnumerable<CloudFileShare> snapshots)
		{
			return snapshots
				.Where(x => x.IsSnapshot)
				.OrderByDescending(x => x.SnapshotTime)
				.Skip(NumberOfSnapshotsToRetain);
		}
	}
}