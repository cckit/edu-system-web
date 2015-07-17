using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using DataSourceServer.Message;
using DataSourceServer.Message.Stream;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;

namespace KinectDataSourceServer.Sensor.Interaction
{
    public class InteractionStreamHandler : SensorStreamHandlerBase, IInteractionClient
    {
        internal const string InteractionStreamName = "interaction";
        internal const string InteractionPrimaryUserPropertyName = "primaryUser";
        internal const string InteractionUserStatesPropertyName = "userStates";

        internal const string UserViewerStreamName = "userviewer";
        internal const string UserViewerResolutionPropertyName = "resolution";
        internal const int UserViewerDefaultWidth = 128;
        internal const int UserViewerDefaultHeight = 96;

        internal const string UserViewerDefaultUserColorPropertyName = "defaultUserColor";
        internal const string UserViewerUserColorsPropertyName = "userColors";

        internal const string ClientUriSubpath = "CLIENT";

        internal static readonly Color UserViewerDefaultDefaultUserColor = new Color { R = 0xd3, G = 0xd3, B = 0xd3, A = 0xff };
        internal static readonly Color UserViewerDefaultTrackedUserColor = new Color { R = 0x00, G = 0xbc, B = 0xf2, A = 0xff };
        internal static readonly Color UserViewerDefaultEngagedUserColor = new Color { R = 0x51, G = 0x1c, B = 0x74, A = 0xff };

        private static readonly Regex UserViewerResolutionRegex = new Regex(@"^(?i)(\d+)x(\d+)$");

        private static readonly Size[] UserViewerSupportedResolutions =
        {
            new Size(640, 480), new Size(320, 240), new Size(160, 120),
            new Size(128, 96), new Size(80, 60)
        };

        private readonly InteractionStreamMessage interactionStreamMessage = new InteractionStreamMessage { stream = InteractionStreamName };
        private readonly ImageHeaderStreamMessage userViewerStreamMessage = new ImageHeaderStreamMessage { stream = UserViewerStreamName };

        private readonly int[] recommendedUserTrackingIds = new int[2];
        private readonly Dictionary<string, int> userViewerUserColors = new Dictionary<string, int>();
        private readonly UserViewerColorizer userViewerColorizer = new UserViewerColorizer(UserViewerDefaultWidth, UserViewerDefaultHeight);
        private readonly IUserStateManager userStateManager = new DefaultUserStateManager();

        private KinectSensor sensor;
        private InteractionStream interactionStream;
        private UserInfo[] userInfos;

        private bool interactionIsEnabled;
        private bool userViewerIsEnabled;
        private int userViewerDefaultUserColor;
        private bool isProcessingInteractionFrame;
        private bool isProcessingUserViewerImage;

        private bool ShouldProcessInteractionData
        {
            get { return (this.userInfos != null) && (this.interactionIsEnabled || this.userViewerIsEnabled); }
        }

        internal InteractionStreamHandler(SensorStreamHandlerContext context)
            : base(context)
        {
            this.userViewerDefaultUserColor = GetRgbaColorInt(UserViewerDefaultDefaultUserColor);
            this.userViewerUserColors[DefaultUserStateManager.TrackedStateName] = GetRgbaColorInt(UserViewerDefaultTrackedUserColor);
            this.userViewerUserColors[DefaultUserStateManager.EngagedStateName] = GetRgbaColorInt(UserViewerDefaultEngagedUserColor);

            this.userStateManager.UserStateChanged += this.OnUserStateChanged;

            this.AddStreamConfiguration(InteractionStreamName, new StreamConfiguration(this.GetInteractionStreamProperties, this.SetInteractionStreamProperty));
            this.AddStreamConfiguration(UserViewerStreamName, new StreamConfiguration(this.GetUserViewerStreamProperties, this.SetUserViewerStreamProperty));
        }

        public override void OnSensorChanged(KinectSensor newSensor)
        {
            if (this.sensor != null)
            {
                try
                {
                    this.interactionStream.InteractionFrameReady -= this.InteractionFrameReadyAsync;
                    this.interactionStream.Dispose();
                    this.interactionStream = null;

                    this.sensor.SkeletonStream.AppChoosesSkeletons = false;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }

                this.userInfos = null;
            }

            this.sensor = newSensor;

            if (newSensor != null)
            {
                try
                {
                    this.interactionStream = new InteractionStream(newSensor, this);
                    this.interactionStream.InteractionFrameReady += this.InteractionFrameReadyAsync;

                    this.sensor.SkeletonStream.AppChoosesSkeletons = true;

                    this.userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            this.userStateManager.Reset();
            this.userViewerColorizer.ResetColorLookupTable();
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

            if (this.ShouldProcessInteractionData)
            {
                this.interactionStream.ProcessDepth(depthData, depthFrame.Timestamp);
            }

            this.ProcessUserViewerImageAsync(depthData, depthFrame);
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

            if (this.ShouldProcessInteractionData)
            {
                this.interactionStream.ProcessSkeleton(skeletons, this.sensor.AccelerometerGetCurrentReading(), skeletonFrame.Timestamp);
            }

            this.userStateManager.ChooseTrackedUsers(skeletons, skeletonFrame.Timestamp, this.recommendedUserTrackingIds);

            try
            {
                this.sensor.SkeletonStream.ChooseSkeletons(
                    this.recommendedUserTrackingIds[0], this.recommendedUserTrackingIds[1]);
            }
            catch (InvalidOperationException)
            {
                // KinectSensor might enter an invalid state while choosing skeletons.
                // E.g.: sensor might be abruptly unplugged.
            }
        }

        internal async void InteractionFrameReadyAsync(object sender, InteractionFrameReadyEventArgs e)
        {
            if (!this.ShouldProcessInteractionData)
            {
                return;
            }

            if (this.isProcessingInteractionFrame)
            {
                // Re-entered InteractionFrameReadyAsync while a previous frame is already being processed.
                // Just ignore new frames until the current one finishes processing.
                return;
            }

            this.isProcessingInteractionFrame = true;

            try
            {
                bool haveFrameData = false;

                using (var interactionFrame = e.OpenInteractionFrame())
                {
                    // Even though we checked value of userInfos above as part of
                    // ShouldProcessInteractionData check, callbacks happening while
                    // opening an interaction frame might have invalidated it, so we
                    // check value again. 
                    if ((interactionFrame != null) && (this.userInfos != null))
                    {
                        // Copy interaction frame data so we can dispose interaction frame
                        // right away, even if data processing/event handling takes a while.
                        interactionFrame.CopyInteractionDataTo(this.userInfos);
                        this.interactionStreamMessage.timestamp = interactionFrame.Timestamp;
                        haveFrameData = true;
                    }
                }

                if (haveFrameData)
                {
                    this.userStateManager.UpdateUserInformation(this.userInfos, this.interactionStreamMessage.timestamp);
                    this.userViewerColorizer.UpdateColorLookupTable(this.userInfos, this.userViewerDefaultUserColor, this.userStateManager.UserStates, this.userViewerUserColors);

                    if (this.interactionIsEnabled)
                    {
                        this.UpdateHandPointers(this.userInfos, this.userStateManager.PrimaryUserTrackingId);
                        await this.Context.SendStreamMessageAsync(this.interactionStreamMessage);
                    }
                }
            }
            finally
            {
                this.isProcessingInteractionFrame = false;
            }
        }

        private async void OnUserStateChanged(object sender, UserStateChangedEventArgs e)
        {
            if (this.interactionIsEnabled)
            {
                // If enabled, forward all user state events to client
                await this.Context.SendEventMessageAsync(e.Message);
            }
        }

        private async void ProcessUserViewerImageAsync(DepthImagePixel[] depthData, DepthImageFrame depthFrame)
        {
            if (this.userViewerIsEnabled)
            {
                if (this.isProcessingUserViewerImage)
                {
                    // Re-entered ProcessUserViewerImageAsync while a previous image is already being processed.
                    // Just ignore new depth frames until the current one finishes processing.
                    return;
                }

                this.isProcessingUserViewerImage = true;

                try
                {
                    this.userViewerColorizer.ColorizeDepthPixels(depthData, depthFrame.Width, depthFrame.Height);
                    this.userViewerStreamMessage.timestamp = depthFrame.Timestamp;
                    this.userViewerStreamMessage.width = this.userViewerColorizer.Width;
                    this.userViewerStreamMessage.height = this.userViewerColorizer.Height;
                    this.userViewerStreamMessage.bufferLength = this.userViewerColorizer.Buffer.Length;

                    await this.Context.SendStreamMessageWithDataAsync(this.userViewerStreamMessage, this.userViewerColorizer.Buffer);
                }
                finally
                {
                    this.isProcessingUserViewerImage = false;
                }
            }
        }

        public void UpdateHandPointers(IEnumerable<UserInfo> userInfoData, int primaryUserTrackingId)
        {
            int handPointerIndex = 0;

            if (userInfoData == null)
            {
                throw new ArgumentNullException("userInfoData");
            }

            foreach (var user in userInfoData)
            {
                foreach (var handPointer in user.HandPointers)
                {
                    if (user.SkeletonTrackingId == SharedConstants.InvalidUserTrackingId)
                    {
                        continue;
                    }

                    var messageHandPointer = this.interactionStreamMessage.internalHandPointers[handPointerIndex];
                    messageHandPointer.trackingId = user.SkeletonTrackingId;
                    messageHandPointer.handType = handPointer.HandType.ToString();
                    messageHandPointer.isTracked = handPointer.IsTracked;
                    messageHandPointer.isActive = handPointer.IsActive;
                    messageHandPointer.isInteractive = handPointer.IsInteractive;
                    messageHandPointer.isPressed = handPointer.IsPressed;
                    messageHandPointer.isPrimaryHandOfUser = handPointer.IsPrimaryForUser;
                    messageHandPointer.isPrimaryUser = primaryUserTrackingId == user.SkeletonTrackingId;
                    messageHandPointer.handEventType = handPointer.HandEventType.ToString();
                    messageHandPointer.x = handPointer.X;
                    messageHandPointer.y = handPointer.Y;
                    messageHandPointer.pressExtent = handPointer.PressExtent;
                    messageHandPointer.rawX = handPointer.RawX;
                    messageHandPointer.rawY = handPointer.RawY;
                    messageHandPointer.rawZ = handPointer.RawZ;

                    if (++handPointerIndex >= InteractionStreamMessage.MaximumHandPointers)
                    {
                        break;
                    }
                }

                if (handPointerIndex >= InteractionStreamMessage.MaximumHandPointers)
                {
                    break;
                }
            }

            if ((this.interactionStreamMessage.handPointers == null) || (this.interactionStreamMessage.handPointers.Length != handPointerIndex))
            {
                this.interactionStreamMessage.handPointers = new MessageHandPointer[handPointerIndex];
            }

            for (handPointerIndex = 0; handPointerIndex < this.interactionStreamMessage.handPointers.Length; ++handPointerIndex)
            {
                this.interactionStreamMessage.handPointers[handPointerIndex] = this.interactionStreamMessage.internalHandPointers[handPointerIndex];
            }
        }

        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
        {
            var interactionInfo = new InteractionInfo { IsPressTarget = false, IsGripTarget = false };

            //if (this.interactionIsEnabled && (this.clientRpcChannel != null))
            //{
            //    var result = this.clientRpcChannel.CallFunction<InteractionStreamHitTestInfo>("getInteractionInfoAtLocation", skeletonTrackingId, handType.ToString(), x, y);
            //    if (result.Success)
            //    {
            //        interactionInfo.IsGripTarget = result.Result.isGripTarget;
            //        interactionInfo.IsPressTarget = result.Result.isPressTarget;
            //        var elementId = result.Result.pressTargetControlId;
            //        interactionInfo.PressTargetControlId = (elementId != null) ? elementId.GetHashCode() : 0;
            //        interactionInfo.PressAttractionPointX = result.Result.pressAttractionPointX;
            //        interactionInfo.PressAttractionPointY = result.Result.pressAttractionPointY;
            //    }
            //}

            return interactionInfo;
        }

        internal static int GetRgbaColorInt(Color color)
        {
            return (color.A << 24) | (color.B << 16) | (color.G << 8) | color.R;
        }

        internal void GetInteractionStreamProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(EnabledPropertyName, this.interactionIsEnabled);
            propertyMap.Add(InteractionPrimaryUserPropertyName, this.userStateManager.PrimaryUserTrackingId);
            propertyMap.Add(InteractionUserStatesPropertyName, DefaultUserStateManager.GetStateMappingEntryArray(this.userStateManager.UserStates));
        }

        internal string SetInteractionStreamProperty(string propertyName, object propertyValue)
        {
            bool recognized = true;

            if (propertyValue == null)
            {
                // None of the interaction stream properties accept a null value
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            try
            {
                switch (propertyName)
                {
                    case EnabledPropertyName:
                        this.interactionIsEnabled = (bool)propertyValue;
                        break;

                    default:
                        recognized = false;
                        break;
                }

                if (!recognized)
                {
                    return Properties.Resources.PropertyNameUnrecognized;
                }
            }
            catch (InvalidCastException)
            {
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            return null;
        }

        internal void GetUserViewerStreamProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(EnabledPropertyName, this.userViewerIsEnabled);
            propertyMap.Add(UserViewerResolutionPropertyName, string.Format(CultureInfo.InvariantCulture, @"{0}x{1}", this.userViewerColorizer.Width, this.userViewerColorizer.Height));
            propertyMap.Add(UserViewerDefaultUserColorPropertyName, this.userViewerDefaultUserColor);
            propertyMap.Add(UserViewerUserColorsPropertyName, this.userViewerUserColors);
        }

        internal string SetUserViewerStreamProperty(string propertyName, object propertyValue)
        {
            bool recognized = true;

            try
            {
                switch (propertyName)
                {
                    case EnabledPropertyName:
                        this.userViewerIsEnabled = (bool)propertyValue;
                        break;

                    case UserViewerResolutionPropertyName:
                        if (propertyValue == null)
                        {
                            return Properties.Resources.PropertyValueInvalidFormat;
                        }

                        var match = UserViewerResolutionRegex.Match((string)propertyValue);
                        if (!match.Success || (match.Groups.Count != 3))
                        {
                            return Properties.Resources.PropertyValueInvalidFormat;
                        }

                        int width = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        int height = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                        if (!IsSupportedUserViewerResolution(width, height))
                        {
                            return Properties.Resources.PropertyValueUnsupportedResolution;
                        }

                        this.userViewerColorizer.SetResolution(width, height);
                        break;

                    case UserViewerDefaultUserColorPropertyName:
                        this.userViewerDefaultUserColor = (int)propertyValue;

                        this.UpdateColorizerLookupTable();
                        break;

                    case UserViewerUserColorsPropertyName:
                        if (propertyValue == null)
                        {
                            // Null values just clear the set of user colors
                            this.userViewerUserColors.Clear();
                            break;
                        }

                        var userColors = (Dictionary<string, object>)propertyValue;

                        // Verify that all dictionary values are integers
                        bool allIntegers = userColors.Values.Select(color => color as int?).All(colorInt => colorInt != null);
                        if (!allIntegers)
                        {
                            return Properties.Resources.PropertyValueInvalidFormat;
                        }

                        // If property value specified is compatible, copy values over
                        this.userViewerUserColors.Clear();
                        foreach (var entry in userColors)
                        {
                            this.userViewerUserColors.Add(entry.Key, (int)entry.Value);
                        }

                        this.UpdateColorizerLookupTable();
                        break;

                    default:
                        recognized = false;
                        break;
                }

                if (!recognized)
                {
                    return Properties.Resources.PropertyNameUnrecognized;
                }
            }
            catch (InvalidCastException)
            {
                return Properties.Resources.PropertyValueInvalidFormat;
            }
            catch (NullReferenceException)
            {
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            return null;
        }

        private static bool IsSupportedUserViewerResolution(int width, int height)
        {
            return UserViewerSupportedResolutions.Any(resolution => ((int)resolution.Width == width) && ((int)resolution.Height == height));
        }

        private void UpdateColorizerLookupTable()
        {
            if (this.ShouldProcessInteractionData)
            {
                this.userViewerColorizer.UpdateColorLookupTable(
                    this.userInfos, this.userViewerDefaultUserColor, this.userStateManager.UserStates, this.userViewerUserColors);
            }
        }
    }
}
