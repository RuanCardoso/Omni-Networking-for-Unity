using MemoryPack;
#if OMNI_RELEASE // Bandwith Optimization
using Newtonsoft.Json;
#endif

namespace Omni.Core
{
    /// <summary>
    /// Enum representing various HTTP response status codes.
    /// </summary>
    public enum ResponseStatusCode
    {
        /// <summary>
        /// The request was successful.
        /// </summary>
        Success = 200,

        /// <summary>
        /// The request was successful and a new resource was created.
        /// </summary>
        Created = 201,

        /// <summary>
        /// The server successfully processed the request, but there is no content to send.
        /// </summary>
        NoContent = 204,

        /// <summary>
        /// The requested resource could not be found.
        /// </summary>
        NotFound = 404,

        /// <summary>
        /// The request requires user authentication.
        /// </summary>
        Unauthorized = 401,

        /// <summary>
        /// The server understood the request but refuses to authorize it.
        /// </summary>
        Forbidden = 403,

        /// <summary>
        /// An internal server error occurred.
        /// </summary>
        Error = 500,
    }

    /// <summary>
    /// Represents a generic API response with a status code and message.
    /// </summary>
    [MemoryPackable]
    public partial class ApiResponse
    {
        /// <summary>
        /// Gets or sets the status code of the API response.
        /// </summary>
#if OMNI_RELEASE
        [JsonProperty("c")]
#endif
        public ResponseStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the status message of the API response.
        /// </summary>
#if OMNI_RELEASE
        [JsonProperty("m")]
#endif
        public string StatusMessage { get; set; }

        public ApiResponse() { }
    }

    /// <summary>
    /// Represents a generic API response that includes a result of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result included in the response.</typeparam>
    [MemoryPackable]
    public partial class ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// Gets or sets the result of the API response.
        /// </summary>
#if OMNI_RELEASE
        [JsonProperty("t")]
#endif
        public T Result { get; set; }

        public ApiResponse() { }
    }
}
