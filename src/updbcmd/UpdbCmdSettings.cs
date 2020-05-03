using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace updbcmd
{
    internal sealed class UpdbCmdSettings
    {
        public string LogFolderPath { get; private set; }
        public string LogFileName { get; private set; }
        public int NumOfWorkers { get; private set; }

        public static UpdbCmdSettings Load(string settingFilePath)
        {
            var settings = JsonConvert.DeserializeObject<SettingsFileSchema>(File.ReadAllText(settingFilePath, Encoding.UTF8));
            return new UpdbCmdSettings()
            { 
                LogFolderPath = settings.LogFolderPath,
                LogFileName = settings.LogFileName,
                NumOfWorkers = settings.NumOfWorkers,
            };
        }

        private UpdbCmdSettings()
        { }

        internal class SettingsFileSchema
        {
            public string LogFolderPath { get; set; }
            public string LogFileName { get; set; }
            public int NumOfWorkers { get; set; }
        }
    }
}
