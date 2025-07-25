using UnityEngine;
using UnityEngine.Android;
/// <summary>
/// This class handles microphone permissions for Android devices.
/// Place at the start of your scene to ensure permissions are requested.
/// </summary>
public class MicrophonePermissions : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
#if UNITY_ANDROID
        // Check if the microphone permission is already granted
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            // Request microphone permission from the user
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif

    }

}