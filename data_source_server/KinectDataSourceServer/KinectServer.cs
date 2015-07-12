using System;
using DataSourceServer;
using KinectDataSourceServer.Sensor;
using Microsoft.Kinect.Toolkit;

namespace KinectDataSourceServer
{
    class KinectServer
    {
        private KinectRequestHandler kinectRequestHandler;
        private DataSourceWebSocketServer dataSourceServer;

        private const bool IS_ALL_FRAME_READY = true;

        public KinectServer()
        {
            KinectSensorChooser sensorChooser = new KinectSensorChooser();

            this.kinectRequestHandler = new KinectRequestHandlerFactory(sensorChooser).CreateHandler();
            this.dataSourceServer = new DataSourceWebSocketServer(kinectRequestHandler.KinectRequestRouter, 8181);
        }

        public void Start()
        {
            kinectRequestHandler.Start();
            dataSourceServer.Start();
        }

        public void Stop()
        {
            kinectRequestHandler.Stop();
        }

        static void Main(string[] args)
        {
            KinectServer kinectServer = new KinectServer();
            kinectServer.Start();

            Console.WriteLine("The server started successfully, press key 'q' to stop it!");
            while (Console.ReadKey().KeyChar != 'q')
            {
                Console.WriteLine();
                continue;
            }

            kinectServer.Stop();
        }
    }
}
