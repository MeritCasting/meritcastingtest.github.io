using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Append.Blazor.Notifications;
using Microsoft.JSInterop;

namespace MeritCasting
{
    public class Posts
    {
        public static async Task ShowLatestPostsAsync(string feedUrl, Blazored.LocalStorage.ILocalStorageService localStorage, INotificationService notificationService)
        {
            const string LatestPostTag = "LatestPost";

            // https://stackoverflow.com/questions/44468743/how-to-call-medium-rss-feed
            var url = $"https://cors-anywhere.herokuapp.com/{feedUrl}";

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("origin", "https://localhost:5001");
            var response = await http.GetAsync(url);

            using var reader = XmlReader.Create(await response.Content.ReadAsStreamAsync());
            var feed = SyndicationFeed.Load(reader);

            var feedTitle = feed.Title.Text.Replace(" on Facebook", string.Empty);
            var latestFeedPostTag = $"{feedTitle}|{LatestPostTag}";
            var lastNotifiedPost = await localStorage.ContainKeyAsync(latestFeedPostTag) ? await localStorage.GetItemAsync<DateTime>(latestFeedPostTag) : DateTime.UtcNow;

            var newPosts = feed.Items.Where(item => item.PublishDate.DateTime > lastNotifiedPost).ToArray();

            foreach (var facebookPost in newPosts)
            {
                //_ = notificationService.CreateAsync(
                //    "Merit Casting",
                //    new NotificationOptions()
                //    {
                //        Body = $"New post from {feedTitle}",
                //        Data = 
                //    });
                //_ = Notification.ShowAsync(
                //    js,
                //    new Notification()
                //    {
                //        Title = "Merit Casting",
                //        Message = $"New post from {feedTitle}!",
                //        Url = facebookPost.Id
                //    });

                //if (newPosts.Any())
                //{
                //    lastNotifiedPost = newPosts.First().PublishDate.DateTime;
                //}
            }

            await localStorage.SetItemAsync(latestFeedPostTag, newPosts.First().PublishDate.DateTime);
        }
    }
}
