using System;
using System.Collections.Generic;
using System.Threading;
using DataSourceServer;
using KinectDataSourceServer.KinectFrameHandler;
using Microsoft.Kinect;

namespace KinectDataSourceServer
{
    public class KinectDataDispatcher
    {
        private Server dataSourceServer;
        private bool isRegisterAllFrameReady;
        private List<AbstractKinectFramesHandler> kinectFramesHandlers;

        public KinectDataDispatcher(Server dataSourceServer, bool isRegisterAllFrameReady = true)
        {
            this.dataSourceServer = dataSourceServer;
            this.isRegisterAllFrameReady = isRegisterAllFrameReady;
            this.kinectFramesHandlers = new List<AbstractKinectFramesHandler>();

            kinectFramesHandlers.Add(new UpdateImageKinectFrameHandler(dataSourceServer));
            KinectState.Instance.NewKinect += RegisterCallbackToSensor;
        }

        private void RegisterCallbackToSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (isRegisterAllFrameReady)
                {
                    sensor.AllFramesReady += sensor_AllFramesReady;
                }
                else
                {
                    sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;
                    sensor.DepthFrameReady += sensor_DepthFrameReady;
                    sensor.ColorFrameReady += sensor_ColorFrameReady;
                }
            }
        }

        public void Start()
        {
            KinectState.Instance.StartKinect();
        }

        public void Stop()
        {
            KinectState.Instance.StopKinect();
        }

        private void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            handleSkeletonFrame(e.OpenSkeletonFrame());
            this.dispatchData(kinectFramesHandler =>
            {
                kinectFramesHandler.SkeletonRawFrameCallback(e);
            });
        }

        private void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            handleDepthImageFrame(e.OpenDepthImageFrame());
            this.dispatchData(kinectFramesHandler =>
            {
                kinectFramesHandler.DepthRawFrameCallback(e);
            });
        }

        private void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            handleColorImageFrame(e.OpenColorImageFrame());
            this.dispatchData(kinectFramesHandler =>
            {
                kinectFramesHandler.ColorRawFrameCallback(e);
            });
        }

        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            handleSkeletonFrame(e.OpenSkeletonFrame());
            handleDepthImageFrame(e.OpenDepthImageFrame());
            handleColorImageFrame(e.OpenColorImageFrame());
            this.dispatchData(kinectFramesHandler =>
            {
                kinectFramesHandler.AllRawFrameCallback(e);
            });
        }

        private void dispatchData(Action<AbstractKinectFramesHandler> callback)
        {
            kinectFramesHandlers.ForEach(kinectFramesHandler =>
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
                {
                    callback(kinectFramesHandler);
                }));
            });
        }

        private void handleSkeletonFrame(SkeletonFrame skeletonFrame)
        {
            using (skeletonFrame)
            {
                if (skeletonFrame != null)
                {
                    Skeleton[] skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletonData);

                    this.dispatchData(kinectFramesHandler =>
                    {
                        kinectFramesHandler.SkeletonFrameCallback(skeletonFrame.Timestamp, skeletonFrame.FrameNumber, skeletonData);
                    });
                }
            }
        }

        private void handleDepthImageFrame(DepthImageFrame depthFrame)
        {
            using (depthFrame)
            {
                if (depthFrame != null)
                {
                    DepthImagePixel[] depthPixels = new DepthImagePixel[depthFrame.PixelDataLength];
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);

                    this.dispatchData(kinectFramesHandler =>
                    {
                        kinectFramesHandler.DepthFrameCallback(depthFrame.Timestamp, depthFrame.FrameNumber, depthPixels);
                    });
                }
            }
        }

        private void handleColorImageFrame(ColorImageFrame colorFrame)
        {
            using (colorFrame)
            {
                if (colorFrame != null)
                {
                    byte[] colorPixels = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyPixelDataTo(colorPixels);

                    this.dispatchData(kinectFramesHandler =>
                    {
                        kinectFramesHandler.ColorFrameCallback(colorFrame.Timestamp, colorFrame.FrameNumber, colorPixels);
                    });
                }
            }
        }
    }
}
