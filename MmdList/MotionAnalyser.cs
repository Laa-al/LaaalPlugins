using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MmdList
{
    public class MotionAnalyser
    {
        private readonly List<MotionDescriptor> _motionFiles = new();
        private readonly List<MotionDescriptor> _morphFiles = new();
        private readonly List<MotionDescriptor> _cameraFiles = new();
        private readonly List<string> _musicFiles = new();
        private readonly List<MotionAnalyser> _subAnalysers = new();
        private readonly string _folderPath;
        private readonly string _basePath;
        private readonly string _danceName;
        private int _dancerNum;

        public MotionAnalyser(string folderPath, string basePath, string danceName, int dancerNum)
        {
            _folderPath = folderPath;
            _basePath = basePath;
            _danceName = danceName;
            _dancerNum = dancerNum;
        }

        private static MotionAnalyser CreateFromPath(string path, string rootPath)
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

            string basePath = isMmd ? folderPath[rootPath.Length..] : "";

            MotionAnalyser analyser = new(folderPath, basePath, danceName, dancerNum);

            if (isMmd)
            {
                foreach (string fileName in fileNames)
                {
                    if (fileName.EndsWith(".wav"))
                    {
                        analyser._musicFiles.Add(Path.GetFileName(fileName));
                    }
                    else if (fileName.EndsWith(".vmd"))
                    {
                        try
                        {
                            MotionDescriptor descriptor = new(fileName);
                            analyser.AddDescriptor(descriptor);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine($"fileName: {fileName}");
                        }
                    }
                }
            }
            else
            {
                string[] folderNames = Directory.GetDirectories(folderPath);
                foreach (string folderName in folderNames)
                {
                    analyser._subAnalysers.Add(CreateFromPath(folderName, rootPath));
                }
            }

            analyser.SortFiles();

            return analyser;
        }

        public void AddDescriptor(MotionDescriptor descriptor)
        {
            if (descriptor.Motion[0] > 0)
            {
                _motionFiles.Add(descriptor);
            }
            else if (descriptor.Motion[1] > 0)
            {
                _morphFiles.Add(descriptor);
            }
            else if (descriptor.Motion[2] > 0)
            {
                _cameraFiles.Add(descriptor);
            }
        }

        public void SortFiles()
        {
            _motionFiles.Sort((u, v) => v.Motion[0] - u.Motion[0]);
            _morphFiles.Sort((u, v) => v.Motion[1] - u.Motion[1]);
            _cameraFiles.Sort((u, v) => v.Motion[2] - u.Motion[2]);
        }

        public void WriteList(StreamWriter sw)
        {
            if (_subAnalysers.Count > 0)
            {
                foreach (MotionAnalyser analyser in _subAnalysers)
                {
                    analyser.WriteList(sw);
                }
            }
            else if (_musicFiles.Any() &&
                     _motionFiles.Any(u => u.Motion[0] > 0))
            {
                sw.WriteLine($"[{_danceName}]");
                sw.WriteLine($"basefolder={_basePath}\\");


                MotionDescriptor[] motions = new MotionDescriptor[_dancerNum];
                MotionDescriptor[] morphs = new MotionDescriptor[_dancerNum];

                int minLen = Math.Min(_motionFiles.Count, _dancerNum);

                _dancerNum = minLen;

                for (int i = 0; i < minLen; i++)
                {
                    motions[i] = _motionFiles[i];
                }

                minLen = Math.Min(_morphFiles.Count, _dancerNum);
                for (int i = 0; i < minLen; i++)
                {
                    if (motions[i] is not null &&
                        motions[i].Motion[1] < _morphFiles[i].Motion[1])
                    {
                        morphs[i] = _morphFiles[i];
                    }
                }

                sw.WriteLine($"DancerNumber={_dancerNum}");

                for (int i = 0; i < _dancerNum; i++)
                {
                    if (motions[i] is null) break;

                    sw.WriteLine($"Dancer{i}Motion={motions[i].FileName}");
                    if (morphs[i] is null)
                    {
                        if (motions[i].Motion[1] > 0)
                        {
                            sw.WriteLine($"Dancer{i}Morph={motions[i].FileName}");
                        }
                    }
                    else
                    {
                        sw.WriteLine($"Dancer{i}Morph={morphs[i].FileName}");
                    }
                }

                for (int i = 0; i < _cameraFiles.Count; i++)
                {
                    MotionDescriptor descriptor = _cameraFiles[i];
                    sw.WriteLine($"Camera{i}={descriptor.FileName}");
                }

                sw.WriteLine($"Music={_musicFiles.First()}");
                sw.WriteLine();
            }
        }

        public void SimplyFileName()
        {
            if (_subAnalysers.Count > 0)
            {
                foreach (MotionAnalyser analyser in _subAnalysers)
                {
                    analyser.SimplyFileName();
                }
            }
            else
            {
                MoveFile(_motionFiles, "Motion");
                MoveFile(_morphFiles, "Morph");
                MoveFile(_cameraFiles, "Camera");

                for (int i = 0; i < _musicFiles.Count; i++)
                {
                    string fileName = "Music" + i + "_.wav";
                    string srcPath = Path.Combine(_folderPath, _musicFiles[i]);
                    string dstPath = Path.Combine(_folderPath, fileName);
                    File.Move(srcPath, dstPath);
                    _musicFiles[i] = fileName;
                }

                for (int i = 0; i < _musicFiles.Count; i++)
                {
                    string fileName = "Music" + i + ".wav";
                    string srcPath = Path.Combine(_folderPath, _musicFiles[i]);
                    string dstPath = Path.Combine(_folderPath, fileName);
                    File.Move(srcPath, dstPath);
                    _musicFiles[i] = fileName;
                }
            }

            return;

            void MoveFile(IReadOnlyList<MotionDescriptor> descriptors, string name)
            {
                for (int i = 0; i < descriptors.Count; i++)
                {
                    string fileName = name + i + "_.vmd";
                    MotionDescriptor descriptor = descriptors[i];
                    string srcPath = Path.Combine(_folderPath, descriptor.FileName);
                    string dstPath = Path.Combine(_folderPath, fileName);
                    File.Move(srcPath, dstPath);
                    descriptor.FileName = fileName;
                }

                for (int i = 0; i < descriptors.Count; i++)
                {
                    string fileName = name + i + ".vmd";
                    MotionDescriptor descriptor = descriptors[i];
                    string srcPath = Path.Combine(_folderPath, descriptor.FileName);
                    string dstPath = Path.Combine(_folderPath, fileName);
                    File.Move(srcPath, dstPath);
                    descriptor.FileName = fileName;
                }
            }
        }


        public static void SimplyFileNames(string path)
        {
            MotionAnalyser analyser = CreateFromPath(path, path);

            analyser.SimplyFileName();
        }

        public static void Analyse(string path, string fileName = "catalogue.txt")
        {
            MotionAnalyser analyser = CreateFromPath(path, path);
            
            using FileStream fileStream = File.OpenWrite(Path.Combine(path, fileName));
            using StreamWriter writer = new(fileStream);
            analyser.WriteList(writer);
        }
    }
}