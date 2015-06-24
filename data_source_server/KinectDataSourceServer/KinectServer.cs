using DataSourceServer;

namespace KinectDataSourceServer
{
    class KinectServer
    {
        private Server dataSourceServer;
        private KinectDataDispatcher kinectDataDispatcher;

        private const bool IS_ALL_FRAME_READY = true;

        public KinectServer()
        {
            dataSourceServer = new Server();
            kinectDataDispatcher = new KinectDataDispatcher(dataSourceServer, IS_ALL_FRAME_READY);
        }

        public void Start()
        {
            kinectDataDispatcher.Start();
            dataSourceServer.Start();
        }

        public void Stop()
        {
            kinectDataDispatcher.Stop();
        }

        static void Main(string[] args)
        {
            KinectServer kinectServer = new KinectServer();
            kinectServer.Start();
        }
    }
}
