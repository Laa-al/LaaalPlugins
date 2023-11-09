using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ZipModUtilities.Data;
using ZipModUtilities.ModDownload;
using static System.Text.Encoding;

ModConfig config = ModConfig.Create(SharedConsts.ConfigName);


ModManager manager = new();
manager.SearchLocalPath(SharedConsts.TargetPath, SharedConsts.LocalConfigName);
manager.ReadRemoteConfig(SharedConsts.RemoteConfigName);
manager.ReadIgnoreConfig(SharedConsts.IgnoreConfigName);
DownloadManager downloadManager = new(manager);
List<DownloadManager.Downloader> downloaderList = new();
for (int i = 0; i < config.ThreadCount; i++)
{
    downloaderList.Add(new DownloadManager.Downloader(downloadManager));
}

foreach (ModMessage message in manager.GetRemoteList())
{
    if (message.FileSize == 0 && manager.ModIsNeed(message))
    {
        if (config.UpdateMod || message.Guid is null)
        {
            AddTask(message);
        }
    }
}

if (config.UpdateMod)
{
    foreach (List<ModMessage> list in manager.GetLocalList())
    {
        ModMessage local = list.FirstOrDefault(u => u.IsLocal);
        ModMessage remote = list.MaxBy(u => u.GetVersion());

        if (!remote.IsLocal)
        {
            if (local is null || local.GetVersion() < remote.GetVersion())
            {
                if (manager.ModIsNeed(remote))
                {
                    AddTask(remote);
                }
            }
        }
    }
}

while (Console.ReadKey().Key != ConsoleKey.Escape)
{
    manager.WriteRemoteConfig(SharedConsts.RemoteConfigName);
    manager.WriteLocalConfig(SharedConsts.LocalConfigName);
    ConsoleColor.Magenta.WriteLine("[Config Write]");
}

manager.WriteRemoteConfig(SharedConsts.RemoteConfigName);
manager.WriteLocalConfig(SharedConsts.LocalConfigName);
ConsoleColor.Magenta.WriteLine("[Config Write]");

foreach (DownloadManager.Downloader downloader in downloaderList)
{
    downloader.Dispose();
}

void AddTask(ModMessage message)
{
    DownloadTask task = new()
    {
        Name = new Guid(MD5.HashData(
            UTF8.GetBytes(message.PathOrUri))).ToString(),
        Path = SharedConsts.DownloadPath,
        Uri = message.PathOrUri
    };
    downloadManager.StartTask(task);
}