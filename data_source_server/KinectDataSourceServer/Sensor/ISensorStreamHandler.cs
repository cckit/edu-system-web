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

        Task UninitializeAsync();
    }
}
