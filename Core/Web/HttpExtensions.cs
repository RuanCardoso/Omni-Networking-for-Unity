using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Omni.Core.Web.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace Omni.Core.Web
{
    /// <summary>
    /// Certificate handler that disables SSL certificate validation for internal HTTPS routes.
    /// </summary>
    /// <remarks>
    /// This handler is used to bypass SSL certificate validation when connecting to internal hosts, eg: service-to-service communication.
    /// such as localhost or 127.0.0.1 in production environments where the server has HTTPS configured.
    /// </remarks>
    internal class DisableSslValidationHandler : CertificateHandler
    {
        /// <summary>
        /// Always validates certificates as valid by returning true.
        /// </summary>
        /// <param name="certificateData">The certificate data to validate.</param>
        /// <returns>Always returns true, bypassing SSL validation.</returns>
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public static class HttpExtensions
    {
        /// <summary>
        /// Thread-safe dictionary that stores HTTP session data keyed by session ID.
        /// </summary>
        /// <remarks>
        /// This dictionary maintains session state across multiple HTTP requests, allowing for persistent user data storage.
        /// The outer dictionary uses session IDs as keys, while the inner dictionary stores session key-value pairs.
        /// </remarks>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _sessions = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _sessions.Clear();
        }

        /// <summary>
        /// Sends the specified text as a response to the HTTP request.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="plainText">The text to send as the response body.</param>
        /// <param name="statusCode">The HTTP status code to send. Defaults to 200 (OK).</param>
        /// <remarks>
        /// This extension method provides a convenient way to send text-based responses.
        /// Automatically sets content type to "text/plain" and encoding to UTF-8.
        /// </remarks>
        public static void SendAsText(this kHttpResponse response, string plainText, int statusCode = 200)
        {
            response.StatusCode = statusCode;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "text/plain";

            byte[] data = Encoding.UTF8.GetBytes(plainText);
            response.ContentLength = data.Length;
            response.Close(data);
        }

        /// <summary>
        /// Sends the specified object as a JSON-formatted response to the HTTP request.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="result">The object to serialize to JSON for the response body.</param>
        /// <param name="statusCode">The HTTP status code to send. Defaults to 200 (OK).</param>
        /// <remarks>
        /// This extension method provides a convenient way to send JSON-formatted responses.
        /// Automatically sets content type to "application/json" and encoding to UTF-8.
        /// Uses the NetworkManager's JSON serialization for consistent object handling.
        /// </remarks>
        public static void SendAsJson(this kHttpResponse response, object result, int statusCode = 200)
        {
            response.StatusCode = statusCode;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "application/json";

            string json = NetworkManager.ToJson(result);
            byte[] data = Encoding.UTF8.GetBytes(json);
            response.ContentLength = data.Length;
            response.Close(data);
        }

        /// <summary>
        /// Sends an HTTP 403 Forbidden response to the client, with an optional message.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="message">Optional message to include in the response body. Defaults to "Server rejected the request."</param>
        /// <remarks>
        /// This extension method provides a convenient way to reject an HTTP request with a 403 status code.
        /// The message is sent as plain text with UTF-8 encoding.
        /// </remarks>
        public static void Reject(this kHttpResponse response, string message = "Server rejected the request.")
        {
            SendAsText(response, message, 403);
        }

        /// <summary>
        /// Sends an HTTP 403 Forbidden response to the client, with an optional JSON-serializable object.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="result">JSON-serializable object to include in the response body.</param>
        /// <remarks>
        /// This extension method provides a convenient way to reject an HTTP request with a 403 status code.
        /// The object is automatically serialized to JSON using the NetworkManager's JSON serialization.
        /// </remarks>
        public static void Reject(this kHttpResponse response, object result)
        {
            SendAsJson(response, result, 403);
        }

        /// <summary>
        /// Parses the request body as <c>multipart/form-data</c>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>A <see cref="MultipartFormDataParser"/> containing the parsed form data.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the request method is not POST or the content type is not <c>multipart/form-data</c>.
        /// </exception>
        /// <remarks>
        /// This method reads the entire request body into memory.  
        /// For large uploads, consider using <see cref="ParseMultipartStreaming"/>.
        /// </remarks>
        public static MultipartFormDataParser ParseMultipart(this kHttpRequest request)
        {
            try
            {
                if (request.HttpMethod != "POST")
                    throw new NotSupportedException("The request method must be POST.");

                if (!request.ContentType.StartsWith("multipart/form-data"))
                {
                    throw new NotSupportedException("The request content type must be multipart/form-data.");
                }

                return MultipartFormDataParser.Parse(request.InputStream, request.ContentEncoding);
            }
            catch (Exception ex)
            {
                throw new Exception("ParseMultipartFormData -> " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Parses the request body as <c>multipart/form-data</c> in a streaming fashion.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>
        /// A <see cref="StreamingMultipartFormDataParser"/> for processing form data sequentially 
        /// without loading the entire request into memory.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the request method is not POST or the content type is not <c>multipart/form-data</c>.
        /// </exception>
        /// <remarks>
        /// Recommended for handling large file uploads or when memory usage is a concern.
        /// </remarks>
        public static StreamingMultipartFormDataParser ParseMultipartStreaming(this kHttpRequest request)
        {
            try
            {
                if (request.HttpMethod != "POST")
                    throw new NotSupportedException("The request method must be POST.");

                if (!request.ContentType.StartsWith("multipart/form-data"))
                {
                    throw new NotSupportedException("The request content type must be multipart/form-data.");
                }

                return new StreamingMultipartFormDataParser(request.InputStream, request.ContentEncoding);
            }
            catch (Exception ex)
            {
                throw new Exception("ParseMultipartFormDataStreaming -> " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Parses parameters from a GET query string or a POST form request (<c>application/x-www-form-urlencoded</c>).
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>A dictionary of key-value pairs representing the request parameters.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the request method/content type is not supported.
        /// </exception>
        /// <remarks>
        /// For GET requests, parses the query string parameters.
        /// For POST requests, only supports "application/x-www-form-urlencoded" content type.
        /// Use <see cref="ParseJsonToDictionary"/> for JSON requests or <see cref="ParseMultipart"/> for multipart form data.
        /// Parameter names are compared case-insensitively.
        /// </remarks>
        public static Dictionary<string, string> ParseForm(this kHttpRequest request)
        {
            try
            {
                if (request.HttpMethod == "GET")
                    return ParseQueryStringToDictionary(request.QueryString);

                if (request.HttpMethod == "POST")
                {
                    if (request.ContentType.StartsWith("application/x-www-form-urlencoded"))
                    {
                        using StreamReader parameters = new(request.InputStream, request.ContentEncoding);
                        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(parameters.ReadToEnd());
                        return ParseQueryStringToDictionary(queryString);
                    }
                    else if (request.ContentType.StartsWith("application/json"))
                        throw new NotSupportedException($"JSON requests are not supported. Use {nameof(ParseJsonToDictionary)} or {nameof(ParseJsonToObject)} instead.");
                    else if (request.ContentType.StartsWith("multipart/form-data"))
                        throw new NotSupportedException($"Multipart/form-data requests are not supported. Use {nameof(ParseMultipart)} instead.");
                    else
                        throw new NotSupportedException("Unsupported content type: " + request.ContentType);
                }

                throw new NotSupportedException("Error parsing request parameters, only GET and POST requests are supported.");
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(ParseForm)} -> " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Parses parameters from a JSON POST request (<c>application/json</c>) into a dictionary.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>A dictionary of key-value pairs representing the JSON request body.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the request method/content type is not supported.
        /// </exception>
        /// <remarks>
        /// Only supports POST requests with "application/json" content type.
        /// Use <see cref="ParseForm"/> for URL-encoded requests or <see cref="ParseMultipart"/> for multipart form data.
        /// Dictionary keys are compared case-insensitively for flexible access.
        /// </remarks>
        public static Dictionary<string, object> ParseJsonToDictionary(this kHttpRequest request)
        {
            try
            {
                if (request.HttpMethod != "POST")
                    throw new NotSupportedException("JSON parsing only supports POST requests.");

                if (request.ContentType.StartsWith("application/json"))
                {
                    using StreamReader parameters = new(request.InputStream, request.ContentEncoding);
                    var rawDictionary = NetworkManager.FromJson<Dictionary<string, object>>(parameters.ReadToEnd());
                    return new Dictionary<string, object>(rawDictionary, StringComparer.OrdinalIgnoreCase);
                }
                else if (request.ContentType.StartsWith("multipart/form-data"))
                    throw new NotSupportedException($"Multipart/form-data requests are not supported. Use {nameof(ParseMultipart)} instead.");
                else if (request.ContentType.StartsWith("application/x-www-form-urlencoded"))
                    throw new NotSupportedException($"URL encoded requests are not supported. Use {nameof(ParseForm)} instead.");
                else
                    throw new NotSupportedException("Unsupported content type: " + request.ContentType);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(ParseJsonToDictionary)} -> " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Parses the JSON request body (<c>application/json</c>) into a strongly typed object.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize the JSON into.</typeparam>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The deserialized object of type T.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the request method/content type is not supported.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown if the JSON cannot be deserialized to the specified type.
        /// </exception>
        /// <remarks>
        /// Only supports POST requests with "application/json" content type.
        /// Use <see cref="ParseForm"/> for URL-encoded requests or <see cref="ParseMultipart"/> for multipart form data.
        /// The target type T must be compatible with the JSON structure.
        /// </remarks>
        public static T ParseJsonToObject<T>(this kHttpRequest request)
        {
            try
            {
                if (request.HttpMethod != "POST")
                    throw new NotSupportedException("JSON parsing only supports POST requests.");

                if (request.ContentType.StartsWith("application/json"))
                {
                    using StreamReader parameters = new(request.InputStream, request.ContentEncoding);
                    return NetworkManager.FromJson<T>(parameters.ReadToEnd());
                }
                else if (request.ContentType.StartsWith("multipart/form-data"))
                    throw new NotSupportedException($"Multipart/form-data requests are not supported. Use {nameof(ParseMultipart)} instead.");
                else if (request.ContentType.StartsWith("application/x-www-form-urlencoded"))
                    throw new NotSupportedException($"URL encoded requests are not supported. Use {nameof(ParseForm)} instead.");
                else
                    throw new NotSupportedException("Unsupported content type: " + request.ContentType);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(ParseJsonToObject)} -> " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Initializes a new session by setting a secure cookie with a unique session ID.
        /// </summary>
        /// <param name="response">The HTTP response to which the session cookie will be added.</param>
        /// <param name="request">The HTTP request used to determine security settings.</param>
        /// <param name="expires">Optional expiration time for the session cookie. If null, creates a non-persistent session that expires when the browser closes.</param>
        /// <returns>A thread-safe dictionary for storing session data.</returns>
        /// <remarks>
        /// Creates a secure session cookie with the following settings:
        /// - HttpOnly: Prevents client-side JavaScript access
        /// - Secure: Only sent over HTTPS connections
        /// - SameSite=Strict: Prevents CSRF attacks
        /// - Path=/: Available across the entire domain
        /// The returned dictionary can be used to store session data that persists across requests.
        /// </remarks>
        public static ConcurrentDictionary<string, object> StartSession(this kHttpResponse response, kHttpRequest request, DateTime? expires = null)
        {
            if (expires == null)
                expires = DateTime.MinValue;

            string sessionId = Guid.NewGuid().ToString();
            Cookie sessionCookie = new("OMNI-SESSID", sessionId)
            {
                Path = "/",
                HttpOnly = true,
                Secure = request.IsSecureConnection,
                Expires = expires.Value,
                SameSite = "Strict"
            };

            response.SetCookie(sessionCookie);
            _sessions[sessionId] = new ConcurrentDictionary<string, object>();
            return _sessions[sessionId];
        }

        /// <summary>
        /// Retrieves the session data associated with the current HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request containing the session cookie.</param>
        /// <returns>A reference to the session data as a dictionary of key-value pairs.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the session cookie is missing, expired, or invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the session ID does not correspond to an active session.
        /// </exception>
        /// <remarks>
        /// Modifying the returned dictionary directly updates the session data.
        /// </remarks>
        public static ConcurrentDictionary<string, object> GetSession(this kHttpRequest request)
        {
            var cookie = request.Cookies["OMNI-SESSID"];
            if (cookie == null)
            {
                throw new InvalidOperationException(
                    "The session cookie 'OMNI-SESSID' is missing from the request. Ensure that a session has been started and the client is sending the cookie."
                );
            }

            if (cookie.Expired)
                throw new InvalidOperationException("The session cookie has expired. A new session must be started.");

            string sessionId = cookie.Value;
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                throw new KeyNotFoundException(
                    $"No active session was found for the session ID '{sessionId}'. Ensure that the session is valid and has not expired or been cleared."
                );
            }

            return session;
        }

        /// <summary>
        /// Attempts to retrieve the session data associated with the current HTTP request without throwing exceptions.
        /// </summary>
        /// <param name="request">The HTTP request containing the session cookie.</param>
        /// <param name="session">When this method returns, contains the session data if found; otherwise, null.</param>
        /// <returns><c>true</c> if the session data is successfully retrieved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This is the safe version of <see cref="GetSession"/> that doesn't throw exceptions.
        /// Returns false if the session cookie is missing, expired, or invalid.
        /// Use this method when you want to handle session absence gracefully.
        /// </remarks>
        public static bool TryGetSession(this kHttpRequest request, out ConcurrentDictionary<string, object> session)
        {
            session = null;
            var cookie = request.Cookies["OMNI-SESSID"];
            if (cookie == null)
                return false;

            if (cookie.Expired)
                return false;

            string sessionId = cookie.Value;
            if (!_sessions.TryGetValue(sessionId, out session))
                return false;

            return true;
        }

        /// <summary>
        /// Ends the session associated with the given HTTP request and response by clearing the session cookie and removing session data.
        /// </summary>
        /// <param name="response">The HTTP response where the session cookie will be updated to expire.</param>
        /// <param name="request">The HTTP request containing the session cookie.</param>
        /// <returns><c>true</c> if the session was successfully destroyed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Destroying a session ensures the associated data is no longer accessible, and the server will need to start a new session.
        /// </remarks>
        public static bool DestroySession(this kHttpResponse response, kHttpRequest request)
        {
            var cookie = request.Cookies["OMNI-SESSID"];
            if (cookie == null)
                return false;

            // Set the session cookie to expire immediately
            StartSession(response, request, DateTime.UnixEpoch);

            string sessionId = cookie.Value;
            return _sessions.TryRemove(sessionId, out _);
        }

        /// <summary>
        /// Converts a query string collection to a dictionary with case-insensitive string keys.
        /// </summary>
        /// <param name="queryString">The query string collection to convert.</param>
        /// <returns>A dictionary with case-insensitive keys and string values.</returns>
        /// <remarks>
        /// This is an internal helper method used by form parsing methods.
        /// Empty or null keys are ignored during conversion.
        /// </remarks>
        internal static Dictionary<string, string> ParseQueryStringToDictionary(NameValueCollection queryString)
        {
            string[] allKeys = queryString.AllKeys;
            Dictionary<string, string> parameters = new(allKeys.Length, StringComparer.OrdinalIgnoreCase);
            foreach (string key in allKeys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                parameters[key] = queryString[key];
            }

            return parameters;
        }

        internal static CookieCollection ToCookieCollection(this IEnumerable<SerializableCookie> cookies)
        {
            var collection = new CookieCollection();
            foreach (var sc in cookies)
                collection.Add(sc.ToCookie());

            return collection;
        }

        internal static NameValueCollection ToNameValueCollection(this IEnumerable<SerializableHeader> headers)
        {
            var collection = new NameValueCollection();
            foreach (var h in headers)
            {
                foreach (var v in h.Values)
                    collection.Add(h.Name, v);
            }

            return collection;
        }

        /// <summary>
        /// Configures the given <see cref="UnityWebRequest"/> to trust internal hosts
        /// such as <c>localhost</c> or <c>127.0.0.1</c>, bypassing SSL certificate validation
        /// to allow secure communication between services running on the same machine or network.
        /// </summary>
        /// <param name="request">
        /// The <see cref="UnityWebRequest"/> instance to configure.
        /// </param>
        /// <remarks>
        /// This method is useful for development and testing environments where self-signed certificates
        /// are commonly used for internal services. It should only be used with trusted internal hosts.
        /// Uses <see cref="DisableSslValidationHandler"/> to bypass SSL validation for internal hosts.
        /// </remarks>
        public static void TrustInternalHosts(this UnityWebRequest request)
        {
            var uri = new Uri(request.url);
            string host = uri.Host;

            // Allow internal hosts to bypass SSL validation
            if (NetworkHelper.IsInternalHost(host))
                request.certificateHandler = new DisableSslValidationHandler();
        }
    }
}
