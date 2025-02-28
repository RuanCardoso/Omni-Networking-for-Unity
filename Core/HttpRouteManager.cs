using System;
using System.Threading.Tasks;
using UnityEngine;
using HttpListenerRequest = Omni.Core.Web.Net.HttpListenerRequest;
using HttpListenerResponse = Omni.Core.Web.Net.HttpListenerResponse;

namespace Omni.Core.Web
{
    /// <summary>
    /// Manages HTTP server operations, including route registration and request handling.
    /// This class provides both synchronous and asynchronous methods for handling HTTP GET and POST requests.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class HttpRouteManager : WebCommunicationManager
    {
        public event Action<HttpListenerRequest, HttpListenerResponse, string> OnRequestHandled
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

        internal void StartServices(bool enableSsl, int port)
        {
            StartServices((webSocket, httpServer) =>
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
        public void GetAsync(string routeName, Func<HttpListenerRequest, HttpListenerResponse, Task> callback)
        {
            HttpServer.GetAsync(routeName, callback);
        }

        /// <summary>
        /// Registers a route for HTTP GET requests with the given <paramref name="routeName"/>.
        /// Invokes the specified <paramref name="callback"/> when a GET request is received with a matching route.
        /// </summary>
        /// <param name="routeName">The route name to register the callback for.</param>
        /// <param name="callback">The callback to invoke when a GET request is received with a matching route.</param>
        /// <exception cref="Exception">Thrown if the HTTP Server is not initialized.</exception>

        public void Get(string routeName, Action<HttpListenerRequest, HttpListenerResponse> callback)
        {
            HttpServer.Get(routeName, callback);
        }

        /// <summary>
        /// Registers a route for HTTP POST requests with the given <paramref name="routeName"/>.
        /// The given callback is invoked when a POST request is received with a matching route.
        /// </summary>
        /// <param name="routeName">The route name to register the callback for.</param>
        /// <param name="callback">The callback to invoke when a POST request is received with a matching route.</param>
        public void PostAsync(string routeName, Func<HttpListenerRequest, HttpListenerResponse, Task> callback)
        {
            HttpServer.PostAsync(routeName, callback);
        }

        /// <summary>
        /// Registers a route for HTTP POST requests with the given <paramref name="routeName"/>.
        /// The given callback is invoked when a POST request is received with a matching route.
        /// The callback is provided with the request and response objects, but does not have to return a value.
        /// </summary>
        /// <param name="routeName">The route name to register the callback for.</param>
        /// <param name="callback">The callback to invoke when a POST request is received with a matching route.</param>
        public void Post(string routeName, Action<HttpListenerRequest, HttpListenerResponse> callback)
        {
            HttpServer.Post(routeName, callback);
        }
    }
}