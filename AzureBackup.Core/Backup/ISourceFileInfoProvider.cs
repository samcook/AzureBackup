using System.Collections.Generic;

namespace AzureBackup.Core.Backup
{
	public interface ISourceFileInfoProvider
	{
		IEnumerable<SourceFileInfo> GetInputFiles();
	}
}