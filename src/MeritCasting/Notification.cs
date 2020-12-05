using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MeritCasting
{
    public record Notification(string Title, string Message, string Url)
    {
        public static async Task ShowAsync(IJSRuntime js, Notification notification)
        {
            const string ShowNotification = "ShowNotification";

            await js.InvokeVoidAsync(
                ShowNotification,
                notification.Title,
                notification.Message,
                notification.Url);
        }
    }
}
