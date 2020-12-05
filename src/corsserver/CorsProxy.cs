using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Azure.Data.Tables;
using System.Linq;
using System.Collections.Generic;
using Azure;
using MeritCasting.Shared;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.AspNetCore.StaticFiles;

namespace MeritCasting.CorsProxy
{
    public static class CorsProxy
    {
        private const string PartitionKey = "feed";

        [FunctionName("CorsProxy")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var feed = req.Query["feed"];

            log.LogInformation($"query param - {feed}");

            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            if (!provider.TryGetContentType(feed, out contentType))
            {
                contentType = "application/octet-stream";
            }

            return new ContentResult() 
            { 
                Content = await GetFeedContentAsync(feed),
                ContentType = contentType,
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        //[FunctionName("Timer")]
        //public static void Run([TimerTrigger("* * * * *")] TimerInfo myTimer, ILogger log)
        //{
        //    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        //    log.LogInformation($"created client");

        //    Task.WhenAll(App.CastingFeeds.Select(castingFeed => NotifyPostUpdatesAsync(castingFeed))).Wait();

        //    log.LogInformation($"notified");
        //}

        private static async Task<SyndicationFeed> LoadFeedAsync(Uri feed)
        {
            using var reader = GetFeedReader(feed);

            return await Task.Run(() => SyndicationFeed.Load(reader));
        }

        private static async Task<SyndicationFeed> LoadFeedAsync(string feedContent)
        {
            using var reader = new StringReader(feedContent);
            using var xmlReader = XmlReader.Create(reader);

            return await Task.Run(() => SyndicationFeed.Load(xmlReader));
        }

        private static async Task<string> LoadFeedRawAsync(Uri feed)
        {
            using var reader = GetFeedReader(feed);

            return await reader.ReadOuterXmlAsync();
        }

        private static XmlReader GetFeedReader(Uri feed) => XmlReader.Create(feed.AbsoluteUri);

        public static async Task<string> GetFeedContentAsync(string feedUrl)
        {
            const string FeedContentKey = "Content";

            string content;

            var cachedFeedInfo = await GetCachedFeedInfoAsync(feedUrl);

            // if content is set and not stale then return cached content, otherwise fetch latest feed
            if(cachedFeedInfo != null && cachedFeedInfo.ContainsKey(FeedContentKey) && DateTime.Now < App.GetNextFeedCheckTime(await LoadFeedAsync(cachedFeedInfo.GetString(FeedContentKey))))
            {
                content = cachedFeedInfo.GetString(FeedContentKey);
            }
            else
            {
                // get raw feed
                content = await LoadFeedRawAsync(new Uri(feedUrl));

                // cache results
                if(cachedFeedInfo != null)
                {
                    if(cachedFeedInfo.ContainsKey(FeedContentKey))
                    {
                        cachedFeedInfo.Remove(FeedContentKey);
                    }

                    cachedFeedInfo.Add(FeedContentKey, content);

                    Client.UpdateEntity(cachedFeedInfo, ETag.All);
                }
                else
                {
                    var newCachedFeedInfo = new TableEntity(PartitionKey, GetFeedRowKey(feedUrl))
                    {
                        { FeedContentKey, content }
                    };

                    Client.AddEntity(newCachedFeedInfo);
                }
            }

            return content;
        }

        private static async Task<TableEntity> GetCachedFeedInfoAsync(string feedUrl)
        {
            var feedRowKey = GetFeedRowKey(feedUrl);
            
            return await Client.QueryAsync<TableEntity>(ent => ent.PartitionKey == PartitionKey && ent.RowKey == feedRowKey).FirstOrDefaultAsync();
        }

        private static TableClient Client { get; } = new TableClient(
                @"DefaultEndpointsProtocol=https;AccountName=meritcasting;AccountKey=SECRET;EndpointSuffix=core.windows.net",
                "CastingAgency");

        private static string GetFeedRowKey(string feedUrl) => Path.GetFileName(new Uri(feedUrl).LocalPath);

        public static async Task NotifyPostUpdatesAsync(string feedUrl)
        {
            const string LatestPostTimestampKey = "LatestPost";

            var savedFeedInfo = await GetCachedFeedInfoAsync(feedUrl);

            var feed = await LoadFeedAsync(new Uri(feedUrl));

            var feedTitle = App.GetNormalizedFeedTitle(feed);
            var lastNotifiedPost = savedFeedInfo != null ? DateTime.Parse(savedFeedInfo.GetString(LatestPostTimestampKey)) : DateTime.Now;
            var newPosts = feed.Items.Where(item => item.PublishDate.DateTime > lastNotifiedPost).ToArray();

            foreach (var newPost in newPosts)
            {
                using var httpClient = new HttpClient();
                var values = new Dictionary<string, string>
                {
                    { "app_key", "fgJvav0vqCkg4v0MuI7d" },
                    { "app_secret", "SECRET" },
                    { "target_type", "app" },
                    { "content", $"New post from {feedTitle}!" },
                    { "content_type", "url" },
                    { "content_extra", newPost.Id }
                };

                var content = new FormUrlEncodedContent(values);

                await httpClient.PostAsync("https://api.pushed.co/1/push", content);
            }

            if (newPosts.Any())
            {
                lastNotifiedPost = newPosts.First().PublishDate.DateTime;
            }

            // save most recent feed info

            TableEntity mostRecentFeedInfo = new TableEntity(PartitionKey, GetFeedRowKey(feedUrl))
            {
                { LatestPostTimestampKey, lastNotifiedPost.ToString() }
            };

            if (savedFeedInfo != null)
            {
                Client.UpdateEntity(mostRecentFeedInfo, ETag.All);
            }
            else
            {
                Client.AddEntity(mostRecentFeedInfo);
            }
        }
    }

    public class ContentResultProxy : ActionResult, IStatusCodeActionResult
    {
        private HttpResponseMessage _response;

        public ContentResultProxy(HttpResponseMessage response)
        {
            _response = response;
        }

        /// <summary>
        /// Gets or set the content representing the body of the response.
        /// </summary>
        public string Content { get => _response.Content.ReadAsStringAsync().Result; }

        /// <summary>
        /// Gets or sets the Content-Type header for the response.
        /// </summary>
        public string ContentType { get => _response.Content.Headers.ContentType.MediaType; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get => (int)HttpStatusCode.OK; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.HttpContext.RequestServices.GetService<IActionResultExecutor<ContentResultProxy>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
