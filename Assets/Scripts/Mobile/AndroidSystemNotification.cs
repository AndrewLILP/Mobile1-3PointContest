using UnityEngine;
using Unity.Notifications.Android;

public class AndroidSystemNotification : MonoBehaviour
{
    [Header("Notification Settings")]
    [SerializeField] private string channelId = "default_channel";
    [SerializeField] private string channelName = "Default Channel";
    [SerializeField] private string notificationTitle = "Your Energy has recharged!";
    [SerializeField] private string notificationText = "Come back to play.";
    [SerializeField] private int daysToWait = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_ANDROID
        // Create a channel for the notification
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = channelName,
            Importance = Importance.Low,
            Description = "Low Importance"
        };
        // Register the channel with the Android system
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        
        // Create a notification
        var notification = new AndroidNotification
        {
            Title = notificationTitle,
            Text = notificationText,
            SmallIcon = "default",
            LargeIcon = "default",
            FireTime = System.DateTime.Now.AddDays(daysToWait) // Fire after specified days
        };
        
        // Schedule the notification
        AndroidNotificationCenter.SendNotification(notification, channelId);
#endif
    }

    public void ScheduleNotification(string title, string text, int days)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = text,
            SmallIcon = "default",
            LargeIcon = "default",
            FireTime = System.DateTime.Now.AddDays(days)
        };
        
        AndroidNotificationCenter.SendNotification(notification, channelId);
#endif
    }

    public void CancelAllNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#endif
    }
}