using System;
using System.IO;
using DataSourceServer.Serialization;

namespace DataSourceServer.Message
{
    public static class WebSocketMessageExtensions
    {
        public static WebSocketMessage ToTextMessage<T>(this T obj)
        {
            using (var stream = new MemoryStream())
            {
                ;
                return new WebSocketMessage(obj.ToJson());
            }
        }

        //public static WebSocketMessage ToTextMessage(this byte[] textData)
        //{
        //    return new WebSocketMessage(new ArraySegment<byte>(textData), false);
        //}

        public static WebSocketMessage ToBinaryMessage(this byte[] data)
        {
            return new WebSocketMessage(new ArraySegment<byte>(data));
        }
    }
}
