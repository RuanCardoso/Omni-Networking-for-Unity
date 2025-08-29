using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Omni.Core.Web
{
    /// <summary>
    /// Manages HTTP server operations, including route registration and request handling.
    /// This class provides methods for handling HTTP GET and POST requests.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class HttpRouteManager : WebCommunicationManager
    {
        public event Action<kHttpRequest, kHttpResponse, string> OnRequestHandled
        {
            add
            {
                HttpServer.OnRequestHandled += value;
            }
            remove
            {
                HttpServer.OnRequestHandled -= value;
            }
        }

        internal void StartServices(bool enableSsl, int port, KestrelOptions kestrelOptions)
        {
            StartServices(kestrelOptions, (webSocket, httpServer) =>
            {
                webSocket.Enabled = false; // WebSocket is enabled by default, so we disable it here.
                // Setup Http Server configuration
                httpServer.EnableSsl = enableSsl;
                httpServer.Port = port;
            });
        }

        /// <summary>
        /// Registers a route for HTTP GET requests with the given <paramref name="routeName"/>.</summary>
        /// <param name="routeName">The route name to register the callback for.</param>
        /// <param name="callback">The callback to invoke when a GET request is received with a matching route.</param>
        /// <remarks>
        /// The HTTP Server must be enabled in the NetworkManager configuration for this to work.
        /// </remarks>
        public void Get(string routeName, Action<kHttpRequest, kHttpResponse> callback)
        {
            HttpServer.Get(routeName, callback);
        }

        /// <summary>
        /// Registers a route for HTTP POST requests with the given <paramref name="routeName"/>.
        /// The given callback is invoked when a POST request is received with a matching route.
        /// </summary>
        /// <param name="routeName">The route name to register the callback for.</param>
        /// <param name="callback">The callback to invoke when a POST request is received with a matching route.</param>
        public void Post(string routeName, Action<kHttpRequest, kHttpResponse> callback)
        {
            HttpServer.Post(routeName, callback);
        }
    }
}