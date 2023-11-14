using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;


namespace PreviewCache
{
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class PreviewCache : BaseUnityPlugin
    {
        public const string GUID = "com.laaal.preview_cache";

        /// <summary> Plugin name </summary>
        public const string PluginName = "PreviewCache";

        /// <summary> Plugin version </summary>
        public const string Version = "0.0.2";

        public const string CachePath = "../temp/preview-cache";

        private static readonly Dictionary<string, Texture2D> Texture2Ds = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> EmptyTexPath = new HashSet<string>();

        private void Awake()
        {
            try
            {
                string filepath = Path.Combine(Application.dataPath, CachePath, "preview_empty.cache");

                if (File.Exists(filepath))
                {
                    using (FileStream stream = File.OpenRead(filepath))
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int count = reader.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            EmptyTexPath.Add(reader.ReadString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }


        private void OnDestroy()
        {
            string directory = Path.Combine(Application.dataPath, CachePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string filepath = Path.Combine(directory, "preview_empty.cache");
            using (FileStream stream = File.OpenWrite(filepath))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(EmptyTexPath.Count);

                foreach (string s in EmptyTexPath)
                {
                    writer.Write(s);
                }
            }
        }
    }
}