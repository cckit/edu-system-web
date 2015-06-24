using Microsoft.Kinect;

namespace KinectDataSourceServer
{
    public abstract class AbstractKinectFramesHandler
    {
        public virtual void SkeletonFrameCallback(long timestamp, int frameNumber, Skeleton[] skeletonData) { }
        public virtual void DepthFrameCallback(long timestamp, int frameNumber, DepthImagePixel[] depthPixels) { }
        public virtual void ColorFrameCallback(long timestamp, int frameNumber, byte[] colorPixels) { }
    }
}
