using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ZipModUtilities.Data
{
    public class ModManager
    {
        private IgnoreConfig _ic;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Uri, ModMessage>> _localMods = new();
        private readonly ConcurrentDictionary<Uri, ModMessage> _remoteMods = new();

        public void SearchLocalPath(string path, string configPath = "")
        {
            int num = 0;
            Dictionary<string, ModMessage> messages = new();
            if (File.Exists(configPath))
            {
                string config = File.ReadAllText(configPath);

                List<ModMessage> list = JsonConvert.DeserializeObject<List<ModMessage>>(config);

                foreach (ModMessage message in list.Where(message => message.IsLocal))
                {
                    if (message.PathOrUri is not null)
                    {
                        messages[message.PathOrUri] = message;
                    }
                }
            }

            foreach (ModMessage message in
                     from list
                         in _localMods.Values
                     from message in list.Values
                     where message.IsLocal
                     select message)
            {
                messages[message.PathOrUri] = message;
            }

            SearchLocalPathInnerFunction(path);

            ConsoleColor.Green.WriteLine("\r\n[Success] File search finished! ");

            return;

            void SearchLocalPathInnerFunction(string d)
            {
                string[] files = Directory.GetFiles(d);

                foreach (string file in files)
                {
                    if (file.EndsWith(".zip") || file.EndsWith(".zipmod"))
                    {
                        if (messages.TryGetValue(file, out ModMessage message))
                        {
                            AddLocalMessage(message);
                        }
                        else
                        {
                            message = new ModMessage()
                            {
                                IsLocal = true,
                                PathOrUri = file
                            };
                            message.FitMessage(file);
                            AddLocalMessage(message);
                        }

                        Console.Write("\r[Search file] searched file count: " + num++);
                    }
                }

                string[] directories = Directory.GetDirectories(d);
                foreach (string directory in directories)
                {
                    SearchLocalPathInnerFunction(directory);
                }
            }
        }

        public void WriteLocalConfig(string configName)
        {
            List<ModMessage> list = _localMods
                .SelectMany(u => u.Value)
                .Select(u => u.Value).ToList();

            string config = JsonConvert.SerializeObject(list);
            File.WriteAllText(configName, config);
        }

        public void ReadRemoteConfig(string configName)
        {
            if (File.Exists(configName))
            {
                string config = File.ReadAllText(configName);

                List<ModMessage> mods = JsonConvert.DeserializeObject<List<ModMessage>>(config);

                foreach (ModMessage modMessage in mods)
                {
                    _remoteMods[new Uri(modMessage.PathOrUri)] = modMessage;
                }
            }
        }

        public void WriteRemoteConfig(string configName)
        {
            List<ModMessage> list = _remoteMods.Values.ToList();
            string config = JsonConvert.SerializeObject(list);
            File.WriteAllText(configName, config);
        }

        public void ReadIgnoreConfig(string configName)
        {
            if (File.Exists(configName))
            {
                string config = File.ReadAllText(configName);

                _ic = JsonConvert.DeserializeObject<IgnoreConfig>(config);
            }
        }

        public bool ModIsNeed(ModMessage message)
        {
            if (_ic is null)
            {
                return true;
            }

            if (_ic.FavoriteAuthors.Contains(message.Author) ||
                _ic.FavoriteAuthors.Contains(message.DirectoryName))
            {
                return true;
            }

            if (message.PathOrUri is not null)
            {
                for (int i = 0; i < _ic.FavoriteKeys.Length; i++)
                {
                    string key = _ic.FavoriteKeys[i];
                    if (message.PathOrUri.Contains(key))
                    {
                        return true;
                    }
                }

                for (int i = 0; i < _ic.IgnoredKeys.Length; i++)
                {
                    string key = _ic.IgnoredKeys[i];
                    if (message.PathOrUri.Contains(key))
                    {
                        return false;
                    }
                }
            }
            
            if (message.Game == "AI Girl")
            {
                return false;
            }
            
            if (_ic.IgnoredAuthors.Contains(message.Author) ||
                _ic.IgnoredAuthors.Contains(message.DirectoryName))
            {
                return false;
            }

            return true;
        }

        public void AddLocalMessage(ModMessage message)
        {
            if (string.IsNullOrEmpty(message.Guid))
                return;

            if (!_localMods.TryGetValue(message.Guid, out var dict))
            {
                dict = new();
                _localMods[message.Guid] = dict;
            }

            Uri uri = new(message.PathOrUri);
            dict[uri] = message;
        }

        public List<ModMessage> GetRemoteList()
        {
            return _remoteMods.Values.ToList();
        }

        public List<List<ModMessage>> GetLocalList()
        {
            Dictionary<string, List<ModMessage>> messages = _localMods.ToDictionary(
                u => u.Key, 
                u => u.Value.Values.ToList());

            foreach ((Uri uri,ModMessage message) in _remoteMods)
            {
                if (!string .IsNullOrEmpty(message.Guid))
                {
                    if (!messages.TryGetValue(message.Guid, out List<ModMessage> list))
                    {
                        list = new List<ModMessage>();
                        messages[message.Guid] = list;
                    }
                    list.Add(message);
                }
            }

            return messages.Values.ToList();
        }

        public ModMessage GetOrCreateRemoteMessage(string uriStr, DateTime updateTime)
        {
            Uri uri = new(uriStr);
            if (!_remoteMods.TryGetValue(uri, out ModMessage message))
            {
                message = new ModMessage
                {
                    IsLocal = false
                };
                _remoteMods[uri] = message;
            }

            message.SetRemoteUri(uriStr, updateTime);

            return message;
        }

        public ModMessage GetRemoteMessage(string uriStr)
        {
            Uri uri = new(uriStr);
            _remoteMods.TryGetValue(uri, out ModMessage message);
            return message;
        }

        public ModMessage AnalyseLocalFile(string filePath)
        {
            ModMessage message = new()
            {
                PathOrUri = filePath,
                IsLocal = true,
            };

            message.FitMessage(filePath);

            if (!ModIsNeed(message))
            {
                string noNeedPath = SharedConsts.GetNoNeedPath(message);

                File.Move(filePath, noNeedPath);

                return message;
            }

            string targetPath = Path.Combine(
                SharedConsts.TargetPath,
                message.DirectoryName,
                message.FileName);

            if (File.Exists(targetPath))
            {
                ModMessage targetMessage = new()
                {
                    PathOrUri = targetPath,
                    IsLocal = true
                };
                targetMessage.FitMessage(targetPath);

                string repeatPath = SharedConsts.GetRepeatPath(message);

                if (targetMessage.VersionObj >= message.VersionObj)
                {
                    File.Move(filePath, repeatPath);
                    AddLocalMessage(targetMessage);

                    return targetMessage;
                }
                else
                {
                    File.Move(targetPath, repeatPath);
                    File.Move(filePath, targetPath);
                    AddLocalMessage(message);
                }
            }
            else
            {
                message.MoveTo(SharedConsts.TargetPath);
                AddLocalMessage(message);
            }

            return message;
        }

        public void UpdateRemoteMessage()
        {
            List<ModMessage> messages = _remoteMods.Values.ToList();
            foreach (ModMessage remoteMessage in messages)
            {
                string guid = remoteMessage.Guid;
                if (guid is not null)
                {
                    remoteMessage.Guid = guid.Trim();
                    if (
                        _localMods.TryGetValue(remoteMessage.Guid,out ConcurrentDictionary<Uri, ModMessage> dict))
                    {
                        ModMessage localMessage = dict.Values.FirstOrDefault(u => u.Guid == remoteMessage.Guid);

                        if (localMessage is not null)
                        {
                            localMessage.Guid = localMessage.Guid.Trim();
                            localMessage?.CopyTo(remoteMessage);
                        }
                    }
                }
            }
        }

        public class IgnoreConfig
        {
            public HashSet<string> IgnoredAuthors { get; set; }
            public string[] IgnoredKeys { get; set; }
            public HashSet<string> FavoriteAuthors { get; set; }
            public string[] FavoriteKeys { get; set; }
        }
    }
}