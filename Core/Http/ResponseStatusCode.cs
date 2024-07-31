namespace Omni.Core
{
    /// <summary>
    /// Enum representing various HTTP response status codes.
    /// </summary>
    public sealed class StatusCode
    {
        /// <summary>
        /// The request was successful.
        /// </summary>
        public const int Success = 200;

        /// <summary>
        /// The request was successful and a new resource was created.
        /// </summary>
        public const int Created = 201;

        /// <summary>
        /// The server successfully processed the request, but there is no content to send.
        /// </summary>
        public const int NoContent = 204;

        /// <summary>
        /// The requested resource could not be found.
        /// </summary>
        public const int NotFound = 404;

        /// <summary>
        /// The request requires user authentication.
        /// </summary>
        public const int Unauthorized = 401;

        /// <summary>
        /// The server understood the request but refuses to authorize it.
        /// </summary>
        public const int Forbidden = 403;

        /// <summary>
        /// An internal server error occurred.
        /// </summary>
        public const int Error = 500;
    }
}
