using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using DataSourceServer;
using DataSourceServer.Channel;
using DataSourceServer.Message;
using DataSourceServer.Message.Event;
using KinectDataSourceServer.Sensor;
using SuperWebSocket;

namespace KinectDataSourceServer
{
    public class KinectRequestRouter : RequestRouter
    {
        internal ISensorStreamHandler[] StreamHandlers { get; private set; }

        private readonly Dictionary<string, ISensorStreamHandler> streamHandlerMap;
        private readonly Dictionary<string, string> uriName2StreamNameMap;

        private List<WebSocketEventChannel> eventChannels;

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

            Initialize(streamHandlerFactories);
        }

        private void Initialize(Collection<ISensorStreamHandlerFactory> streamHandlerFactories)
        {
            this.StreamHandlers = new ISensorStreamHandler[streamHandlerFactories.Count];

            var streamHandlerContext = new SensorStreamHandlerContext(this.SendEventMessageAsync);
            var normalizedNameSet = new HashSet<string>();

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
            foreach (var channel in this.eventChannels.SafeCopy())
            {
                bool success = channel.SendMessage(message.ToTextMessage());
                if (!success)
                {
                    Console.WriteLine("Cannot send event message to a channel");
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

        private void HandleGetStateRequest(WebSocketSession session)
        {
            var responseProperties = new Dictionary<string, object>();
            foreach (var mapEntry in this.streamHandlerMap)
            {
                var handlerStatus = mapEntry.Value.GetState(mapEntry.Key);
                responseProperties.Add(mapEntry.Key, handlerStatus);
            }

            string json = responseProperties.DictionaryToJson();
            session.Send(json);
        }

        private void HandleStateRequest(WebSocketSession session)
        {
            switch (session.Method)
            {
                case "GET":
                    this.HandleGetStateRequest(session);
                    break;

                default:
                    session.CloseWithHandshake(405, "MethodNotAllowed");
                    break;
            }
        }

        private void HandleEventRequest(WebSocketSession session)
        {
            WebSocketEventChannel.TryOpen(session,
                channel =>
                {
                    this.eventChannels.Add(channel);
                    Console.WriteLine("New channel for {0} is added", session.Path);
                },
                channel =>
                {
                    this.eventChannels.Remove(channel);
                    Console.WriteLine("Channel for {0} is removed", session.Path);
                });
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
                        //await this.HandleStateRequest(requestContext);
                        break;

                    case StreamUriSubpath:
                        //this.HandleStreamRequest(requestContext);
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
