using MemoryPack;
#if OMNI_RELEASE // Bandwith Optimization
using Newtonsoft.Json;
#endif

namespace Omni.Core
{
    /// <summary>
    /// Represents an response to an GET/POST request.
    /// </summary>
    [MemoryPackable]
    public partial class Response
    {
        /// <summary>
        /// Gets or sets the status code of the response.
        /// </summary>
#if OMNI_RELEASE
        [JsonProperty("c")]
#endif
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the status message of the response.
        /// </summary>
#if OMNI_RELEASE
        [JsonProperty("m")]
#endif
        public string StatusMessage { get; set; }
    }
}