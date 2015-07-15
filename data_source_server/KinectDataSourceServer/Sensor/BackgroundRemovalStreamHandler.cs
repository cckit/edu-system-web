using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using DataSourceServer.Message;
using DataSourceServer.Message.Stream;
using KinectDataSourceServer.Properties;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.BackgroundRemoval;

namespace KinectDataSourceServer.Sensor
{
    public class BackgroundRemovalStreamHandler : SensorStreamHandlerBase
    {
        internal const string BackgroundRemovalStreamName = "backgroundRemoval";
        internal const string TrackingIdPropertyName = "trackingId";
        internal const string ResolutionPropertyName = "resolution";

        private static readonly Regex BackgroundRemovalResolutionRegex = new Regex(@"^(?i)(\d+)x(\d+)$");

        private static readonly KeyValuePair<ColorImageFormat, Size>[] BackgroundRemovalResolutions =
        {
            new KeyValuePair<ColorImageFormat, Size>(ColorImageFormat.RgbResolution640x480Fps30, new Size(640, 480)),
            new KeyValuePair<ColorImageFormat, Size>(ColorImageFormat.RgbResolution1280x960Fps12, new Size(1280, 960))
        };

        private readonly BackgroundRemovalStreamMessage backgroundRemovalStreamMessage = new BackgroundRemovalStreamMessage { stream = BackgroundRemovalStreamName };

        private KinectSensor sensor;

        private BackgroundRemovedColorStream backgroundRemovalStream;

        private int trackingId;

        private bool backgroundRemovalStreamIsEnabled;

        private ColorImageFormat colorImageFormat = ColorImageFormat.RgbResolution640x480Fps30;

        private bool isProcessingBackgroundRemovedFrame;

        internal BackgroundRemovalStreamHandler(SensorStreamHandlerContext context)
            : base(context)
        {
            this.AddStreamConfiguration(BackgroundRemovalStreamName, new StreamConfiguration(this.GetProperties, this.SetProperty));
        }

        public override void OnSensorChanged(KinectSensor newSensor)
        {
            if (this.sensor != null)
            {
                try
                {
                    this.backgroundRemovalStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyAsync;
                    this.backgroundRemovalStream.Dispose();
                    this.backgroundRemovalStream = null;
                    this.sensor.ColorStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            this.sensor = newSensor;

            if (newSensor != null)
            {
                this.backgroundRemovalStream = new BackgroundRemovedColorStream(newSensor);
                this.backgroundRemovalStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyAsync;

                // Force update
                this.UpdateBackgroundRemovalFrameFormat(this.colorImageFormat, true);
            }
        }

        public override void ProcessColor(byte[] colorData, ColorImageFrame colorFrame)
        {
            if (colorData == null)
            {
                throw new ArgumentNullException("colorData");
            }

            if (colorFrame == null)
            {
                throw new ArgumentNullException("colorFrame");
            }

            if (this.backgroundRemovalStreamIsEnabled)
            {
                this.backgroundRemovalStream.ProcessColor(colorData, colorFrame.Timestamp);
            }
        }

        public override void ProcessDepth(DepthImagePixel[] depthData, DepthImageFrame depthFrame)
        {
            if (depthData == null)
            {
                throw new ArgumentNullException("depthData");
            }

            if (depthFrame == null)
            {
                throw new ArgumentNullException("depthFrame");
            }

            if (this.backgroundRemovalStreamIsEnabled)
            {
                this.backgroundRemovalStream.ProcessDepth(depthData, depthFrame.Timestamp);
            }
        }

        public override void ProcessSkeleton(Skeleton[] skeletons, SkeletonFrame skeletonFrame)
        {
            if (skeletons == null)
            {
                throw new ArgumentNullException("skeletons");
            }

            if (skeletonFrame == null)
            {
                throw new ArgumentNullException("skeletonFrame");
            }

            if (this.backgroundRemovalStreamIsEnabled)
            {
                this.backgroundRemovalStream.ProcessSkeleton(skeletons, skeletonFrame.Timestamp);
            }
        }

        internal async void BackgroundRemovedFrameReadyAsync(object sender, BackgroundRemovedColorFrameReadyEventArgs e)
        {
            if (!this.backgroundRemovalStreamIsEnabled || this.isProcessingBackgroundRemovedFrame)
            {
                return;
            }

            this.isProcessingBackgroundRemovedFrame = true;

            try
            {
                bool haveFrameData = false;

                using (var backgroundRemovedFrame = e.OpenBackgroundRemovedColorFrame())
                {
                    if (backgroundRemovedFrame != null)
                    {
                        this.UpdateBackgroundRemovedColorFrame(this.backgroundRemovalStreamMessage, backgroundRemovedFrame);
                        haveFrameData = true;
                    }
                }

                if (haveFrameData)
                {
                    await this.Context.SendStreamMessageWithDataAsync(this.backgroundRemovalStreamMessage, this.backgroundRemovalStreamMessage.Buffer);
                }
            }
            finally
            {
                this.isProcessingBackgroundRemovedFrame = false;
            }
        }

        private void UpdateBackgroundRemovalFrameFormat(ColorImageFormat format, bool forceEnable)
        {
            bool isNeedChange = (!forceEnable && (format == this.colorImageFormat));
            if (isNeedChange)
            {
                return;
            }

            if (this.sensor != null)
            {
                try
                {
                    this.sensor.ColorStream.Enable(format);
                    this.backgroundRemovalStream.Enable(format, DepthImageFormat.Resolution640x480Fps30);
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            this.colorImageFormat = format;
        }

        private void UpdateBackgroundRemovedColorFrame(BackgroundRemovalStreamMessage message, BackgroundRemovedColorFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame");
            }

            message.timestamp = frame.Timestamp;
            message.width = frame.Width;
            message.height = frame.Height;
            message.bufferLength = frame.PixelDataLength;
            message.trackedPlayerId = frame.TrackedPlayerId;
            message.averageDepth = frame.AverageDepth;

            message.UpdateBuffer(frame.GetRawPixelData());
        }

        private static Size GetColorImageSize(ColorImageFormat format)
        {
            try
            {
                var q = from item in BackgroundRemovalResolutions
                        where item.Key == format
                        select item.Value;

                return q.Single();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(Resources.UnsupportedColorFormat, "format");
            }
        }

        private static ColorImageFormat GetColorImageFormat(int width, int height)
        {
            try
            {
                var q = from item in BackgroundRemovalResolutions
                        where (int)item.Value.Width == width && (int)item.Value.Height == height
                        select item.Key;

                return q.Single();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(Resources.UnsupportedColorFormat);
            }
        }

        private void GetProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(EnabledPropertyName, this.backgroundRemovalStreamIsEnabled);
            propertyMap.Add(TrackingIdPropertyName, this.trackingId);

            var size = GetColorImageSize(this.colorImageFormat);
            propertyMap.Add(ResolutionPropertyName, string.Format(CultureInfo.InvariantCulture, @"{0}x{1}", (int)size.Width, (int)size.Height));
        }

        private string SetProperty(string propertyName, object propertyValue)
        {
            bool recognized = true;

            if (propertyValue == null)
            {
                return Resources.PropertyValueInvalidFormat;
            }

            try
            {
                switch (propertyName)
                {
                    case EnabledPropertyName:
                        this.backgroundRemovalStreamIsEnabled = (bool)propertyValue;
                        break;

                    case TrackingIdPropertyName:
                        var oldTrackingId = this.trackingId;
                        this.trackingId = (int)propertyValue;

                        if (this.trackingId != oldTrackingId)
                        {
                            this.backgroundRemovalStream.SetTrackedPlayer(this.trackingId);
                        }
                        break;

                    case ResolutionPropertyName:
                        var match = BackgroundRemovalResolutionRegex.Match((string)propertyValue);
                        if (!match.Success || (match.Groups.Count != 3))
                        {
                            return Resources.PropertyValueInvalidFormat;
                        }

                        int width = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        int height = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                        try
                        {
                            var format = GetColorImageFormat(width, height);
                            this.UpdateBackgroundRemovalFrameFormat(format, false);
                        }
                        catch (ArgumentException)
                        {
                            return Resources.PropertyValueUnsupportedResolution;
                        }
                        break;

                    default:
                        recognized = false;
                        break;
                }

                if (!recognized)
                {
                    return Resources.PropertyNameUnrecognized;
                }
            }
            catch (InvalidCastException)
            {
                return Resources.PropertyValueInvalidFormat;
            }

            return null;
        }

    }
}
