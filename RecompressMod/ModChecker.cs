using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ReCompressMod
{
    public class ModChecker
    {
        public int ModNeedRecompressed { get; private set; }
        public int ModRecompressedFinished{ get; private set; }

        private readonly ConcurrentQueue<string> _queue = new();

        public ModChecker(int threadCount)
        {
            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new(RecompressMod);
                thread.Start();
            }
        }

        public void RecompressMod()
        {
            while (true)
            {
                try
                {
                    if (_queue.TryDequeue(out string file))
                    {
                        string tPath = file + "tmp";

                        ZipFile.ExtractToDirectory(
                            file,
                            tPath,
                            true);

                        File.Delete(file);

                        ZipFile.CreateFromDirectory(
                            tPath,
                            file,
                            CompressionLevel.NoCompression,
                            false);

                        Directory.Delete(tPath, true);
                        
                        Console.WriteLine(
                            $"\r\n[Recompress] Count: {++ModRecompressedFinished} / {ModNeedRecompressed}\r\n {file}");
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }


        public void CheckModInDirectory(string path)
        {
            int finishedCount = 0;
            ListModInDirectory(path);
            Console.WriteLine();


            return;

            void ListModInDirectory(string d)
            {
                string[] files = Directory.GetFiles(d);

                foreach (string file in files)
                {
                    bool shouldCompress = false;
                    if (file.EndsWith(".zipmod"))
                    {
                        using ZipArchive archive = ZipFile.OpenRead(file);
                        
                        ZipArchiveEntry entry = archive.GetEntry("manifest.xml");
                        
                        if (entry is not null &&
                            entry.CompressedLength != entry.Length)
                        {
                            shouldCompress = true;
                        }
                    }

                    if (shouldCompress)
                    {
                        _queue.Enqueue(file);
                        ModNeedRecompressed++;
                    }

                    Console.Write($"\r[Search] Searched count: {++finishedCount}");
                }

                string[] directories = Directory.GetDirectories(d);

                foreach (string directory in directories)
                {
                    ListModInDirectory(directory);
                }
            }
        }

        public void ReCompressMod(string path)
        {
        }
    }
}