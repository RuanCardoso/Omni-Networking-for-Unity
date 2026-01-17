using MemoryPack;
#if OMNI_RELEASE // Bandwith Optimization
using Newtonsoft.Json;
#endif

namespace Omni.Core
{
    /// <summary>
    /// Represents an response with a generic payload.
    /// </summary>
    /// <typeparam name="T">The type of the payload.</typeparam>
    [MemoryPackable]
    public sealed partial class Response<T> : Response
    {
        /// <summary>
        /// Gets or sets the payload of the response.
        /// </summary>
        public T Result { get; set; }
    }
}