using System;
using DataSourceServer.Message.Event;

namespace KinectDataSourceServer.Sensor.Interaction
{
    public class UserStateChangedEventArgs : EventArgs
    {
        public EventMessage Message { get; private set; }

        public UserStateChangedEventArgs(EventMessage message)
        {
            this.Message = message;
        }
    }
}
