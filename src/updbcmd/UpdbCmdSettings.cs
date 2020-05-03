namespace updbcmd
{
    internal sealed class UpdbCmdSettings
    {
        public string LogFolderPath { get; private set; }
        public string LogFileName { get; private set; }
        public int NumOfWorkers { get; private set; }

        public UpdbCmdSettings()
        {
            LogFolderPath = @"D:\Temp\updb\log";
            LogFileName = "updbcmd.txt";
            NumOfWorkers = 6;
        }
    }
}
