using System.Configuration;

namespace DataSourceServer
{
    public class Server
    {
        private const string DEFAULT_CONFIG_FILE_PATH = "server.config";
        private const string CONFIG_KEY_WEB_SERVER_URL = "webServerUrl";

        private Configuration config;
        public SocketIOHandler SocketIOHandler { get; private set; }

        public Server()
        {
            config = LoadConfigFile();
        }

        public Server(string configFilePath)
        {
            config = LoadConfigFile(configFilePath);
        }

        private Configuration LoadConfigFile(string filePath = DEFAULT_CONFIG_FILE_PATH)
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = DEFAULT_CONFIG_FILE_PATH;
            return ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        }

        private void StartSocketIO()
        {
            var webServerUrlPair = config.AppSettings.Settings[CONFIG_KEY_WEB_SERVER_URL];
            if (webServerUrlPair != null)
            {
                SocketIOHandler = new SocketIOHandler(webServerUrlPair.Value);
            }
        }

        private void WaitForever()
        {
            while (true)
            {
                System.Console.ReadLine();
            }
        }

        public void Start()
        {
            StartSocketIO();
            WaitForever();
        }

        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                new Server(args[0]).Start();
            }
            else
            {
                new Server().Start();
            }
        }
    }
}
