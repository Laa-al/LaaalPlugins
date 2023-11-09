using System;
using System.IO;
using Newtonsoft.Json;

namespace ZipModUtilities.Data
{
    public class ModConfig
    {
        public int ThreadCount { get; set; }
        public string OriginPath { get; set; }
        public string TargetPath { get; set; }
        public string DownloadPath { get; set; }
        public string RepeatPath { get; set; }
        public string NoNeedPath { get; set; }
        public bool UpdateMod { get; set; }

        public static ModConfig Create(string configName)
        {
            if (File.Exists(configName))
            {
                string configStr = File.ReadAllText(configName);

                ModConfig config = JsonConvert.DeserializeObject<ModConfig>(configStr);

                if (config.DownloadPath is not null)
                {
                    SharedConsts.DownloadPath = config.DownloadPath;
                }
                if (config.RepeatPath is not null)
                {
                    SharedConsts.RepeatPath = config.RepeatPath;
                }
                if (config.NoNeedPath is not null)
                {
                    SharedConsts.NoNeedPath = config.NoNeedPath;
                }
                if (config.TargetPath is not null)
                {
                    SharedConsts.TargetPath = config.TargetPath;
                }

                if (config.ThreadCount < 0)
                {
                    config.ThreadCount = 4;
                }
           
                
                return config;
            }
     
            ConsoleColor.Red.WriteLine("[Error] Cannot read config.");
            Console.ReadKey();
            throw new Exception("[Error] Cannot read config.");
        }
    }
}