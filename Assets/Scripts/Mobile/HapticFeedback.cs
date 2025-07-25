using UnityEngine;

public class HapticFeedback : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Handheld.Vibrate(); // Trigger haptic feedback on start
    }

    
    public void TriggerVibration()
    {
#if UNITY_ANDROID
        // Android-specific vibration code
        Handheld.Vibrate();
#endif
    }
}