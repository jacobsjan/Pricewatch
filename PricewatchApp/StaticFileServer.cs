using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using MimeTypes;

namespace PricewatchApp
{
    public static class StaticFileServer
    {
        const string staticFilesFolder = "www";
        const string defaultPage = "index.html";

        [FunctionName("StaticFileServer")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                var filePath = GetFilePath(req, log);
                log.Info($"Function1 function processing request for {filePath}.");

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                var stream = new FileStream(filePath, FileMode.Open);
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType =
                    new MediaTypeHeaderValue(GetMimeType(filePath));
                response.Headers.CacheControl = new CacheControlHeaderValue() { MaxAge = new TimeSpan(1, 0, 0, 0) }; // One day
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static string GetScriptPath()
            => Path.Combine(GetEnvironmentVariable("HOME"), @"site\wwwroot");

        private static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        private static string GetFilePath(HttpRequestMessage req, TraceWriter log)
        {
            var pathValue = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "file", true) == 0)
                .Value;

            var path = pathValue ?? "";

            var staticFilesPath =
                Path.GetFullPath(Path.Combine(GetScriptPath(), staticFilesFolder));
            var fullPath = Path.GetFullPath(Path.Combine(staticFilesPath, path));

            if (!IsInDirectory(staticFilesPath, fullPath))
            {
                throw new ArgumentException("Invalid path");
            }

            var isDirectory = Directory.Exists(fullPath);
            if (isDirectory)
            {
                fullPath = Path.Combine(fullPath, defaultPage);
            }

            return fullPath;
        }

        private static bool IsInDirectory(string parentPath, string childPath)
        {
            var parent = new DirectoryInfo(parentPath);
            var child = new DirectoryInfo(childPath);

            var dir = child;
            do
            {
                if (dir.FullName == parent.FullName)
                {
                    return true;
                }
                dir = dir.Parent;
            } while (dir != null);

            return false;
        }

        private static string GetMimeType(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return MimeTypeMap.GetMimeType(fileInfo.Extension);
        }
    }
}
