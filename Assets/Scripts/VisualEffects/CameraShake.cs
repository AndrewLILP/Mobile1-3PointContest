using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float defaultIntensity = 0.1f;
    [SerializeField] private float defaultDuration = 0.2f;
    [SerializeField] private bool enableShake = true;
    
    [Header("Shake Types")]
    [SerializeField] private AnimationCurve shakeCurve = new AnimationCurve(
        new Keyframe(0, 1), 
        new Keyframe(0.5f, 0.5f), 
        new Keyframe(1, 0)
    );
    [SerializeField] private float shakeFrequency = 25f;
    [SerializeField] private bool useRandomDirection = true;
    
    [Header("Mobile Optimization")]
    [SerializeField] private bool reducedShakeOnMobile = true;
    [SerializeField] private float mobileIntensityMultiplier = 0.7f;
    
    [Header("Different Shake Types")]
    [SerializeField] private ShakePreset[] shakePresets = new ShakePreset[]
    {
        new ShakePreset { name = "Score", intensity = 0.15f, duration = 0.3f, frequency = 30f },
        new ShakePreset { name = "Swish", intensity = 0.25f, duration = 0.5f, frequency = 20f },
        new ShakePreset { name = "Rim Hit", intensity = 0.08f, duration = 0.15f, frequency = 40f },
        new ShakePreset { name = "Miss", intensity = 0.05f, duration = 0.1f, frequency = 50f }
    };
    
    private Camera cameraComponent;
    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isShaking = false;
    private Coroutine currentShakeCoroutine;
    
    public System.Action OnShakeStarted;
    public System.Action OnShakeCompleted;
    
    [System.Serializable]
    public class ShakePreset
    {
        public string name;
        public float intensity;
        public float duration;
        public float frequency;
    }
    
    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        cameraTransform = transform;
        
        originalPosition = cameraTransform.localPosition;
        originalRotation = cameraTransform.localRotation;
        
        if (reducedShakeOnMobile && Application.isMobilePlatform)
        {
            AdjustForMobile();
        }
    }
    
    private void AdjustForMobile()
    {
        defaultIntensity *= mobileIntensityMultiplier;
        
        for (int i = 0; i < shakePresets.Length; i++)
        {
            shakePresets[i].intensity *= mobileIntensityMultiplier;
        }
    }
    
    public void ShakeCamera(float intensity = -1f, float duration = -1f)
    {
        if (!enableShake) return;
        
        if (intensity < 0) intensity = defaultIntensity;
        if (duration < 0) duration = defaultDuration;
        
        ShakeCamera(intensity, duration, shakeFrequency);
    }
    
    public void ShakeCamera(float intensity, float duration, float frequency)
    {
        if (!enableShake) return;
        
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
        }
        
        currentShakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration, frequency));
    }
    
    public void ShakeWithPreset(string presetName)
    {
        ShakePreset preset = System.Array.Find(shakePresets, p => p.name == presetName);
        if (preset != null)
        {
            ShakeCamera(preset.intensity, preset.duration, preset.frequency);
        }
        else
        {
            Debug.LogWarning($"Shake preset '{presetName}' not found!");
            ShakeCamera();
        }
    }
    
    public void ShakeForScore(bool isSwish = false)
    {
        if (isSwish)
        {
            ShakeWithPreset("Swish");
        }
        else
        {
            ShakeWithPreset("Score");
        }
    }
    
    public void ShakeForRimHit()
    {
        ShakeWithPreset("Rim Hit");
    }
    
    public void ShakeForMiss()
    {
        ShakeWithPreset("Miss");
    }
    
    private IEnumerator ShakeCoroutine(float intensity, float duration, float frequency)
    {
        isShaking = true;
        OnShakeStarted?.Invoke();
        
        float elapsed = 0f;
        Vector3 originalPos = cameraTransform.localPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            float progress = elapsed / duration;
            float currentIntensity = intensity * shakeCurve.Evaluate(progress);
            
            Vector3 shakeOffset;
            if (useRandomDirection)
            {
                shakeOffset = new Vector3(
                    Mathf.Sin(Time.time * frequency + Random.Range(0f, 100f)) * currentIntensity,
                    Mathf.Sin(Time.time * frequency * 1.1f + Random.Range(0f, 100f)) * currentIntensity,
                    Mathf.Sin(Time.time * frequency * 0.9f + Random.Range(0f, 100f)) * currentIntensity * 0.5f
                );
            }
            else
            {
                float angle = Time.time * frequency;
                shakeOffset = new Vector3(
                    Mathf.Cos(angle) * currentIntensity,
                    Mathf.Sin(angle) * currentIntensity,
                    0f
                );
            }
            
            cameraTransform.localPosition = originalPos + shakeOffset;
            
            yield return null;
        }
        
        cameraTransform.localPosition = originalPos;
        
        isShaking = false;
        currentShakeCoroutine = null;
        OnShakeCompleted?.Invoke();
    }
    
    public void StopShake()
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            currentShakeCoroutine = null;
        }
        
        cameraTransform.localPosition = originalPosition;
        cameraTransform.localRotation = originalRotation;
        
        isShaking = false;
    }
    
    public void SetShakeEnabled(bool enabled)
    {
        enableShake = enabled;
        
        if (!enabled && isShaking)
        {
            StopShake();
        }
    }
    
    public void UpdateOriginalPosition()
    {
        if (!isShaking)
        {
            originalPosition = cameraTransform.localPosition;
            originalRotation = cameraTransform.localRotation;
        }
    }
    
    public void PulseEffect(float intensity = 0.05f, float duration = 0.1f)
    {
        if (!enableShake) return;
        
        StartCoroutine(PulseCoroutine(intensity, duration));
    }
    
    private IEnumerator PulseCoroutine(float intensity, float duration)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        Vector3 targetPos = originalPos + cameraTransform.forward * intensity;
        
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            
            cameraTransform.localPosition = Vector3.Lerp(originalPos, targetPos, progress);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            
            cameraTransform.localPosition = Vector3.Lerp(targetPos, originalPos, progress);
            yield return null;
        }
        
        cameraTransform.localPosition = originalPos;
    }
    
    public void AddShakePreset(string name, float intensity, float duration, float frequency)
    {
        ShakePreset newPreset = new ShakePreset
        {
            name = name,
            intensity = intensity,
            duration = duration,
            frequency = frequency
        };
        
        System.Array.Resize(ref shakePresets, shakePresets.Length + 1);
        shakePresets[shakePresets.Length - 1] = newPreset;
    }
    
    public bool IsShaking => isShaking;
    public bool ShakeEnabled => enableShake;
    public float DefaultIntensity => defaultIntensity;
    public float DefaultDuration => defaultDuration;
    
    private void OnDrawGizmosSelected()
    {
        if (isShaking)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}