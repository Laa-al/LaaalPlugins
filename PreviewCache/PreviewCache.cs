using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using ICSharpCode.SharpZipLib.Zip;
using Sideloader;
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
        public const string Version = "0.0.3";

        public const string CachePath = "../temp/preview-cache";

        private static readonly Dictionary<string, Texture2D> Texture2Ds = new Dictionary<string, Texture2D>();

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
                            Texture2Ds[reader.ReadString()] = null;
                        }
                    }
                }
                
                filepath = Path.Combine(Application.dataPath, CachePath, "preview.cache");

                if (File.Exists(filepath))
                {
                    using (FileStream stream = File.OpenRead(filepath))
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int count = reader.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            string path = reader.ReadString();
                            TextureFormat format = (TextureFormat)reader.ReadByte();
                            bool mipmap = reader.ReadBoolean();
                            Texture2Ds[path] = GetTextureFromZipmod(path,format,mipmap);
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
            
            using (FileStream emptyStream = File.OpenWrite(Path.Combine(directory, "preview_empty.cache")))
            using (BinaryWriter emptyWriter = new BinaryWriter(emptyStream)) 
            using (FileStream textureStream = File.OpenWrite(Path.Combine(directory, "preview.cache")))
            using (BinaryWriter textureWriter = new BinaryWriter(textureStream))
            {
                int emptyCount = 0;
                int textureCount = 0;
                emptyWriter.Write(emptyCount);
                textureWriter.Write(textureCount);
                
                foreach ((string path,Texture2D texture) in Texture2Ds)
                {
                    if (texture == null)
                    {
                        emptyWriter.Write(path);
                        emptyCount++;
                    }
                    else
                    {
                        textureWriter.Write(path);
                        textureWriter.Write((byte)texture.format);
                        textureWriter.Write(texture.mipmapCount<0);
                        textureCount++;
                    }
                }

                emptyStream.Seek(0, SeekOrigin.Begin);
                textureWriter.Seek(0, SeekOrigin.Begin);
                emptyWriter.Write(emptyCount);
                textureWriter.Write(textureCount);
            }
        }
        
        public static Texture2D GetTextureFromZipmod(string pngPath, TextureFormat format, bool mipmap)
        {
            if (!Sideloader.Sideloader.PngList.TryGetValue(pngPath, out ZipmodInfo zipmodInfo))
            {
                return null;
            }

            ZipFile zipFile = zipmodInfo.GetZipFile();
            ZipEntry entry = zipFile.GetEntry(pngPath);

            if (entry == null || entry.Size == 0)
            {
                return null;
            }

            using (Stream stream = zipFile.GetInputStream(entry))
            {
                byte[] buffer = new byte[entry.Size];
                int _ = stream.Read(buffer, 0, (int)entry.Size);

                Texture2D tex = new Texture2D(2, 2, format, mipmap);

                tex.LoadImage(buffer);

                if (pngPath.Contains("clamp"))
                    tex.wrapMode = TextureWrapMode.Clamp;
                else if (pngPath.Contains("repeat"))
                    tex.wrapMode = TextureWrapMode.Repeat;

                Texture2Ds[pngPath] = tex;
                return tex;
            }
        }
    }
}