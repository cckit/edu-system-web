using System;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;

namespace KinectDataSourceServer.Sensor
{
    internal sealed class KinectRawFrameHandler
    {
        private readonly KinectSensorChooser sensorChooser;
        private KinectSensor sensor;

        private ISensorStreamHandler[] streamHandlers;
        private Skeleton[] skeletons;

        private readonly bool IS_NEAR_RANGE = true;
        private readonly DepthImageFormat DEFAULT_DEPTH_IMAGE_FORMAT = DepthImageFormat.Resolution640x480Fps30;
        private readonly ColorImageFormat DEFAULT_COLOR_IMAGE_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

        public KinectRawFrameHandler(KinectSensorChooser sensorChooser, ISensorStreamHandler[] streamHandlers)
        {
            this.sensorChooser = sensorChooser;
            this.streamHandlers = streamHandlers;
        }

        public Task InitializeAsync()
        {
            this.OnKinectChanged(this.sensorChooser.Kinect);
            this.sensorChooser.KinectChanged += (sender, e) => OnKinectChanged(e.NewSensor);
            this.sensorChooser.Start();
            return SharedConstants.EmptyCompletedTask;
        }

        public async Task UninitializeAsync()
        {
            this.OnKinectChanged(null);

            foreach (var handler in this.streamHandlers)
            {
                await handler.UninitializeAsync();
            }
        }

        #region Kinect change event

        private void OnKinectChanged(KinectSensor newSensor)
        {
            pruneOldSensor();
            onNewSensor(newSensor);

            // Notify all handlers for the new sensor
            Array.ForEach(this.streamHandlers, handler => handler.OnSensorChanged(newSensor));
        }

        private void pruneOldSensor()
        {
            if (this.sensor != null)
            {
                try
                {
                    this.sensor.AllFramesReady -= this.SensorAllFrameReady;

                    this.sensor.ColorStream.Disable();
                    this.sensor.DepthStream.Range = DepthRange.Default;
                    this.sensor.SkeletonStream.EnableTrackingInNearRange = false;
                    this.sensor.DepthStream.Disable();
                    this.sensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
                this.sensor = null;
            }
        }

        private void onNewSensor(KinectSensor newSensor)
        {
            if (newSensor != null)
            {
                this.sensor = newSensor;

                try
                {
                    newSensor.ColorStream.Enable(DEFAULT_COLOR_IMAGE_FORMAT);
                    newSensor.DepthStream.Enable(DEFAULT_DEPTH_IMAGE_FORMAT);
                    newSensor.SkeletonStream.Enable();

                    if (IS_NEAR_RANGE)
                    {
                        try
                        {
                            newSensor.DepthStream.Range = DepthRange.Near;
                            newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                        }
                        catch (InvalidOperationException)
                        {
                            newSensor.DepthStream.Range = DepthRange.Default;
                            newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                        }
                    }

                    this.skeletons = new Skeleton[newSensor.SkeletonStream.FrameSkeletonArrayLength];

                    newSensor.AllFramesReady += this.SensorAllFrameReady;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        #endregion

        #region Stream handling

        private void SensorAllFrameReady(object sender, AllFramesReadyEventArgs e)
        {
            if (this.sensor != sender)
            {
                return;
            }

            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                HandleSkeletonFrame(skeletonFrame);
            }

            using (var colorFrame = e.OpenColorImageFrame())
            {
                HandleColorFrame(colorFrame);
            }

            using (var depthImageFrame = e.OpenDepthImageFrame())
            {
                HandleDepthFrame(depthImageFrame);
            }
        }

        private void handleFrame(Action<ISensorStreamHandler> action)
        {
            try
            {
                Array.ForEach(this.streamHandlers, action);
            }
            catch (InvalidOperationException)
            {
                // Ignore the frame when sensor gets into a bad state
            }
        }

        public void HandleColorFrame(ColorImageFrame colorFrame)
        {
            if (null != colorFrame)
            {
                handleFrame(handler => handler.ProcessColor(colorFrame.GetRawPixelData(), colorFrame));
            }
        }

        public void HandleDepthFrame(DepthImageFrame depthFrame)
        {
            if (null != depthFrame)
            {
                var depthBuffer = depthFrame.GetRawPixelData();
                handleFrame(handler => handler.ProcessDepth(depthBuffer, depthFrame));
            }
        }

        public void HandleSkeletonFrame(SkeletonFrame skeletonFrame)
        {
            if (null != skeletonFrame)
            {
                skeletonFrame.CopySkeletonDataTo(this.skeletons);
                handleFrame(handler => handler.ProcessSkeleton(skeletons, skeletonFrame));
            }
        }

        #endregion
    }
}
