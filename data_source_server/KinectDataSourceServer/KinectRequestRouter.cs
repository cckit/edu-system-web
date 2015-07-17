using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DataSourceServer;
using DataSourceServer.Channel;
using DataSourceServer.Message;
using DataSourceServer.Message.Event;
using DataSourceServer.Message.Stream;
using DataSourceServer.Serialization;
using KinectDataSourceServer.Sensor;
using Newtonsoft.Json.Linq;
using SuperWebSocket;

namespace KinectDataSourceServer
{
    public class KinectRequestRouter : RequestRouter
    {
        internal ISensorStreamHandler[] StreamHandlers { get; private set; }

        private readonly Dictionary<string, ISensorStreamHandler> streamHandlerMap;
        private readonly Dictionary<string, string> uriName2StreamNameMap;

        private List<WebSocketEventChannel> eventChannels;
        private List<WebSocketEventChannel> streamChannels;

        public const string StreamUriSubpath = "STREAM";
        public const string EventsUriSubpath = "EVENTS";
        public const string StateUriSubpath = "STATE";
        public const string SuccessPropertyName = "success";
        public const string ErrorsPropertyName = "errors";

        private static readonly HashSet<string> ReservedNames = new HashSet<string>
                                                                {
                                                                    SuccessPropertyName.ToUpperInvariant(),
                                                                    ErrorsPropertyName.ToUpperInvariant(),
                                                                    StreamUriSubpath.ToUpperInvariant(),
                                                                    EventsUriSubpath.ToUpperInvariant(),
                                                                    StateUriSubpath.ToUpperInvariant()
                                                                };

        public KinectRequestRouter(Collection<ISensorStreamHandlerFactory> streamHandlerFactories)
        {
            this.streamHandlerMap = new Dictionary<string, ISensorStreamHandler>();
            this.uriName2StreamNameMap = new Dictionary<string, string>();
            this.eventChannels = new List<WebSocketEventChannel>();
            this.streamChannels = new List<WebSocketEventChannel>();

            Initialize(streamHandlerFactories);
        }

        private void Initialize(Collection<ISensorStreamHandlerFactory> streamHandlerFactories)
        {
            this.StreamHandlers = new ISensorStreamHandler[streamHandlerFactories.Count];

            var normalizedNameSet = new HashSet<string>();
            var streamHandlerContext = new SensorStreamHandlerContext(this.SendEventMessageAsync, this.SendStreamMessageAsync);

            for (int i = 0; i < streamHandlerFactories.Count; i++)
            {
                var handler = streamHandlerFactories[i].CreateHandler(streamHandlerContext);
                var names = handler.GetSupportedStreamNames();
                this.StreamHandlers[i] = handler;

                foreach (var name in names)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new InvalidOperationException(@"Empty stream names are not supported");
                    }

                    if (name.IndexOfAny(SharedConstants.UriPathComponentDelimiters) >= 0)
                    {
                        throw new InvalidOperationException(@"Stream names can't contain '/' character");
                    }

                    var normalizedName = name.ToUpperInvariant();

                    if (ReservedNames.Contains(normalizedName))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "'{0}' is a reserved stream name", normalizedName));
                    }

                    if (normalizedNameSet.Contains(normalizedName))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "'{0}' is a duplicate stream name", normalizedName));
                    }

                    normalizedNameSet.Add(normalizedName);

                    this.uriName2StreamNameMap.Add(normalizedName, name);
                    this.streamHandlerMap.Add(name, handler);
                }
            }
        }

        private async Task SendEventMessageAsync(EventMessage message)
        {
            var webSocketMessage = message.ToTextMessage();

            foreach (var channel in this.eventChannels.SafeCopy())
            {
                bool success = channel.SendMessage(webSocketMessage);
                if (!success)
                {
                    Console.WriteLine("Cannot send event message to a channel");
                }
                else
                {
                    Console.WriteLine("Sent an event message to a channel");
                }
            }
        }

        private async Task SendStreamMessageAsync(StreamMessage message, byte[] binaryPayload)
        {
            var webSocketMessage = message.ToTextMessage();

            foreach (var channel in this.streamChannels.SafeCopy())
            {
                if (binaryPayload != null)
                {
                    channel.SendMessage(webSocketMessage, new WebSocketMessage(new ArraySegment<byte>(binaryPayload)));
                }
                else
                {
                    channel.SendMessage(webSocketMessage);
                }
            }
        }

        private Tuple<string, string> SplitUriSubpath(string path)
        {
            if (!path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            path = path.Substring(1).ToUpperInvariant();
            int delimiterIndex = path.IndexOfAny(SharedConstants.UriPathComponentDelimiters);
            var firstComponent = (delimiterIndex < 0) ? path : path.Substring(0, delimiterIndex);
            var remainingSubpath = (delimiterIndex < 0) ? null : path.Substring(delimiterIndex);

            return new Tuple<string, string>(firstComponent, remainingSubpath);
        }

        private void HandlePostStateRequest(WebSocketSession session, string message)
        {
            Dictionary<string, object> requestProperties;

            try
            {
                requestProperties = message.DictionaryFromJson();
            }
            catch (SerializationException)
            {
                requestProperties = null;
            }

            if (requestProperties == null)
            {
                return;
            }

            var responseProperties = new Dictionary<string, object>();
            var errorStreamNames = new List<string>();

            foreach (var requestEntry in requestProperties)
            {
                ISensorStreamHandler handler;
                if (!this.streamHandlerMap.TryGetValue(requestEntry.Key, out handler))
                {
                    // Don't process unrecognized handlers
                    responseProperties.Add(requestEntry.Key, Properties.Resources.StreamNameUnrecognized);
                    errorStreamNames.Add(requestEntry.Key);
                    continue;
                }

                var typ = requestEntry.Value.GetType();
                Dictionary<string, object> propertiesToSet = null;
                if ("JObject".Equals(typ.Name))
                {
                    propertiesToSet = (requestEntry.Value as JObject).ToObject<Dictionary<string, object>>();
                }
                else
                {
                    System.Console.WriteLine();
                }

                if (propertiesToSet == null)
                {
                    continue;
                }

                var propertyErrors = new Dictionary<string, object>();
                var success = handler.SetState(requestEntry.Key, new ReadOnlyDictionary<string, object>(propertiesToSet), propertyErrors);

                if (!success)
                {
                    responseProperties.Add(requestEntry.Key, propertyErrors);
                    errorStreamNames.Add(requestEntry.Key);
                }
            }

            if (errorStreamNames.Count == 0)
            {
                responseProperties.Add(SuccessPropertyName, true);
            }
            else
            {
                responseProperties.Add(SuccessPropertyName, false);
                responseProperties.Add(ErrorsPropertyName, errorStreamNames.ToArray());
            }

            string responseJson = responseProperties.DictionaryToJson();
            session.Send(responseJson);
        }

        private void HandleGetStateRequest(WebSocketSession session)
        {
            var responseProperties = new Dictionary<string, object>();
            foreach (var mapEntry in this.streamHandlerMap)
            {
                var handlerStatus = mapEntry.Value.GetState(mapEntry.Key);
                responseProperties.Add(mapEntry.Key, handlerStatus);
            }

            string json = responseProperties.DictionaryToJson();
            Console.WriteLine(json);
            session.Send(json);
        }

        private void HandleStreamRequest(WebSocketSession session)
        {
            WebSocketEventChannel.TryOpen(session,
                channel =>
                {
                    this.streamChannels.Add(channel);
                    Console.WriteLine("New stream channel for {0} is added", session.Path);
                },
                channel =>
                {
                    this.streamChannels.Remove(channel);
                    Console.WriteLine("Stream channel for {0} is removed", session.Path);
                });
        }

        private void HandleEventRequest(WebSocketSession session)
        {
            WebSocketEventChannel.TryOpen(session,
                channel =>
                {
                    this.eventChannels.Add(channel);
                    Console.WriteLine("New event channel for {0} is added", session.Path);
                },
                channel =>
                {
                    this.eventChannels.Remove(channel);
                    Console.WriteLine("Event channel for {0} is removed", session.Path);
                });
        }

        public override void OnNewMessage(WebSocketSession session, string message, string subPath)
        {
            if (string.IsNullOrEmpty(subPath))
            {
                throw new ArgumentNullException("path");
            }

            var splitPath = SplitUriSubpath(subPath);

            if (splitPath == null)
            {
                session.CloseWithHandshake(404, "Not Found");
                return;
            }

            var pathComponent = splitPath.Item1;

            try
            {
                switch (pathComponent)
                {
                    case StateUriSubpath:
                        this.HandlePostStateRequest(session, message);
                        break;

                    default:
                        var remainingSubpath = splitPath.Item2;
                        if (remainingSubpath == null)
                        {
                            session.CloseWithHandshake(404, "Not Found");
                            return;
                        }

                        string streamName;
                        if (!this.uriName2StreamNameMap.TryGetValue(pathComponent, out streamName))
                        {
                            session.CloseWithHandshake(404, "Not Found");
                            return;
                        }

                        var streamHandler = this.streamHandlerMap[streamName];

                        // Handle request

                        break;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Exception encountered while handling Kinect sensor request:\n{0}", e);
                session.CloseWithHandshake(500, "Internal server error");
            }
        }

        public override void OnNewRequest(WebSocketSession session, string subPath)
        {
            if (string.IsNullOrEmpty(subPath))
            {
                throw new ArgumentNullException("path");
            }

            var splitPath = SplitUriSubpath(subPath);

            if (splitPath == null)
            {
                session.CloseWithHandshake(404, "Not Found");
                return;
            }

            var pathComponent = splitPath.Item1;

            try
            {
                switch (pathComponent)
                {
                    case StateUriSubpath:
                        this.HandleGetStateRequest(session);
                        break;

                    case StreamUriSubpath:
                        this.HandleStreamRequest(session);
                        break;

                    case EventsUriSubpath:
                        this.HandleEventRequest(session);
                        break;

                    default:
                        var remainingSubpath = splitPath.Item2;
                        if (remainingSubpath == null)
                        {
                            session.CloseWithHandshake(404, "Not Found");
                            return;
                        }

                        string streamName;
                        if (!this.uriName2StreamNameMap.TryGetValue(pathComponent, out streamName))
                        {
                            session.CloseWithHandshake(404, "Not Found");
                            return;
                        }

                        var streamHandler = this.streamHandlerMap[streamName];

                        // Handle request

                        break;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Exception encountered while handling Kinect sensor request:\n{0}", e);
                session.CloseWithHandshake(500, "Internal server error");
            }
        }
    }

    internal static class ChannelListHelper
    {
        public static IEnumerable<WebSocketEventChannel> SafeCopy(this List<WebSocketEventChannel> list)
        {
            var channels = new WebSocketEventChannel[list.Count];
            list.CopyTo(channels);
            return channels;
        }
    }
}
