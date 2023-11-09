using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using ZipModUtilities.Data;

namespace RemoteSearch
{
    public class RemoteAnalyzer : IDisposable
    {
        private readonly ModManager _manager;
        private readonly HttpClient _client = new();
        private readonly Queue<string> _uris = new();

        public RemoteAnalyzer(ModManager manager)
        {
            _manager = manager;
        }

        public async Task AnalyzeSourcesAsync(params string[] sources)
        {
            foreach (string source in sources)
            {
                _uris.Enqueue(source);
            }

            while (_uris.TryDequeue(out string uri))
            {
                await AnalyzeUri(uri);
            }
        }

        private async Task AnalyzeUri(string uri)
        {
            XmlDocument document = new();
            
            string html = await _client.GetStringAsync(uri);

            int start = html.IndexOf("<table", StringComparison.Ordinal);
            int end = html.IndexOf("</table>", StringComparison.Ordinal) + 8;
            string xml = html[start..end].Replace("&nbsp;", "");
            document.LoadXml(xml);

            if (document.DocumentElement is null)
                return;

            XmlNodeList trs = document.DocumentElement.GetElementsByTagName("tr");

            foreach (XmlElement tr in trs)
            {
                if (!tr.HasAttribute("class") ||
                    tr.GetAttribute("class") == "indexhead")
                    continue;

                XmlElement updateTime = null, updatePath = null;

                foreach (XmlElement td in tr.GetElementsByTagName("td"))
                {
                    if (!tr.HasAttribute("class"))
                        continue;
                    string tdClass = td.GetAttribute("class");

                    switch (tdClass)
                    {
                        case "indexcollastmod":
                            updateTime = td;
                            break;
                        case "indexcolname":
                            updatePath = td;
                            break;
                    }
                }

                XmlNode linkNode = updatePath?.GetElementsByTagName("a").Item(0);

                if (linkNode is null || linkNode.InnerText == "Parent Directory") continue;

                string path = linkNode.Attributes?["href"]?.Value.Trim();

                if (string.IsNullOrEmpty(path)) continue;

                path = uri + path;

                if (path.EndsWith("/"))
                {
                    _uris.Enqueue(path);
                }
                else if (
                    path.EndsWith(".zipmod") ||
                    path.EndsWith(".zip"))
                {
                    _ = DateTime.TryParse(updateTime?.InnerText, out DateTime dateTime);
                    _manager.GetOrCreateRemoteMessage(path, dateTime);
                    ConsoleColor.Green.WriteLine($"[Success] Add {path}");
                }
            }
        }


        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}