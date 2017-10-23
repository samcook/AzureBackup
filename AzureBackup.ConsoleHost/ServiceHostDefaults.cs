using System.Net;
using System.Threading;

namespace AzureBackup.ConsoleHost
{
	internal static class ServiceHostDefaults
	{
		/// <summary>
		/// Set up Service Host defaults
		/// </summary>
		public static void Apply()
		{
			// Decrease latency by disabling Nagle's algorithm https://docs.microsoft.com/en-us/azure/storage/storage-performance-checklist#subheading26
			ServicePointManager.UseNagleAlgorithm = false;

			// Decrease latency by disabling Expect-100 behaviour
			ServicePointManager.Expect100Continue = false;

			// Increase throughput by increasing connection limit https://docs.microsoft.com/en-us/azure/storage/storage-performance-checklist#subheading9
			ServicePointManager.DefaultConnectionLimit = 100;

			// Increase min thread count https://docs.microsoft.com/en-us/azure/storage/storage-performance-checklist#subheading10
			ThreadPool.SetMinThreads(100, 100);
		}
	}
}