using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace MmdList
{
    public class MotionAnalyser
    {
        public List<MotionInfo> MotionFiles { get; } = new();
        public List<MotionInfo> MorphFiles { get; } = new();
        public List<MotionInfo> CameraFiles { get; } = new();
        public List<string> MusicFiles { get; } = new();
        public List<MotionAnalyser> SubAnalysers { get; } = new();
        public string FolderPath { get; }
        public string BasePath { get; }
        public string DanceName { get; }
        public int DancerNum { get; set; }
        public bool IsMmd { get; set; }

        public MotionAnalyser(bool isMmd,
            string folderPath, string basePath,
            string danceName, int dancerNum)
        {
            IsMmd = isMmd;
            FolderPath = folderPath;
            BasePath = basePath;
            DanceName = danceName;
            DancerNum = dancerNum;
        }

        public static MotionAnalyser CreateFromPath(string path, string rootPath)
        {
            string folderPath = path;
            string[] fileNames = Directory.GetFiles(folderPath);
            int dancerNum = 1;

            if (folderPath.EndsWith("P", StringComparison.Ordinal))
            {
                if (int.TryParse(folderPath[^3..^1], out int count))
                {
                    dancerNum = count;
                }
            }

            List<char> danceNameList = Path.GetFileName(folderPath).ToList();
            danceNameList.RemoveAll(u => u is '[' or ']');
            string danceName = string.Join(null, danceNameList);

            bool isMmd = fileNames.Any(u =>
                u.EndsWith(".vmd", StringComparison.OrdinalIgnoreCase));

            string basePath = folderPath[rootPath.Length..];

            MotionAnalyser analyser = new(isMmd, folderPath, basePath, danceName, dancerNum);

            if (isMmd)
            {
                foreach (string fileName in fileNames)
                {
                    if (fileName.EndsWith(".wav"))
                    {
                        analyser.MusicFiles.Add(Path.GetFileName(fileName));
                    }
                    else if (fileName.EndsWith(".vmd"))
                    {
                        try
                        {
                            analyser.CreateAndAddInfo(fileName);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine($"fileName: {fileName}");
                        }
                    }
                }

                analyser.MotionFiles.Sort((u, v) => v.Motion - u.Motion);
                analyser.MorphFiles.Sort((u, v) => v.Morph - u.Morph);
                analyser.CameraFiles.Sort((u, v) => v.Camera - u.Camera);

                analyser.IsMmd =
                    analyser.MusicFiles.Any() &&
                    analyser.MotionFiles.Any(u => u.Motion > 0);
            }
            else
            {
                string[] folderNames = Directory.GetDirectories(folderPath);
                foreach (string folderName in folderNames)
                {
                    analyser.SubAnalysers.Add(CreateFromPath(folderName, rootPath));
                }
            }

            return analyser;
        }

        private void CreateAndAddInfo(string path)
        {
            MotionInfo info = MotionInfo.Create(path);
            if (info.Motion > 0)
            {
                MotionFiles.Add(info);
            }
            else if (info.Morph > 0)
            {
                MorphFiles.Add(info);
            }
            else if (info.Camera > 0)
            {
                CameraFiles.Add(info);
            }
        }

        private void DoRecursion([NotNull] Action<MotionAnalyser> action)
        {
            if (SubAnalysers.Count > 0)
            {
                foreach (MotionAnalyser analyser in SubAnalysers)
                {
                    analyser.DoRecursion(action);
                }
            }

            action.Invoke(this);
        }

        public void WriteList(StreamWriter sw)
        {
            if (!IsMmd) return;

            sw.WriteLine($"[{DanceName}]");
            sw.WriteLine($"basefolder={BasePath}\\");


            MotionInfo[] motions = new MotionInfo[DancerNum];
            MotionInfo[] morphs = new MotionInfo[DancerNum];

            int minLen = Math.Min(MotionFiles.Count, DancerNum);

            DancerNum = minLen;

            for (int i = 0; i < minLen; i++)
            {
                motions[i] = MotionFiles[i];
            }

            minLen = Math.Min(MorphFiles.Count, DancerNum);
            for (int i = 0; i < minLen; i++)
            {
                if (motions[i] is not null &&
                    motions[i].Morph < MorphFiles[i].Morph)
                {
                    morphs[i] = MorphFiles[i];
                }
            }

            sw.WriteLine($"DancerNumber={DancerNum}");

            for (int i = 0; i < DancerNum; i++)
            {
                if (motions[i] is null) break;

                sw.WriteLine($"Dancer{i}Motion={motions[i].FileName}");
                if (morphs[i] is null)
                {
                    if (motions[i].Morph > 0)
                    {
                        sw.WriteLine($"Dancer{i}Morph={motions[i].FileName}");
                    }
                }
                else
                {
                    sw.WriteLine($"Dancer{i}Morph={morphs[i].FileName}");
                }
            }

            for (int i = 0; i < CameraFiles.Count; i++)
            {
                MotionInfo info = CameraFiles[i];
                sw.WriteLine($"Camera{i}={info.FileName}");
            }

            sw.WriteLine($"Music={MusicFiles.First()}");
            sw.WriteLine();
        }

        public void SimplyFileName()
        {
            if (SubAnalysers.Count > 0)
            {
                foreach (MotionAnalyser analyser in SubAnalysers)
                {
                    analyser.SimplyFileName();
                }
            }
            else
            {
                MoveFile(MotionFiles, "Motion");
                MoveFile(MorphFiles, "Morph");
                MoveFile(CameraFiles, "Camera");

                for (int i = 0; i < MusicFiles.Count; i++)
                {
                    string fileName = "Music" + i + "_.wav";
                    string srcPath = Path.Combine(FolderPath, MusicFiles[i]);
                    string dstPath = Path.Combine(FolderPath, fileName);
                    File.Move(srcPath, dstPath);
                    MusicFiles[i] = fileName;
                }

                for (int i = 0; i < MusicFiles.Count; i++)
                {
                    string fileName = "Music" + i + ".wav";
                    string srcPath = Path.Combine(FolderPath, MusicFiles[i]);
                    string dstPath = Path.Combine(FolderPath, fileName);
                    File.Move(srcPath, dstPath);
                    MusicFiles[i] = fileName;
                }
            }

            return;

            void MoveFile(IReadOnlyList<MotionInfo> infos, string name)
            {
                for (int i = 0; i < infos.Count; i++)
                {
                    string fileName = name + i + "_.vmd";
                    MotionInfo info = infos[i];
                    string srcPath = Path.Combine(FolderPath, info.FileName);
                    string dstPath = Path.Combine(FolderPath, fileName);
                    File.Move(srcPath, dstPath);
                    info.FileName = fileName;
                }

                for (int i = 0; i < infos.Count; i++)
                {
                    string fileName = name + i + ".vmd";
                    MotionInfo info = infos[i];
                    string srcPath = Path.Combine(FolderPath, info.FileName);
                    string dstPath = Path.Combine(FolderPath, fileName);
                    File.Move(srcPath, dstPath);
                    info.FileName = fileName;
                }
            }
        }


        public static void SimplyFileNames(string path)
        {
            MotionAnalyser analyser = CreateFromPath(path, path);

            analyser.SimplyFileName();
        }
    }
}