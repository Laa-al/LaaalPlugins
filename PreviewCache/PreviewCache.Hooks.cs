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
                if (!Texture2Ds.TryGetValue(pngPath, out var texture))
                {
                    texture = GetTextureFromZipmod(pngPath, format, mipmap);
                    Texture2Ds[pngPath] = texture;
                }
                
                __result = texture;

                return false;
            }
        }
    }
}