using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Blazored.LocalStorage;
using MeritCasting.Shared;

namespace MeritCasting
{
    public class Repository
    {
        private const string UnreadPostsKey = "UnreadPosts";

        private ILocalStorageService _localStorage;
        private HttpClient _http;

        public Repository(ILocalStorageService localStorage, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _http = httpClient;
        }

        public async Task<SyndicationFeed> GetFeedAsync(string feedUrl)
        {
            // Use CORS proxy - see https://stackoverflow.com/questions/44468743/how-to-call-medium-rss-feed
            var url = $"https://winappmerit.azurewebsites.net/api/CorsProxy?feed={feedUrl}";

            using var reader = XmlReader.Create(await _http.GetStreamAsync(url));

            return SyndicationFeed.Load(reader);
        }

        public async Task<List<Post>> GetUnreadPostsAsync()
        {
            var unreadPosts = new List<Post>();

            if (await _localStorage.ContainKeyAsync(UnreadPostsKey))
            {
                unreadPosts = JsonSerializer.Deserialize<List<Post>>(await _localStorage.GetItemAsync<string>(UnreadPostsKey));
            }

            return unreadPosts;
        }

        private static string GetLastFeedPublishTimeKey(string feedUrl) => $"{feedUrl}|LastFeedPublishTime";

        public async Task<DateTime?> GetLastFeedPublishTime(string feedUrl)
        {
            return await _localStorage.ContainKeyAsync(GetLastFeedPublishTimeKey(feedUrl)) ? await _localStorage.GetItemAsync<DateTime>(GetLastFeedPublishTimeKey(feedUrl)) : (DateTime?)null;
        }

        public async Task SaveLastFeedPublishTime(string feedUrl, DateTime timestamp)
        {
            await _localStorage.SetItemAsync(GetLastFeedPublishTimeKey(feedUrl), timestamp);
        }

        public async Task SaveUnreadPostsAsync(List<Post> unreadPosts)
        {
            if (unreadPosts.Any())
            {
                await _localStorage.SetItemAsync(UnreadPostsKey, JsonSerializer.Serialize(unreadPosts));
            }
            else
            {
                if (await _localStorage.ContainKeyAsync(UnreadPostsKey))
                {
                    await _localStorage.RemoveItemAsync(UnreadPostsKey);
                }
            }
        }
    }
}
