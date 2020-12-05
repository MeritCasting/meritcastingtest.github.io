using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Append.Blazor.Notifications;
using Microsoft.JSInterop;

namespace MeritCasting
{
    public static class NotificationServiceExtensions
    {
        public static async Task<PermissionType> GetCurrentPermissionAsync(this INotificationService notificationService, IJSRuntime js)
        {
            return await js.InvokeAsync<string>("getPermissionStatus") switch
            {
                "default" => PermissionType.Default,
                "denied" => PermissionType.Denied,
                "granted" => PermissionType.Granted
            };
        }
    }
}
