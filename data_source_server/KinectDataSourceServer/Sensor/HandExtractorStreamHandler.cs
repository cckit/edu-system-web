using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CURELab.SignLanguage.HandDetector;
using DataSourceServer.Message;
using DataSourceServer.Message.Stream;
using KinectDataSourceServer.Properties;
using Microsoft.Kinect;

namespace KinectDataSourceServer.Sensor
{
    public class HandExtractorStreamHandler : SensorStreamHandlerBase
    {
        internal const string HandExtractorStreamName = "handExtractor";
        internal const string ResolutionPropertyName = "resolution";

        private static readonly Regex BackgroundRemovalResolutionRegex = new Regex(@"^(?i)(\d+)x(\d+)$");

        private HandExtractor handExtractor;
        private readonly HandExtractorStreamMessage handExtractorStreamMessage;

        private bool isHandExtractorEnabled;
        private bool isProcessingHandExtractorFrame;
        private bool hasSkeletonFrame;
        private bool hasColorFrame;
        private DepthImageFormat depthImageFormat;

        private static readonly KeyValuePair<DepthImageFormat, Size>[] HandExtractorResolutions =
        {
            new KeyValuePair<DepthImageFormat, Size>(DepthImageFormat.Resolution640x480Fps30, new Size(640, 480)),
            new KeyValuePair<DepthImageFormat, Size>(DepthImageFormat.Resolution320x240Fps30, new Size(320, 240)),
            new KeyValuePair<DepthImageFormat, Size>(DepthImageFormat.Resolution80x60Fps30, new Size(80, 60))
        };

        internal HandExtractorStreamHandler(SensorStreamHandlerContext context)
            : base(context)
        {
            this.AddStreamConfiguration(HandExtractorStreamName, new StreamConfiguration(this.GetProperties, this.SetProperties));
            this.handExtractor = HandExtractor.GetSingletonInstance();
            this.handExtractorStreamMessage = new HandExtractorStreamMessage
            {
                stream = HandExtractorStreamName,
                format = "png"
            };
            this.depthImageFormat = DepthImageFormat.Resolution640x480Fps30;
        }

        public override void OnSensorChanged(KinectSensor newSensor)
        {
            if (newSensor != null)
            {
                handExtractor.Initialize(newSensor);
            }
        }

        public override void ProcessSkeleton(Skeleton[] skeletons, SkeletonFrame skeletonFrame)
        {
            handExtractor.OnSkeletonFrameUpdated(skeletons);
            hasSkeletonFrame = true;
        }

        public override void ProcessColor(byte[] colorData, ColorImageFrame colorFrame)
        {
            handExtractor.OnColorFrameUpdated(colorData);
            hasColorFrame = true;
        }

        public override void ProcessDepth(DepthImagePixel[] depthData, DepthImageFrame depthFrame)
        {
            if (!this.isHandExtractorEnabled || this.isProcessingHandExtractorFrame || !this.hasSkeletonFrame || !this.hasColorFrame)
            {
                this.hasSkeletonFrame = false;
                this.hasColorFrame = false;
                return;
            }

            this.isProcessingHandExtractorFrame = true;
            try
            {
                this.depthImageFormat = depthFrame.Format;

                byte[] processImg;
                HandShapeModel model = handExtractor.OnDepthFrameUpdated(depthFrame, out processImg);

                if (processImg != null)
                {
                    this.UpdateHandExtractorStreamMessage(handExtractorStreamMessage, model, processImg, depthFrame);
                    this.Context.SendStreamMessageWithDataAsync(this.handExtractorStreamMessage, this.handExtractorStreamMessage.GetCompressedImage());
                }
            }
            finally
            {
                this.isProcessingHandExtractorFrame = false;
                this.hasSkeletonFrame = false;
                this.hasColorFrame = false;
            }
        }

        private void UpdateHandExtractorStreamMessage(HandExtractorStreamMessage message, HandShapeModel model, byte[] processImg, DepthImageFrame depthFrame)
        {
            message.timestamp = model.frame;
            message.UpdateBuffer(processImg, depthFrame.Width, depthFrame.Height);
        }

        private void UpdateHandExtractorFrameFormat(DepthImageFormat format, bool forceEnable)
        {
            bool isNeedChange = (!forceEnable && (format == this.depthImageFormat));
            if (isNeedChange)
            {
                return;
            }

            this.depthImageFormat = format;
        }

        private static Size GetDepthImageSize(DepthImageFormat format)
        {
            try
            {
                var q = from item in HandExtractorResolutions
                        where item.Key == format
                        select item.Value;

                return q.Single();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(Resources.UnsupportedColorFormat, "format");
            }
        }

        private static DepthImageFormat GetDepthImageFormat(int width, int height)
        {
            try
            {
                var q = from item in HandExtractorResolutions
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
            propertyMap.Add(EnabledPropertyName, this.isHandExtractorEnabled);

            var size = GetDepthImageSize(this.depthImageFormat);
            propertyMap.Add(ResolutionPropertyName, string.Format(CultureInfo.InvariantCulture, @"{0}x{1}", (int)size.Width, (int)size.Height));
        }

        private string SetProperties(string propertyName, object propertyValue)
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
                        this.isHandExtractorEnabled = (bool)propertyValue;
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
                            var format = GetDepthImageFormat(width, height);
                            this.UpdateHandExtractorFrameFormat(format, false);
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
