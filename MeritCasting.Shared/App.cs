using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MeritCasting.Shared
{
    public class Post 
    {
        public Post()
        {
        }

        public Post(string feedName, string descriptionPreview, string url, DateTime publishTime, bool notificationsShown)
        {
            FeedName = feedName;
            DescriptionPreview = descriptionPreview;
            Url = url;
            PublishTime = publishTime;
            NotificationShown = notificationsShown;
        }

        public string FeedName { get; set; }
        public string DescriptionPreview { get; set; }
        public string Url { get; set; }
        public DateTime PublishTime { get; set; }
        public bool NotificationShown { get; set; }
    }

    public static class App
    {
        //const string MeritCasting = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675f5bb15a1fd4ee26d31c2d92.xml";
        private const string ChicagoFire = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675e68148d8a93f8694c8b4567.xml";
        private const string DarlingSeries = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675e66a5e78a93f8533f8b4567.xml";
        private const string BigLeap = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675e66a59c8a93f80e3b8b4567.xml";
        private const string ExtraOrdinary = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675e66a5478a93f8f1398b4567.xml";
        private const string FourStar = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675e66a5018a93f876368b4567.xml";
        private const string ChicagoPD = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675e66a4998a93f8372d8b4567.xml";
        private const string ChicagoMed = "https://fetchrss.com/rss/5e666b9e8a93f8ee2b8b45675e66a4348a93f89b288b4567.xml";

        public const int PollingIntervalAfterExpectedPublishTimeSeconds = 1;
        public const int FeedRefreshIntervalMinutes = 15;

        public static readonly string[] CastingFeeds = { DarlingSeries, BigLeap, ExtraOrdinary, FourStar, ChicagoFire, ChicagoPD, ChicagoMed };

        public static DateTime GetPublishDate(SyndicationFeed feed)
        {
            // .NET 5 apps will require custom linker configuration for the next line to work properly 
            // (see https://stackoverflow.com/questions/36788798/ixmlserializable-type-system-xml-linq-xelement-must-have-default-constructor)
            var att = feed.ElementExtensions.ReadElementExtensions<XElement>("pubDate", string.Empty);
            var value = att.FirstOrDefault();
            
            return DateTime.Parse(value.Value);
        }

        public static DateTime GetExpectedFeedUpdateTime(DateTime lastFeedUpdateTime)
        {
            return lastFeedUpdateTime.AddMinutes(FeedRefreshIntervalMinutes);
        }

        public static string GetNormalizedFeedTitle(SyndicationFeed feed) => feed.Title.Text.Replace(" on Facebook", string.Empty);

        public static DateTime GetNextFeedCheckTime(SyndicationFeed feed) => GetNextFeedCheckTime(feed.LastUpdatedTime.UtcDateTime);

        public static DateTime GetNextFeedCheckTime(DateTime lastFeedPublishTime)
        {
            var oneMinuteFromNow = DateTime.Now.AddMinutes(1);
            var expectedFeedRefreshTime = lastFeedPublishTime.AddMinutes(FeedRefreshIntervalMinutes);

            return expectedFeedRefreshTime >= oneMinuteFromNow ? expectedFeedRefreshTime : oneMinuteFromNow;
        }
    }
}
