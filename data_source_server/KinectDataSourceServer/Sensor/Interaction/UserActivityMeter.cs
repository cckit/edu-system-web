using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectDataSourceServer.Sensor.Interaction
{
    public class UserActivityMeter
    {
        private readonly Dictionary<int, UserActivityRecord> activityRecords = new Dictionary<int, UserActivityRecord>();
        private int totalUpdatesSoFar;

        public void Clear()
        {
            this.activityRecords.Clear();
        }

        public bool TryGetActivityRecord(int userTackingId, out UserActivityRecord record)
        {
            return this.activityRecords.TryGetValue(userTackingId, out record);
        }

        public void Update(ICollection<Skeleton> skeletons, long timestamp)
        {
            foreach (var skeleton in skeletons)
            {
                UserActivityRecord record;

                if (this.activityRecords.TryGetValue(skeleton.TrackingId, out record))
                {
                    record.Update(skeleton.Position, this.totalUpdatesSoFar, timestamp);
                }
                else
                {
                    record = new UserActivityRecord(skeleton.Position, this.totalUpdatesSoFar, timestamp);
                    this.activityRecords[skeleton.TrackingId] = record;
                }
            }

            var idsToRemove = new List<int>();
            foreach (var record in this.activityRecords)
            {
                if (record.Value.LastUpdateId != this.totalUpdatesSoFar)
                {
                    idsToRemove.Add(record.Key);
                }
            }

            foreach (var id in idsToRemove)
            {
                this.activityRecords.Remove(id);
            }

            ++this.totalUpdatesSoFar;
        }
    }
}
