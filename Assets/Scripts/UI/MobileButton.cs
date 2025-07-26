using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Mobile Optimization")]
    [SerializeField] private float minimumTouchSize = 44f; // Apple's recommendation
    [SerializeField] private bool autoResizeForMobile = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float animationSpeed = 8f;
    [SerializeField] private bool enableHapticFeedback = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;
    
    private Button button;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private AudioSource audioSource;
    private HapticFeedback hapticFeedback;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        
        // Get or create audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && clickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Find haptic feedback component
        hapticFeedback = FindFirstObjectByType<HapticFeedback>();
        
        OptimizeForMobile();
    }
    
    private void OptimizeForMobile()
    {
        if (!autoResizeForMobile) return;
        
        // Ensure minimum touch target size
        Vector2 sizeDelta = rectTransform.sizeDelta;
        
        if (sizeDelta.x < minimumTouchSize)
            sizeDelta.x = minimumTouchSize;
        
        if (sizeDelta.y < minimumTouchSize)
            sizeDelta.y = minimumTouchSize;
        
        rectTransform.sizeDelta = sizeDelta;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        
        // Scale down animation
        StopAllCoroutines();
        StartCoroutine(ScaleAnimation(pressScale));
        
        // Audio feedback
        PlayClickSound();
        
        // Haptic feedback
        TriggerHapticFeedback();
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;
        
        // Scale back up animation
        StopAllCoroutines();
        StartCoroutine(ScaleAnimation(1f));
    }
    
    private System.Collections.IEnumerator ScaleAnimation(float targetScale)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;
        
        float elapsed = 0f;
        float duration = 1f / animationSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            yield return null;
        }
        
        transform.localScale = endScale;
    }
    
    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
    
    private void TriggerHapticFeedback()
    {
        if (enableHapticFeedback && hapticFeedback != null)
        {
            hapticFeedback.TriggerVibration();
        }
    }
    
    private void OnDisable()
    {
        // Reset scale when disabled
        transform.localScale = originalScale;
    }
}