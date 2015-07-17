using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataSourceServer.Message.Stream
{
    public class MessageHandPointer
    {
        public int trackingId { get; set; }

        public string handType { get; set; }

        public string handEventType { get; set; }

        public bool isTracked { get; set; }

        public bool isActive { get; set; }

        public bool isInteractive { get; set; }

        public bool isPressed { get; set; }

        public bool isPrimaryHandOfUser { get; set; }

        public bool isPrimaryUser { get; set; }

        public double x { get; set; }

        public double y { get; set; }

        public double pressExtent { get; set; }

        public double rawX { get; set; }

        public double rawY { get; set; }

        public double rawZ { get; set; }

        public static implicit operator MessageHandPointer(Dictionary<string, object> handPointerDictionary)
        {
            return FromDictionary(handPointerDictionary);
        }

        public static MessageHandPointer FromDictionary(Dictionary<string, object> handPointerDictionary)
        {
            if (handPointerDictionary == null)
            {
                throw new ArgumentNullException("handPointerDictionary");
            }

            var messageHandPointer = new MessageHandPointer
            {
                trackingId = (int)handPointerDictionary["trackingId"],
                handType = (string)handPointerDictionary["handType"],
                handEventType = (string)handPointerDictionary["handEventType"],
                isTracked = (bool)handPointerDictionary["isTracked"],
                isActive = (bool)handPointerDictionary["isActive"],
                isInteractive = (bool)handPointerDictionary["isInteractive"],
                isPressed = (bool)handPointerDictionary["isPressed"],
                isPrimaryHandOfUser = (bool)handPointerDictionary["isPrimaryHandOfUser"],
                isPrimaryUser = (bool)handPointerDictionary["isPrimaryUser"],
                x = double.Parse(handPointerDictionary["x"].ToString(), CultureInfo.InvariantCulture),
                y = double.Parse(handPointerDictionary["y"].ToString(), CultureInfo.InvariantCulture),
                pressExtent = double.Parse(handPointerDictionary["pressExtent"].ToString(), CultureInfo.InvariantCulture),
                rawX = double.Parse(handPointerDictionary["rawX"].ToString(), CultureInfo.InvariantCulture),
                rawY = double.Parse(handPointerDictionary["rawY"].ToString(), CultureInfo.InvariantCulture),
                rawZ = double.Parse(handPointerDictionary["rawZ"].ToString(), CultureInfo.InvariantCulture)
            };
            return messageHandPointer;
        }
    }
}
