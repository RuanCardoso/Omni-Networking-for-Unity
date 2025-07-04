using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using Omni.Core.Web;
using UnityEngine;
using HttpListenerRequest = Omni.Core.Web.Net.HttpListenerRequest;
using HttpListenerResponse = Omni.Core.Web.Net.HttpListenerResponse;
using Cookie = Omni.Core.Web.Net.Cookie;
using Omni.Shared;
using Newtonsoft.Json.Linq;
using Omni.Collections;
#if OMNI_RELEASE
using System.Runtime.CompilerServices;
#endif

namespace Omni.Core
{
    public static class NetworkExtensions
    {
        private static readonly string[] SizeSuffixes =
        {
            "B/s",
            "kB/s",
            "mB/s",
            "gB/s",
            "tB/s",
            "pB/s",
            "eB/s",
            "zB/s",
            "yB/s"
        };

        private static readonly Dictionary<string, Dictionary<string, object>> _sessions = new();

        internal static string ToSizeSuffix(this double value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException("decimalPlaces < 0");
            }

            if (value < 0)
            {
                return "-" + ToSizeSuffix(-value, decimalPlaces);
            }

            if (value == 0)
            {
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0);
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag)
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        /// <summary>
        /// Retrieves the <see cref="NetworkIdentity"/> component from the root of the transform
        /// affected by the 3D raycast hit.
        /// </summary>
        /// <param name="hit">The <see cref="RaycastHit"/> instance resulting from a raycast operation.</param>
        /// <returns>
        /// The <see cref="NetworkIdentity"/> component located on the root object of the impacted transform,
        /// or <c>null</c> if no <see cref="NetworkIdentity"/> is found.
        /// </returns>
        public static NetworkIdentity GetIdentity(this RaycastHit hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the <see cref="NetworkIdentity"/> component from the root of the transform
        /// impacted by the 2D raycast hit.
        /// </summary>
        /// <returns>
        /// The <see cref="NetworkIdentity"/> component located on the root object of the impacted transform,
        /// or <c>null</c> if no <see cref="NetworkIdentity"/> is found.
        /// </returns>
        public static NetworkIdentity GetIdentity(this RaycastHit2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity associated with the root of the transform involved in the collision.
        /// </summary>
        /// <param name="hit">The Collision object from which to extract the NetworkIdentity.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the collision, or null if not found.</returns>
        public static NetworkIdentity GetIdentity(this Collision hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component associated with the given Collider.
        /// </summary>
        /// <param name="hit">The Collider from which to retrieve the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the Collider, or null if none is found.</returns>
        public static NetworkIdentity GetIdentity(this Collider hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component from the root transform of a Collision2D instance.
        /// </summary>
        /// <param name="hit">The Collision2D instance from which to obtain the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component associated with the root transform of the collision, or null if not found.</returns>
        public static NetworkIdentity GetIdentity(this Collision2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component associated with the given Collider2D.
        /// </summary>
        /// <param name="hit">The Collider2D from which to obtain the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the specified Collider2D, or null if no such component exists.</returns>
        public static NetworkIdentity GetIdentity(this Collider2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static float ScaleDelta(this float input, float multiplier)
        {
            return input * multiplier * Time.deltaTime;
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static Vector3 ScaleDelta(this Vector3 input, float multiplier)
        {
            return input * multiplier * Time.deltaTime;
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static Vector2 ScaleDelta(this Vector2 input, float multiplier)
        {
            return input * multiplier * Time.deltaTime;
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static Vector4 ScaleDelta(this Vector4 input, float multiplier)
        {
            return input * multiplier * Time.deltaTime;
        }

        /// <summary>
        /// Sends the specified text as a response to the HTTP request.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="plainText">The text to send as the response body.</param>
        /// <param name="willBlock">Whether the write operation should block the calling thread.</param>
        /// <remarks>
        /// This extension method provides a convenient way to send text-based responses.
        /// </remarks>
        public static void Send(this HttpListenerResponse response, string plainText, int statusCode = 200, bool willBlock = true)
        {
            response.StatusCode = statusCode;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = NetworkHelper.IsValidJson(plainText) ? "application/json" : "text/plain";

            byte[] data = Encoding.UTF8.GetBytes(plainText);
            response.ContentLength64 = data.Length;
            response.Close(data, willBlock);
        }

        /// <summary>
        /// Sends the specified object as a JSON-formatted response to the HTTP request.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="result">The object to serialize to JSON for the response body.</param>
        /// <param name="willBlock">Whether the write operation should block the calling thread.</param>
        /// <remarks>
        /// This extension method provides a convenient way to send JSON-formatted responses.
        /// </remarks>
        public static void Send(this HttpListenerResponse response, object result, int statusCode = 200, bool willBlock = true)
        {
            response.StatusCode = statusCode;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "application/json";

            string json = NetworkManager.ToJson(result);
            byte[] data = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = data.Length;
            response.Close(data, willBlock);
        }

        /// <summary>
        /// Sends an HTTP 403 Forbidden response to the client, with an optional message.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="message">Optional message to include in the response body.</param>
        /// <param name="willBlock">Whether the write operation should block the calling thread.</param>
        /// <remarks>
        /// This extension method provides a convenient way to reject an HTTP request.
        /// </remarks>
        public static void Reject(this HttpListenerResponse response, string message = "Server rejected the request.", bool willBlock = true)
        {
            Send(response, message, 403, willBlock);
        }

        /// <summary>
        /// Sends an HTTP 403 Forbidden response to the client, with an optional JSON-serializable object.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="result">Optional JSON-serializable object to include in the response body.</param>
        /// <param name="willBlock">Whether the write operation should block the calling thread.</param>
        /// <remarks>
        /// This extension method provides a convenient way to reject an HTTP request.
        /// </remarks>
        public static void Reject(this HttpListenerResponse response, object result, bool willBlock = true)
        {
            Send(response, result, 403, willBlock);
        }

        /// <summary>
        /// Parses the HTTP request as a multipart/form-data request.
        /// </summary>
        /// <param name="request">The HTTP request to parse.</param>
        /// <returns>The parsed form data.</returns>
        /// <remarks>
        /// This extension method provides a convenient way to parse the body of an HTTP request
        /// as a multipart/form-data request.
        /// </remarks>
        public static MultipartFormDataParser ParseMultipartFormData(this HttpListenerRequest request)
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
        /// Parses the HTTP request as a multipart/form-data request, returning a <see cref="StreamingMultipartFormDataParser"/>
        /// which can be used to process the request in a streaming fashion. This is useful for handling large form data
        /// requests without loading the entire request into memory.
        /// </summary>
        /// <param name="request">The HTTP request to parse.</param>
        /// <returns>A <see cref="StreamingMultipartFormDataParser"/> which can be used to process the request in a streaming fashion.</returns>
        /// <remarks>
        /// This extension method provides a convenient way to parse the body of an HTTP request
        /// as a multipart/form-data request in a streaming fashion.
        /// </remarks>
        public static StreamingMultipartFormDataParser ParseMultipartFormDataStreaming(this HttpListenerRequest request)
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
        /// Gets the parameters from a GET or POST request as a dictionary of string key-value pairs.
        /// </summary>
        /// <param name="request">The HTTP request containing the parameters to parse.</param>
        /// <returns>A dictionary of string key-value pairs representing the parameters from the request.</returns>
        /// <remarks>
        /// This extension method provides a convenient way to parse the parameters from a GET or POST request.
        /// </remarks>
        public static Dictionary<string, string> ParseRequestParametersToDictionary(this HttpListenerRequest request)
        {
            try
            {
                if (request.HttpMethod == "GET")
                {
                    return NetworkHelper.ParseQueryStringToDictionary(request.QueryString);
                }
                else if (request.HttpMethod == "POST")
                {
                    if (request.ContentType.StartsWith("application/x-www-form-urlencoded"))
                    {
                        using StreamReader parameters = new(request.InputStream, request.ContentEncoding);
                        NameValueCollection queryString = HttpUtility.ParseQueryString(parameters.ReadToEnd());
                        return NetworkHelper.ParseQueryStringToDictionary(queryString);
                    }
                    else if (request.ContentType.StartsWith("application/json"))
                    {
                        using StreamReader parameters = new(request.InputStream, request.ContentEncoding);
                        return NetworkManager.FromJson<Dictionary<string, string>>(parameters.ReadToEnd());
                    }
                    else if (request.ContentType.StartsWith("multipart/form-data"))
                    {
                        throw new NotSupportedException($"Multipart/form-data requests are not supported. Use {nameof(ParseMultipartFormData)} instead.");
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported content type: " + request.ContentType);
                    }
                }

                throw new NotSupportedException("Error parsing request parameters, only GET and POST requests are supported.");
            }
            catch (Exception ex)
            {
                throw new Exception("ParseRequestParametersToDictionary -> " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Initializes a new session by setting a secure cookie with a unique session ID.
        /// </summary>
        /// <param name="response">The HTTP response to which the session cookie will be added.</param>
        /// <param name="request">The HTTP request used to determine security settings.</param>
        /// <param name="expires">Optional expiration time for the session cookie. Defaults to a non-persistent session if null.</param>
        public static void StartSession(this HttpListenerResponse response, HttpListenerRequest request, DateTime? expires = null)
        {
            if (expires == null)
            {
                expires = DateTime.MinValue;
            }

            string sessionId = Guid.NewGuid().ToString();
            Cookie sessionCookie = new Cookie("OMNI-SESSID", sessionId)
            {
                Path = "/",
                HttpOnly = true,
                Secure = request.IsSecureConnection,
                Expires = expires.Value,
            };

            response.SetCookie(sessionCookie);
            _sessions[sessionId] = new Dictionary<string, object>();
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
        public static Dictionary<string, object> GetSession(this HttpListenerRequest request)
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
        /// <param name="session">The session data as a dictionary of key-value pairs, if found.</param>
        /// <returns><c>true</c> if the session data is successfully retrieved; otherwise, <c>false</c>.</returns>
        public static bool TryGetSession(this HttpListenerRequest request, out Dictionary<string, object> session)
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
        public static bool DestroySession(this HttpListenerResponse response, HttpListenerRequest request)
        {
            var cookie = request.Cookies["OMNI-SESSID"];
            if (cookie == null)
                return false;

            // Set the session cookie to expire immediately
            StartSession(response, request, DateTime.UnixEpoch);

            string sessionId = cookie.Value;
            return _sessions.Remove(sessionId);
        }

        /// <summary>
        /// Creates a deep copy of an object through serialization and deserialization.
        /// </summary>
        /// <typeparam name="T">The type of object to clone. Must be serializable.</typeparam>
        /// <param name="obj">The source object to clone.</param>
        /// <param name="useBinarySerializer">
        /// When true, uses binary serialization for better performance.
        /// When false (default), uses JSON serialization for better compatibility.
        /// </param>
        /// <returns>A new instance of type T with all properties deeply copied.</returns>
        /// <exception cref="Exception">Thrown when serialization/deserialization fails.</exception>
        /// <remarks>
        /// Binary serialization is faster but less flexible than JSON serialization.
        /// JSON serialization better handles circular references and complex object graphs.
        /// </remarks>
        /// <example>
        /// var player = new PlayerData { Name = "Player1", Score = 100 };
        /// var clone = player.DeepClone(); // JSON serialization
        /// var fastClone = player.DeepClone(useBinarySerializer: true); // Binary serialization
        /// </example>
        public static T DeepClone<T>(this T obj, bool useBinarySerializer = false)
        {
            try
            {
                if (!useBinarySerializer)
                {
                    string json = NetworkManager.ToJson(obj);
                    return NetworkManager.FromJson<T>(json);
                }

                byte[] data = NetworkManager.ToBinary(obj);
                return NetworkManager.FromBinary<T>(data);
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"[Serialization] Operation failed for {typeof(T).Name}: {ex.Message}\n" +
                    $"Method: DeepClone ({(useBinarySerializer ? "Binary" : "JSON")})\n" +
                    $"Exception: {ex.GetType().Name}",
                    NetworkLogger.LogType.Error
                );

                throw;
            }
        }

        private static void ResolveType<T>(ref object @ref)
        {
            if (@ref is T)
                return;

            if (@ref is JObject jObject)
            {
                @ref = jObject.ToObject<T>();
                return;
            }

            if (@ref is JArray jArray)
            {
                @ref = jArray.ToObject<T>();
                return;
            }

            @ref = Convert.ChangeType(@ref, typeof(T));
        }

        /// <summary>
        /// Retrieves the value associated with the specified key from the dictionary and casts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <returns>The value associated with the specified key, casted to type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method retrieves the value associated with the specified key from the dictionary and casts it to the specified type.
        /// If the key is not found in the dictionary, this method will throw a KeyNotFoundException.
        /// </remarks>
        public static T Get<T>(this IDictionary<string, object> @this, string name)
        {
            var @ref = @this[name];
            try
            {
                ResolveType<T>(ref @ref);
                return (T)@ref;
            }
            catch (InvalidCastException)
            {
                NetworkLogger.__Log__(
                    $"[Dictionary] Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                    NetworkLogger.LogType.Error
                );
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"[Dictionary] Exception while casting value for key '{name}': {ex.Message}",
                    NetworkLogger.LogType.Error
                );
            }

            return default;
        }

        /// <summary>
        /// Retrieves the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// </summary>
        /// <typeparam name="T">The reference type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <returns>The value associated with the specified key, casted to type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method retrieves the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// It is intended for performance-sensitive scenarios where the type is known at compile-time and type safety is ensured by the caller.
        /// If the key is not found in the dictionary, this method will throw a KeyNotFoundException.
        /// </remarks>
        public static T UnsafeGet<T>(this IDictionary<string, object> @this, string name) where T : class
        {
            var @ref = @this[name];
            try
            {
                ResolveType<T>(ref @ref);
#if OMNI_RELEASE
                return Unsafe.As<T>(@ref);
#else
                return (T)@ref;
#endif
            }
            catch (InvalidCastException)
            {
                NetworkLogger.__Log__(
                    $"[Dictionary] Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                    NetworkLogger.LogType.Error
                );
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"[Dictionary] Exception while casting value for key '{name}': {ex.Message}",
                    NetworkLogger.LogType.Error
                );
            }

            return default;
        }

        /// <summary>
        /// Tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if the key is found; otherwise, the default value for type <typeparamref name="T"/>.</param>
        /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
        /// <remarks>
        /// This method tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified type.
        /// If the key is found in the dictionary, the value is assigned to the <paramref name="value"/> parameter and the method returns true; otherwise, it returns false.
        /// </remarks>
        public static bool TryGet<T>(this IDictionary<string, object> @this, string name, out T value)
        {
            value = default;
            if (@this.TryGetValue(name, out object @ref))
            {
                try
                {
                    ResolveType<T>(ref @ref);
                    value = (T)@ref;
                    return true;
                }
                catch (InvalidCastException)
                {
                    NetworkLogger.__Log__(
                        $"[Dictionary] Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                        NetworkLogger.LogType.Error
                    );
                }
                catch (Exception ex)
                {
                    NetworkLogger.__Log__(
                        $"[Dictionary] Exception while casting value for key '{name}': {ex.Message}",
                        NetworkLogger.LogType.Error
                    );
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// </summary>
        /// <typeparam name="T">The reference type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if the key is found; otherwise, the default value for type <typeparamref name="T"/>.</param>
        /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
        /// <remarks>
        /// This method tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// It is intended for performance-sensitive scenarios where the type is known at compile-time and type safety is ensured by the caller.
        /// If the key is found in the dictionary, the value is assigned to the <paramref name="value"/> parameter and the method returns true; otherwise, it returns false.
        /// </remarks>
        public static bool TryUnsafeGet<T>(this IDictionary<string, object> @this, string name, out T value)
            where T : class
        {
            value = default;
            if (@this.TryGetValue(name, out object @ref))
            {
                try
                {
                    ResolveType<T>(ref @ref);
#if OMNI_RELEASE
                    value = Unsafe.As<T>(@ref);
#else
                    value = (T)@ref;
#endif
                    return true;
                }
                catch (InvalidCastException)
                {
                    NetworkLogger.__Log__(
                        $"[Dictionary] Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                        NetworkLogger.LogType.Error
                    );
                }
                catch (Exception ex)
                {
                    NetworkLogger.__Log__(
                        $"[Dictionary] Exception while casting value for key '{name}': {ex.Message}",
                        NetworkLogger.LogType.Error
                    );
                }
            }

            return false;
        }

        public static ObservableDictionary<TKey, TValue> ToObservableDictionary<TKey, TValue, TSource>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) where TKey : notnull
        {
            var dict = new ObservableDictionary<TKey, TValue>();
            foreach (var item in source)
                dict.Add(keySelector(item), valueSelector(item));
            return dict;
        }
    }
}