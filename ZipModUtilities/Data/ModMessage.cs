using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace ZipModUtilities.Data
{
    public sealed class ModMessage
    {
        public DateTime UpdateLabel { get; set; }

        public bool IsLocal { get; init; }
        public string PathOrUri { get; set; }
        
        public string Guid { get; set; }
        public string FileName { get; set; }
        public string Version { get; set; }
        [NonSerialized] 
        private Version _version;
        public string Author { get; set; }
        public string DirectoryName { get; set; }
        public string Game { get; set; }
        public long FileSize { get; set; }
        public DateTime UpdateTime { get; set; }

        public Version GetVersion()
        {
            if (_version is null)
            {
                try
                {
                    _version = new Version(Version);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return _version;
        }

        public void SetRemoteUri(string uri, DateTime updateLabel)
        {
            if (updateLabel > UpdateLabel)
            {
                FileSize = 0;
            }

            PathOrUri = uri;
            UpdateLabel = updateLabel;
        }

        public void FitMessage(string path)
        {
            using ZipArchive file = ZipFile.OpenRead(path);
            ZipArchiveEntry entry = file.GetEntry("manifest.xml");
            if (entry is null) return;
            using Stream stream = entry.Open();
            XmlDocument document = new();
            document.Load(stream);
            XmlElement root = document.DocumentElement;
            string guid = GetProperty(root, "guid");
            if (guid is null) return;
            FileInfo fileInfo = new(path);

            Guid = GetProperty(root, "guid");
            Author = GetProperty(root, "author");
            Version = GetProperty(root, "version");

            if (root is not null)
            {
                foreach (XmlNode node in root
                             .GetElementsByTagName("game"))
                {
                    if (string.Equals(node.InnerText, "Honey Select 2", StringComparison.OrdinalIgnoreCase))
                    {
                        Game = "Honey Select 2";
                        break;
                    }
                }
            }

            FileSize = fileInfo.Length;
            UpdateTime = fileInfo.LastWriteTime;

            DirectoryName = FitPropertyToPath(Author);
            if (string.IsNullOrEmpty(DirectoryName))
            {
                DirectoryName = "anonymous";
            }

            FileName = $"[{DirectoryName}]{FitPropertyToPath(Guid)}.zipmod";
        }

        private static string GetProperty(XmlElement element, string property)
        {
            return element?
                .GetElementsByTagName(property)
                .Item(0)?.InnerText.Trim();
        }

        private static string FitPropertyToPath(string property)
        {
            if (property is null)
            {
                return null;
            }

            List<char> chars = property.ToList();

            for (int i = 0; i < chars.Count; i++)
            {
                char c = chars[i];
                if (c is '\\' or '.'
                    or '/' or ':' or '*' or '?'
                    or '"' or '<' or '>' or '|')
                {
                    chars[i] = '_';
                }
            }

            RemoveBetween('[', ']');
            RemoveBetween('(', ')');

            return string.Join(null, chars).Trim();

            void RemoveBetween(char lft, char rht)
            {
                int start = chars.IndexOf(lft);
                int end = chars.IndexOf(rht);
                if (start >= 0 && end >= 0 && start < end)
                {
                    chars.RemoveRange(start, end - start + 1);
                }
            }
        }

        public void MoveTo(string path)
        {
            if (!IsLocal)
            {
                ConsoleColor.Red.WriteLine($"\r\n[Error] Not local path: {PathOrUri}");
                return;
            }

            if (!File.Exists(PathOrUri))
            {
                ConsoleColor.Red.WriteLine($"\r\n[Error] File doesn't exist: {PathOrUri}");
                return;
            }

            string targetPath = Path.Combine(path, DirectoryName);
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            string newPath = Path.Combine(targetPath, FileName);
            string oldPath = PathOrUri;

            if (File.Exists(newPath))
            {
                string repeatPath = SharedConsts.GetRepeatPath(this);

                File.Move(newPath, repeatPath);
            }

            File.Move(oldPath, newPath);
            PathOrUri = newPath;
        }


        public ModMessage Clone()
        {
            return MemberwiseClone() as ModMessage;
        }

        public void CopyTo(ModMessage target)
        {
            target.Guid = Guid;
            target.Author = Author;
            target.Version = Version;
            target.Game = Game;

            target.FileSize = FileSize;
            target.UpdateTime = UpdateTime;

            target.DirectoryName = DirectoryName;
            target.FileName = FileName;
        }
    }
}