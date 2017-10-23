using System.Collections.Generic;

namespace AzureBackup.Core.Backup
{
	public class SourceDirectoryInfo : SourceFileInfo
	{
		public SourceDirectoryInfo(IReadOnlyList<string> directoryHierarchy)
			: base(null, 0, directoryHierarchy, null, null)
		{
		}
	}
}