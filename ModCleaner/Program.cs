using System;
using System.IO;
using ZipModUtilities.Data;

ModConfig config = ModConfig.Create(SharedConsts.ConfigName);

int count = 0;
ModManager manager = new();

manager.ReadIgnoreConfig(SharedConsts.IgnoreConfigName);

AutoClearFiles(config.OriginPath);
return;

void AutoClearFiles(string origin)
{
    string[] files = Directory.GetFiles(origin);
    foreach (string file in files)
    {
        if (file.EndsWith(".zipmod") ||
            file.EndsWith(".zip"))
        {
            try
            {
                manager.AnalyseLocalFile(file);
                Console.Write($"\r[Write] {++count} files");
            }
            catch (Exception e)
            {
                Console.WriteLine(file);
                Console.WriteLine(e);
            }
        }
    }

    string[] directories = Directory.GetDirectories(origin);

    foreach (string directory in directories)
    {
        AutoClearFiles(directory);
    }
}