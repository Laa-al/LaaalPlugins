using System.IO;
using HarmonyLib;
using ICSharpCode.SharpZipLib.Zip;
using Sideloader;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace PreviewCache
{
    public partial class PreviewCache
    {
        public static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(Sideloader.Sideloader), nameof(Sideloader.Sideloader.GetPng))]
            private static bool PreGetPng(
                Sideloader.Sideloader __instance, ref Texture2D __result,
                string pngPath, TextureFormat format, bool mipmap)
            {
                if (string.IsNullOrEmpty(pngPath) || EmptyTexPath.Contains(pngPath))
                {
                    __result = null;
#if DEBUG
                    Debug.Log($"[{PluginName}] Null for {pngPath}");
#endif
                }
                else if (Texture2Ds.TryGetValue(pngPath, out Texture2D texture2D))
                {
                    __result = texture2D;
#if DEBUG
                    Debug.Log($"[{PluginName}] Read form memory: {pngPath}");
#endif
                }
                else if (Sideloader.Sideloader.PngList.TryGetValue(pngPath, out ZipmodInfo zipmodInfo))
                {
                    string filepath = Path.Combine(Application.dataPath, CachePath, pngPath);
                    byte[] buffer = null;
                    if (File.Exists(filepath))
                    {
                        using (FileStream stream = File.OpenRead(filepath))
                        {
                            buffer = new byte[stream.Length];
                            int _ = stream.Read(buffer, 0, (int)stream.Length);
                        }
#if DEBUG
                        Debug.Log($"[{PluginName}] Read form file: {pngPath}");
#endif
                    }
                    else
                    {
                        ZipFile zipFile = zipmodInfo.GetZipFile();
                        ZipEntry entry = zipFile.GetEntry(pngPath);

                        if (entry != null)
                        {
                            using (Stream stream = zipFile.GetInputStream(entry))
                            {
                                buffer = new byte[entry.Size];
                                int _ = stream.Read(buffer, 0, (int)entry.Size);


                                string directory = Path.GetDirectoryName(filepath);

                                if (!Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }

                                using (FileStream fileStream = File.OpenWrite(filepath))
                                using (BinaryWriter writer = new BinaryWriter(fileStream))
                                {
                                    writer.Write(buffer);
                                }

                                Debug.Log($"[{PluginName}] Write to {filepath}");
                            }
#if DEBUG
                            Debug.Log($"[{PluginName}] Read form zipmod: {pngPath}");
#endif
                        }
                    }

                    if (buffer != null && buffer.Length > 0)
                    {
                        var tex = new Texture2D(2, 2, format, mipmap);

                        tex.LoadImage(buffer);

                        if (pngPath.Contains("clamp"))
                            tex.wrapMode = TextureWrapMode.Clamp;
                        else if (pngPath.Contains("repeat"))
                            tex.wrapMode = TextureWrapMode.Repeat;

                        Texture2Ds[pngPath] = tex;
                        __result = tex;
                    }
                }


                if (__result == null)
                {
                    EmptyTexPath.Add(pngPath);
                }

                return false;
            }
        }
    }
}