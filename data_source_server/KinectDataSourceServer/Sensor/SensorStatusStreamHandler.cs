using System.Collections.Generic;
using DataSourceServer.Message;
using DataSourceServer.Message.Event;
using Microsoft.Kinect;

namespace KinectDataSourceServer.Sensor
{
    public class SensorStatusStreamHandler : SensorStreamHandlerBase
    {
        private KinectSensor sensor;
        private bool isConnected;

        internal const string SensorStreamName = "sensorStatus";
        internal const string SensorStatusEventCategory = "sensorStatus";
        internal const string SensorStatusEventType = "statusChanged";
        internal const string SensorStatusConnectedPropertyName = "connected";

        internal SensorStatusStreamHandler(SensorStreamHandlerContext context)
            : base(context)
        {
            this.AddStreamConfiguration(SensorStreamName, new StreamConfiguration(this.GetSensorStreamProperties, this.SetSensorStreamProperty));
        }

        public async override void OnSensorChanged(KinectSensor newSensor)
        {
            if (this.sensor != newSensor)
            {
                bool oldConnected = this.isConnected;
                this.isConnected = (newSensor != null);

                if (oldConnected != this.isConnected)
                {
                    await this.Context.SendEventMessageAsync(new SensorStatusEventMessage()
                    {
                        category = SensorStatusEventCategory,
                        eventType = SensorStatusEventType,
                        connected = this.isConnected
                    });
                }
            }

            this.sensor = newSensor;
        }

        private void GetSensorStreamProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(SensorStatusConnectedPropertyName, this.isConnected);
        }

        private string SetSensorStreamProperty(string propertyName, object propertyValue)
        {
            if (propertyName == SensorStatusConnectedPropertyName)
            {
                return Properties.Resources.PropertyReadOnly;
            }
            else
            {
                return Properties.Resources.PropertyNameUnrecognized;
            }
        }
    }
}
