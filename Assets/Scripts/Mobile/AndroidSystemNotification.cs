using UnityEngine;
using Unity.Notifications.Android;

public class AndroidSystemNotification : MonoBehaviour
{
    private string idString = "default_channel";


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_ANDROID
        // Create a channel for the notification
        var channel = new AndroidNotificationChannel()
        {
            Id = "default_channel",
            Name = "Default Channel",
            Importance = Importance.Low,
            Description = "Low Importance"
        };
        // Register the channel with the Android system
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        // Create a notification
        var notification = new AndroidNotification
        {
            Title = "Your Energy has recharged!",
            Text = "Come back to play.",
            SmallIcon = "default",
            LargeIcon = "default",
            FireTime = System.DateTime.Now.AddDays(1) // Fire after 1 day
        };
        // Schedule the notification
        AndroidNotificationCenter.SendNotification(notification, channel.Id);
#endif
    }

}