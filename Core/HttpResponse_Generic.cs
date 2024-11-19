using MemoryPack;
#if OMNI_RELEASE // Bandwith Optimization
using Newtonsoft.Json;
#endif

namespace Omni.Core
{
    /// <summary>
    /// Represents an HTTP response with a generic payload.
    /// </summary>
    /// <typeparam name="T">The type of the payload.</typeparam>
    [MemoryPackable]
    public sealed partial class HttpResponse<T> : HttpResponse
    {
        /// <summary>
        /// Gets or sets the payload of the response.
        /// </summary>
#if OMNI_RELEASE
        [JsonProperty("t")]
#endif
        public T Result { get; set; }
    }
}
