using Microsoft.IO;
using System.Buffers;

namespace Omni.Core.Web
{
	internal static class Utilities
	{
		internal static RecyclableMemoryStreamManager MemoryStreamManager { get; } = new RecyclableMemoryStreamManager();

		internal static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Shared;
	}
}
