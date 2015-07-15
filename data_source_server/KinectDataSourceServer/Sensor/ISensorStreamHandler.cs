using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectDataSourceServer.Sensor
{
    public interface ISensorStreamHandler
    {
        void OnSensorChanged(KinectSensor newSensor);

        void ProcessColor(byte[] colorData, ColorImageFrame colorFrame);

        void ProcessDepth(DepthImagePixel[] depthData, DepthImageFrame depthFrame);

        void ProcessSkeleton(Skeleton[] skeletons, SkeletonFrame skeletonFrame);

        string[] GetSupportedStreamNames();

        IDictionary<string, object> GetState(string streamName);

        bool SetState(string streamName, IReadOnlyDictionary<string, object> properties, IDictionary<string, object> errors);

        Task UninitializeAsync();
    }
}
