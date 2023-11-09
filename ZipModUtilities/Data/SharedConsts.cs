using System;
using System.IO;

namespace ZipModUtilities.Data
{
    public static class SharedConsts
    {
        public const string RemoteConfigName = "remote.json";
        public const string LocalConfigName = "local.json";
        public const string IgnoreConfigName = "ignore.json";
        public const string ConfigName = "config.json";
        
        public static string DownloadPath { get; set; } = @"D:\DownloadTmp";
        public static string RepeatPath { get; set; } = @"D:\DownloadTmp\Repeat";
        public static string NoNeedPath { get; set; } = @"D:\DownloadTmp\NoNeed";
        public static string TargetPath { get; set; } = @"D:\mods";

        public static string GetRepeatPath(ModMessage message)
        {
            if (!Directory.Exists(RepeatPath))
            {
                Directory.CreateDirectory(RepeatPath);
            }

            return Path.Combine(RepeatPath, DateTime.Now.Ticks % 65536 + message.FileName);
        }

        public static string GetNoNeedPath(ModMessage message)
        {
            if (!Directory.Exists(NoNeedPath))
            {
                Directory.CreateDirectory(NoNeedPath);
            }

            return Path.Combine(NoNeedPath, DateTime.Now.Ticks % 65536 + message.FileName);
        }
    }
}