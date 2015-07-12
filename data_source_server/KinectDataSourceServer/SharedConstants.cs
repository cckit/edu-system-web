using System.Threading.Tasks;

namespace KinectDataSourceServer
{
    internal static class SharedConstants
    {
        internal const int InvalidUserTrackingId = 0;

        internal const int MaxUsersTracked = 6;

        internal static readonly char[] UriPathComponentDelimiters = new[] { '/', '?' };

        internal static readonly Task EmptyCompletedTask = Task.FromResult(0);
    }
}
