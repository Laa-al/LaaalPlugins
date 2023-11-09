using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using ZipModUtilities.Data;

namespace ZipModUtilities.ModDownload
{
    public class DownloadManager
    {
        private readonly ModManager _manager;
        private readonly ConcurrentQueue<DownloadTask> _tasks = new();
        private int _totalTaskCount;
        private int _finishedTaskCount;

        public DownloadManager(ModManager manager)
        {
            _manager = manager;
        }

        protected virtual void FinishTask(DownloadTask task)
        {
            ModMessage message = _manager.AnalyseLocalFile(Path.Combine(task.Path, task.Name));

            ModMessage remote = _manager.GetRemoteMessage(task.Uri);

            if (remote is not null)
            {
                message.CopyTo(remote);
            }
        }

        public void StartTask(DownloadTask task)
        {
            if (task is not null)
            {
                _tasks.Enqueue(task);
                _totalTaskCount++;
            }
        }

        public class Downloader : IDisposable
        {
            private readonly DownloadManager _manager;
            private readonly HttpClient _client = new();
            private bool _isRunning = true;
            private DownloadTask _task;

            public Downloader(DownloadManager manager)
            {
                _manager = manager;
                Thread thread = new(DownloadAsync);
                thread.Start();
            }

            private async void DownloadAsync()
            {
                while (_isRunning)
                {
                    if (!_manager._tasks.TryDequeue(out _task))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    try
                    {
                        if (!Directory.Exists(_task.Path))
                            Directory.CreateDirectory(_task.Path);

                        ConsoleTask("Download Start", ConsoleColor.Blue);

                        await using (Stream response = await _client.GetStreamAsync(_task.Uri))
                        {
                            await using FileStream fs = File.OpenWrite(Path.Combine(_task.Path, _task.Name));

                            await response.CopyToAsync(fs);
                        }

                        _manager.FinishTask(_task);

                        ConsoleTask($"Download Finish({++_manager._finishedTaskCount}/{_manager._totalTaskCount})", ConsoleColor.Green);
                    }
                    catch (Exception e)
                    {
                        string path = Path.Combine(_task.Path, _task.Name);
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }

                        ConsoleTask($"Download Failed({++_manager._finishedTaskCount}/{_manager._totalTaskCount})", ConsoleColor.Red);
                        Console.WriteLine(e);
                    }
                }
                _client.Dispose();
            }

            private void ConsoleTask(string prefix, ConsoleColor color)
            {
                color.WriteLine($"\r\n[{prefix}]\r\n\t Id:\t{_task.Name}\r\n\t Uri:\t{_task.Uri}");
            }

            public void Dispose()
            {
                _isRunning = false;
            }
        }
    }
}