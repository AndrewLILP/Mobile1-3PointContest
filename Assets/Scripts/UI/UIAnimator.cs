using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float scaleAnimDuration = 0.4f;
    [SerializeField] private float slideDistance = 100f;
    
    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve fadeInCurve = new AnimationCurve(
        new Keyframe(0, 0), 
        new Keyframe(1, 1)
    );
    [SerializeField] private AnimationCurve scaleInCurve = new AnimationCurve(
        new Keyframe(0, 0, 0, 2), 
        new Keyframe(0.5f, 1.1f, 0, 0), 
        new Keyframe(1, 1, -2, 0)
    );
    [SerializeField] private AnimationCurve slideInCurve = new AnimationCurve(
        new Keyframe(0, 0), 
        new Keyframe(1, 1)
    );
    
    [Header("Score Animation")]
    [SerializeField] private float scorePunchScale = 1.2f;
    [SerializeField] private float scorePunchDuration = 0.2f;
    [SerializeField] private Color scoreFlashColor = Color.yellow;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        // Add CanvasGroup if not present
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    #region Panel Animations
    
    public void FadeIn(System.Action onComplete = null)
    {
        StartCoroutine(FadeCoroutine(0f, 1f, fadeInDuration, fadeInCurve, onComplete));
    }
    
    public void FadeOut(System.Action onComplete = null)
    {
        StartCoroutine(FadeCoroutine(1f, 0f, fadeOutDuration, AnimationCurve.EaseInOut(0, 1, 1, 0), onComplete));
    }
    
    public void ScaleIn(System.Action onComplete = null)
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(ScaleCoroutine(Vector3.zero, Vector3.one, scaleAnimDuration, scaleInCurve, onComplete));
    }
    
    public void SlideInFromLeft(System.Action onComplete = null)
    {
        Vector3 startPos = rectTransform.anchoredPosition;
        Vector3 offScreenPos = startPos + Vector3.left * slideDistance;
        rectTransform.anchoredPosition = offScreenPos;
        
        StartCoroutine(MoveCoroutine(offScreenPos, startPos, fadeInDuration, slideInCurve, onComplete));
    }
    
    public void SlideInFromRight(System.Action onComplete = null)
    {
        Vector3 startPos = rectTransform.anchoredPosition;
        Vector3 offScreenPos = startPos + Vector3.right * slideDistance;
        rectTransform.anchoredPosition = offScreenPos;
        
        StartCoroutine(MoveCoroutine(offScreenPos, startPos, fadeInDuration, slideInCurve, onComplete));
    }
    
    public void SlideInFromTop(System.Action onComplete = null)
    {
        Vector3 startPos = rectTransform.anchoredPosition;
        Vector3 offScreenPos = startPos + Vector3.up * slideDistance;
        rectTransform.anchoredPosition = offScreenPos;
        
        StartCoroutine(MoveCoroutine(offScreenPos, startPos, fadeInDuration, slideInCurve, onComplete));
    }
    
    #endregion
    
    #region Score Animations
    
    public void AnimateScoreUpdate(TextMeshProUGUI scoreText, int newScore)
    {
        StartCoroutine(ScoreUpdateCoroutine(scoreText, newScore));
    }
    
    public void PunchScale(float scale = -1f, float duration = -1f)
    {
        if (scale < 0) scale = scorePunchScale;
        if (duration < 0) duration = scorePunchDuration;
        
        StartCoroutine(PunchScaleCoroutine(scale, duration));
    }
    
    public void FlashColor(Image image, Color flashColor, float duration = 0.2f)
    {
        StartCoroutine(FlashColorCoroutine(image, flashColor, duration));
    }
    
    public void FlashColor(TextMeshProUGUI text, Color flashColor, float duration = 0.2f)
    {
        StartCoroutine(FlashTextColorCoroutine(text, flashColor, duration));
    }
    
    #endregion
    
    #region Progress Bar Animations
    
    public void AnimateProgressBar(Slider slider, float targetValue, float duration = 0.5f)
    {
        StartCoroutine(ProgressBarCoroutine(slider, targetValue, duration));
    }
    
    public void AnimateFillImage(Image fillImage, float targetFill, float duration = 0.5f)
    {
        StartCoroutine(FillImageCoroutine(fillImage, targetFill, duration));
    }
    
    #endregion
    
    #region Coroutines
    
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, AnimationCurve curve, System.Action onComplete)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, curve.Evaluate(progress));
            
            canvasGroup.alpha = alpha;
            
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }
    
    private IEnumerator ScaleCoroutine(Vector3 startScale, Vector3 endScale, float duration, AnimationCurve curve, System.Action onComplete)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            Vector3 scale = Vector3.Lerp(startScale, endScale, curve.Evaluate(progress));
            
            transform.localScale = scale;
            
            yield return null;
        }
        
        transform.localScale = endScale;
        onComplete?.Invoke();
    }
    
    private IEnumerator MoveCoroutine(Vector3 startPos, Vector3 endPos, float duration, AnimationCurve curve, System.Action onComplete)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            Vector3 position = Vector3.Lerp(startPos, endPos, curve.Evaluate(progress));
            
            rectTransform.anchoredPosition = position;
            
            yield return null;
        }
        
        rectTransform.anchoredPosition = endPos;
        onComplete?.Invoke();
    }
    
    private IEnumerator ScoreUpdateCoroutine(TextMeshProUGUI scoreText, int newScore)
    {
        // Parse current score
        string currentText = scoreText.text;
        int currentScore = 0;
        
        // Extract number from text (assumes format like "Total: 5" or just "5")
        string[] parts = currentText.Split(':');
        if (parts.Length > 1)
        {
            int.TryParse(parts[1].Trim(), out currentScore);
        }
        else
        {
            int.TryParse(currentText, out currentScore);
        }
        
        // Animate from current to new score
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(currentScore, newScore, progress));
            
            // Update text while preserving format
            if (parts.Length > 1)
            {
                scoreText.text = $"{parts[0]}: {displayScore}";
            }
            else
            {
                scoreText.text = displayScore.ToString();
            }
            
            yield return null;
        }
        
        // Ensure final value is exact
        if (parts.Length > 1)
        {
            scoreText.text = $"{parts[0]}: {newScore}";
        }
        else
        {
            scoreText.text = newScore.ToString();
        }
        
        // Add punch effect
        StartCoroutine(PunchScaleCoroutine(scorePunchScale, scorePunchDuration));
    }
    
    private IEnumerator PunchScaleCoroutine(float punchScale, float duration)
    {
        Vector3 originalScale = transform.localScale;
        Vector3 punchScaleVector = originalScale * punchScale;
        
        // Scale up
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / halfDuration;
            
            transform.localScale = Vector3.Lerp(originalScale, punchScaleVector, progress);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / halfDuration;
            
            transform.localScale = Vector3.Lerp(punchScaleVector, originalScale, progress);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    private IEnumerator FlashColorCoroutine(Image image, Color flashColor, float duration)
    {
        Color originalColor = image.color;
        
        // Flash to new color
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / halfDuration;
            
            image.color = Color.Lerp(originalColor, flashColor, progress);
            yield return null;
        }
        
        // Flash back to original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / halfDuration;
            
            image.color = Color.Lerp(flashColor, originalColor, progress);
            yield return null;
        }
        
        image.color = originalColor;
    }
    
    private IEnumerator FlashTextColorCoroutine(TextMeshProUGUI text, Color flashColor, float duration)
    {
        Color originalColor = text.color;
        
        // Flash to new color
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / halfDuration;
            
            text.color = Color.Lerp(originalColor, flashColor, progress);
            yield return null;
        }
        
        // Flash back to original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / halfDuration;
            
            text.color = Color.Lerp(flashColor, originalColor, progress);
            yield return null;
        }
        
        text.color = originalColor;
    }
    
    private IEnumerator ProgressBarCoroutine(Slider slider, float targetValue, float duration)
    {
        float startValue = slider.value;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            slider.value = Mathf.Lerp(startValue, targetValue, slideInCurve.Evaluate(progress));
            yield return null;
        }
        
        slider.value = targetValue;
    }
    
    private IEnumerator FillImageCoroutine(Image fillImage, float targetFill, float duration)
    {
        float startFill = fillImage.fillAmount;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            fillImage.fillAmount = Mathf.Lerp(startFill, targetFill, slideInCurve.Evaluate(progress));
            yield return null;
        }
        
        fillImage.fillAmount = targetFill;
    }
    
    #endregion
    
    #region Public Utility Methods
    
    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }
    
    public void SetInteractable(bool interactable)
    {
        canvasGroup.interactable = interactable;
    }
    
    public void SetBlocksRaycasts(bool blocks)
    {
        canvasGroup.blocksRaycasts = blocks;
    }
    
    #endregion
}