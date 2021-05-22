using System;
using System.IO;
using System.Threading.Tasks;
using RestSharp;
using System.Net;
using System.Collections.Generic;

namespace Beefusal
{
    internal static class Sync
    {
        // Login to Beevenue as service account (keep cookie around), setting SFW to false.
        private static int? Setup(RestClient client, Config config)
        {
            BeefusalLog.ReadableLogger.Information($"Setting up API connection...");

            // LOGIN
            var loginRequest = new RestRequest("login").AddJsonBody(new
            {
                username = config.Credentials.User,
                password = config.Credentials.Password
            });

            var response = client.Post(loginRequest);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                BeefusalLog.ReadableLogger.Error($"Could not log in to remote server: {response.Content}");
                return 1;
            }


            // SFW
            var sfwRequest = new RestRequest("sfw").AddJsonBody(new
            {
                sfwSession = false
            });

            response = client.Patch(sfwRequest);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                BeefusalLog.ReadableLogger.Error($"Could not update SFW mode: {response.Content}");
                return 2;
            }

            return null;
        }

        public static async Task<int> Run(Config config)
        {
            BeefusalLog.ReadableLogger.Information($"Starting sync at {DateTime.Now}");

            var client = new RestClient("https://i.prat.ch/api/");
            client.CookieContainer = new CookieContainer();

            var setupErrorCode = Setup(client, config);

            if (setupErrorCode.HasValue)
            {
                return setupErrorCode.Value;
            }

            foreach (var entry in config.Queries)
            {
                var errorCode = SyncQuery(config, entry, client);
                if (errorCode.HasValue)
                {
                    return errorCode.Value;
                }
            }

            BeefusalLog.ReadableLogger.Information($"Completed synchronization at {DateTime.Now}");
            return 0;
        }

        private static int? SyncQuery(Config config, QueryHolder entry, RestClient client)
        {
            BeefusalLog.ReadableLogger.Information($"Running query \"{entry.Name}\" ({entry.Query})...");

            // Ensure target folder exists
            var targetPath = Path.Combine(config.TargetFolder, entry.Name);
            Directory.CreateDirectory(targetPath);

            // Run search query on API
            var response = client.Get<BatchSearchJson>(new RestRequest("search/batch").AddQueryParameter("q", entry.Query));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                BeefusalLog.ReadableLogger.Error($"Could not execute query for terms \"{entry.Query}\": {response.Content}");
                return 4;
            }

            var searchResults = response.Data.Items;

            var currentFiles = new HashSet<string>(Directory.GetFiles(targetPath));

            var downloadedCount = 0;
            foreach (var searchResult in searchResults)
            {
                // If it exists locally, don't download it.
                var targetFileName = Path.Combine(targetPath, searchResult);
                if (File.Exists(targetFileName))
                    continue;

                // Else go ahead and download it.
                var binaryBlob = client.DownloadData(new RestRequest($"files/{searchResult}"));
                File.WriteAllBytes(targetFileName, binaryBlob);
                downloadedCount++;
            }

            var obsoleteCount = 0;
            // At the end, take all items that exist locally, but not in searchResults, and delete them.
            foreach (var currentFile in currentFiles)
            {
                var shortFileName = Path.GetFileName(currentFile);
                if (!searchResults.Contains(shortFileName))
                {
                    File.Delete(currentFile);
                    obsoleteCount++;
                }
            }

            BeefusalLog.ReadableLogger.Information($"Downloaded {downloadedCount} new files, deleted {obsoleteCount} obsolete files.");
            return null;
        }

        private class BatchSearchJson
        {
            public List<string> Items { get; set; }
        }
    }
}