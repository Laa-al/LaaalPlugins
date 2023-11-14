using System;
using System.IO;
using Newtonsoft.Json;
using RemoteSearch;
using ZipModUtilities.Data;

ModManager manager = new();
manager.ReadRemoteConfig(SharedConsts.RemoteConfigName);
RemoteAnalyzer analyzer = new(manager);
string configName = "sources.json";
if (!File.Exists(configName))
{
    ConsoleColor.Red.WriteLine("[Error] Cannot find sources.json");
    Console.ReadKey();
    return;
}

string configStr = File.ReadAllText(configName);

string[] urls = JsonConvert.DeserializeObject<string[]>(configStr);

await analyzer.AnalyzeSourcesAsync(urls);

manager.WriteRemoteConfig(SharedConsts.RemoteConfigName);

Console.WriteLine("Successfully check update!");

Console.ReadKey();