using System;
using Microsoft.Kinect;

namespace KinectDataSourceServer.Sensor.Interaction
{
    public class UserActivityRecord
    {
        private const double ActivityMetricThreshold = 0.1;
        private double activityLevel;

        public double ActivityLevel
        {
            get { return this.activityLevel; }
            set { this.activityLevel = Math.Max(0.0, Math.Min(1.0, value)); }
        }

        public int LastUpdateId { get; private set; }

        public bool IsActive { get; private set; }

        public long StateTransitionTimestamp { get; private set; }

        public SkeletonPoint LastPosition { get; private set; }

        public UserActivityRecord(SkeletonPoint position, int updateId, long timestamp)
        {
            this.ActivityLevel = 0.0;
            this.LastPosition = position;
            this.LastUpdateId = updateId;
            this.IsActive = false;
            this.StateTransitionTimestamp = timestamp;
        }

        public void Update(SkeletonPoint position, int updateId, long timestamp)
        {
            const double DeltaScalingFactor = 10.0;
            const double ActivityDecay = 0.1;

            var delta = new SkeletonPoint
            {
                X = position.X - this.LastPosition.X,
                Y = position.Y - this.LastPosition.Y,
                Z = position.Z - this.LastPosition.Z
            };

            double deltaLengthSquared = (delta.X * delta.X) + (delta.Y * delta.Y) + (delta.Z * delta.Z);
            double newMetric = DeltaScalingFactor * Math.Sqrt(deltaLengthSquared);

            this.ActivityLevel = ((1.0 - ActivityDecay) * this.ActivityLevel) + (ActivityDecay * newMetric);

            bool newIsActive = this.ActivityLevel >= ActivityMetricThreshold;

            if (newIsActive != this.IsActive)
            {
                this.IsActive = newIsActive;
                this.StateTransitionTimestamp = timestamp;
            }

            this.LastPosition = position;
            this.LastUpdateId = updateId;
        }
    }
}
