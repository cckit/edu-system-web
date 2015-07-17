using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;

namespace KinectDataSourceServer.Sensor.Interaction
{
    public interface IUserStateManager
    {
        event EventHandler<UserStateChangedEventArgs> UserStateChanged;

        IDictionary<int, string> UserStates { get; }

        int PrimaryUserTrackingId { get; }

        void ChooseTrackedUsers(Skeleton[] frameSkeletons, long timestamp, int[] chosenTrackingIds);

        void UpdateUserInformation(IEnumerable<UserInfo> trackedUserInfo, long timestamp);

        void Reset();
    }
}
