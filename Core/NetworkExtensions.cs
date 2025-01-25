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

        /// <summary>
        /// Spawns a network identity on both server and specified target clients with given delivery and caching options.
        /// </summary>
        /// <param name="prefab">The network identity prefab to spawn.</param>
        /// <param name="peer">The network peer to receive the spawned object.</param>
        /// <returns>The spawned network identity instance.</returns>
        public static NetworkIdentity Spawn(this NetworkIdentity prefab, NetworkPeer peer, ServerOptions options)
        {
            var identity = NetworkManager.SpawnOnServer(prefab, peer);
            identity.SpawnOnClient(options);
            return identity;
        }

        /// <summary>
        /// Spawns a network identity on the server and specified client targets with defined delivery and caching options.
        /// </summary>
        /// <param name="prefab">The network identity prefab to instantiate.</param>
        /// <param name="peer">The network peer that will receive the instantiated object.</param>
        /// <param name="target">Specifies the target clients for the instantiated object.</param>
        /// <param name="deliveryMode">Determines the manner in which the instantiated object is delivered over the network.</param>
        /// <param name="groupId">An identifier used for organizing network messages into groups.</param>
        /// <param name="dataCache">Optional parameter for caching additional data associated with the instantiation process.</param>
        /// <param name="sequenceChannel">The sequence channel used to maintain message order for network delivery.</param>
        /// <returns>The instantiated network identity as observed on the server.</returns>
        public static NetworkIdentity Spawn(this NetworkIdentity prefab, NetworkPeer peer, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            var identity = NetworkManager.SpawnOnServer(prefab, peer);
            identity.SpawnOnClient(target, deliveryMode, groupId, dataCache, sequenceChannel);
            return identity;
        }

        /// <summary>
        /// Spawns a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peer">The peer who will receive the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnServer(this NetworkIdentity prefab, NetworkPeer peer)
        {
            return NetworkManager.SpawnOnServer(prefab, peer);
        }

        /// <summary>
        /// Spawns a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peer">The peer who will receive the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnServer(this NetworkIdentity prefab, NetworkPeer peer, int identityId)
        {
            return NetworkManager.SpawnOnServer(prefab, peer, identityId);
        }

        /// <summary>
        /// Spawns a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The network identity prefab to instantiate on the server.</param>
        /// <param name="peerId">The network peer associated with the spawned object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity object on the server.</returns>
        public static NetworkIdentity SpawnOnServer(this NetworkIdentity prefab, int peerId, int identityId = 0)
        {
            return NetworkManager.SpawnOnServer(prefab, peerId, identityId);
        }

        /// <summary>
        /// Spawns a network identity on a client with specified peer and identity identifiers.
        /// </summary>
        /// <param name="prefab">The network identity prefab to instantiate on the client.</param>
        /// <param name="peerId">The identifier of the peer who will own the instantiated object.</param>
        /// <param name="identityId">The unique identifier for the instantiated network identity.</param>
        /// <returns>The instantiated network identity on the client.</returns>
        public static NetworkIdentity SpawnOnClient(this NetworkIdentity prefab, int peerId, int identityId)
        {
            return NetworkManager.SpawnOnClient(prefab, peerId, identityId);
        }

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
    }
}