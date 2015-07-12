using DataSourceServer.Message;
using KinectDataSourceServer.Sensor.Serialization.Message;
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

        internal SensorStatusStreamHandler(SensorStreamHandlerContext context)
            : base(context)
        {

        }

        public async override void OnSensorChanged(Microsoft.Kinect.KinectSensor newSensor)
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
    }
}
