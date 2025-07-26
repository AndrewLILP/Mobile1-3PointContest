using UnityEngine;
using TMPro;

public class ScorePopup : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private float moveDistance = 100f;
    [SerializeField] private AnimationCurve moveCurve = new AnimationCurve(
        new Keyframe(0, 0, 0, 2), 
        new Keyframe(0.5f, 1.1f, 0, 0), 
        new Keyframe(1, 1, -2, 0)
    );
    [SerializeField] private AnimationCurve alphaCurve = new AnimationCurve(
        new Keyframe(0, 1), 
        new Keyframe(1, 0)
    );
    
    private Vector3 startPosition;
    
    private void Awake()
    {
        // Get components if not assigned
        if (scoreText == null)
            scoreText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        startPosition = transform.localPosition;
    }
    
    public void Initialize(string text, Color color)
    {
        if (scoreText != null)
        {
            scoreText.text = text;
            scoreText.color = color;
        }
        
        StartAnimation();
    }
    
    private void StartAnimation()
    {
        StartCoroutine(AnimatePopup());
    }
    
    private System.Collections.IEnumerator AnimatePopup()
    {
        Vector3 endPosition = startPosition + Vector3.up * moveDistance;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / animationDuration;
            
            // Animate position
            Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, moveCurve.Evaluate(progress));
            transform.localPosition = currentPos;
            
            // Animate alpha
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alphaCurve.Evaluate(progress);
            }
            
            yield return null;
        }
        
        // Destroy popup when animation completes
        Destroy(gameObject);
    }
}