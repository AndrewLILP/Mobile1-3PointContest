using UnityEngine;
using System.Collections;

public class HoopVisualEffects : MonoBehaviour
{
    [Header("Score Celebration Effects")]
    [SerializeField] private ParticleSystem scoreFireworks;
    [SerializeField] private ParticleSystem swishStars;
    [SerializeField] private ParticleSystem confettiEffect;
    [SerializeField] private float celebrationDuration = 2f;
    
    [Header("Hoop Glow Effects")]
    [SerializeField] private Light hoopLight;
    [SerializeField] private Renderer hoopRimRenderer;
    [SerializeField] private Color scoreGlowColor = Color.green;
    [SerializeField] private Color swishGlowColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private float glowDuration = 1f;
    [SerializeField] private AnimationCurve glowCurve = new AnimationCurve(
        new Keyframe(0, 0), new Keyframe(0.2f, 1), new Keyframe(1, 0)
    );
    
    [Header("Rim Animation")]
    [SerializeField] private Transform hoopRim;
    [SerializeField] private float bounceScale = 1.1f;
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0, 1), new Keyframe(0.5f, 1.1f), new Keyframe(1, 1)
    );
    
    [Header("Net Animation")]
    [SerializeField] private Transform hoopNet;
    [SerializeField] private float netSwayAmount = 0.1f;
    [SerializeField] private float netSwayDuration = 1f;
    
    [Header("Impact Effects")]
    [SerializeField] private ParticleSystem rimSparks;
    [SerializeField] private ParticleSystem backboardRipples;
    [SerializeField] private AudioSource impactAudio;
    [SerializeField] private AudioClip[] rimHitSounds;
    [SerializeField] private AudioClip[] backboardHitSounds;
    
    [Header("Screen Effects")]
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 0.2f;
    
    [Header("Mobile Optimization")]
    [SerializeField] private int maxParticles = 50;
    [SerializeField] private bool enableLowDetailMode = false;
    
    private Hoop hoopScript;
    private CameraShake cameraShake;
    private Material hoopRimMaterial;
    private Material originalRimMaterial;
    private Vector3 originalRimScale;
    private Vector3 originalNetPosition;
    private bool effectsEnabled = true;
    
    public System.Action<bool> OnScoreEffectStarted;
    public System.Action OnScoreEffectCompleted;
    public System.Action OnRimImpact;
    public System.Action OnBackboardImpact;
    
    private void Awake()
    {
        hoopScript = GetComponent<Hoop>();
        cameraShake = FindFirstObjectByType<CameraShake>();
        
        SetupHoopComponents();
        SetupParticleEffects();
        SetupLightEffects();
    }
    
    private void Start()
    {
        ConnectToHoopEvents();
        
        if (hoopRim != null)
            originalRimScale = hoopRim.localScale;
        if (hoopNet != null)
            originalNetPosition = hoopNet.localPosition;
    }
    
    private void SetupHoopComponents()
    {
        if (hoopRimRenderer != null)
        {
            originalRimMaterial = hoopRimRenderer.material;
            hoopRimMaterial = new Material(originalRimMaterial);
            hoopRimRenderer.material = hoopRimMaterial;
        }
        
        if (hoopLight == null)
        {
            GameObject lightObj = new GameObject("HoopLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 0.5f;
            hoopLight = lightObj.AddComponent<Light>();
        }
        
        hoopLight.type = LightType.Point;
        hoopLight.intensity = 0f;
        hoopLight.range = 5f;
        hoopLight.enabled = false;
    }
    
    private void SetupParticleEffects()
    {
        if (scoreFireworks != null)
        {
            var main = scoreFireworks.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 8f;
            main.maxParticles = enableLowDetailMode ? 20 : maxParticles;
            main.startColor = Color.white;
            
            var emission = scoreFireworks.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)(enableLowDetailMode ? 15 : 30)),
                new ParticleSystem.Burst(0.5f, (short)(enableLowDetailMode ? 10 : 20))
            });
            
            var shape = scoreFireworks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            
            scoreFireworks.Stop();
        }
        
        if (swishStars != null)
        {
            var main = swishStars.main;
            main.startLifetime = 2f;
            main.startSpeed = 3f;
            main.maxParticles = enableLowDetailMode ? 10 : 25;
            main.startColor = swishGlowColor;
            
            var emission = swishStars.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)(enableLowDetailMode ? 8 : 15))
            });
            
            swishStars.Stop();
        }
        
        if (confettiEffect != null)
        {
            var main = confettiEffect.main;
            main.startLifetime = 3f;
            main.startSpeed = 5f;
            main.maxParticles = enableLowDetailMode ? 15 : 40;
            
            var emission = confettiEffect.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)(enableLowDetailMode ? 20 : 40))
            });
            
            confettiEffect.Stop();
        }
        
        if (rimSparks != null)
        {
            var main = rimSparks.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 8f;
            main.maxParticles = 15;
            main.startColor = Color.yellow;
            
            var emission = rimSparks.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 8)
            });
            
            rimSparks.Stop();
        }
        
        if (backboardRipples != null)
        {
            var main = backboardRipples.main;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.maxParticles = 10;
            main.startColor = Color.blue;
            
            var emission = backboardRipples.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 5)
            });
            
            backboardRipples.Stop();
        }
    }
    
    private void SetupLightEffects()
    {
        if (hoopLight != null)
        {
            hoopLight.color = scoreGlowColor;
            hoopLight.intensity = 0f;
            hoopLight.enabled = false;
        }
    }
    
    private void ConnectToHoopEvents()
    {
        if (hoopScript != null)
        {
            hoopScript.OnScore += HandleScore;
        }
        
        RimDetector rimDetector = GetComponentInChildren<RimDetector>();
        if (rimDetector == null && hoopRim != null)
        {
            rimDetector = hoopRim.GetComponent<RimDetector>();
        }
        
        BackboardDetector backboardDetector = GetComponentInChildren<BackboardDetector>();
    }
    
    private void HandleScore(int points, bool isSwish)
    {
        if (!effectsEnabled) return;
        
        StartCoroutine(PlayScoreEffects(isSwish));
        OnScoreEffectStarted?.Invoke(isSwish);
    }
    
    private IEnumerator PlayScoreEffects(bool isSwish)
    {
        Color glowColor = isSwish ? swishGlowColor : scoreGlowColor;
        
        StartCoroutine(AnimateRimBounce());
        StartCoroutine(AnimateNetSway());
        StartCoroutine(AnimateHoopGlow(glowColor));
        
        if (isSwish)
        {
            PlaySwishEffects();
        }
        else
        {
            PlayNormalScoreEffects();
        }
        
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeDuration);
        }
        
        yield return new WaitForSeconds(celebrationDuration);
        
        OnScoreEffectCompleted?.Invoke();
    }
    
    private void PlaySwishEffects()
    {
        if (swishStars != null)
        {
            swishStars.Play();
        }
        
        if (confettiEffect != null && !enableLowDetailMode)
        {
            confettiEffect.Play();
        }
    }
    
    private void PlayNormalScoreEffects()
    {
        if (scoreFireworks != null)
        {
            scoreFireworks.Play();
        }
    }
    
    private IEnumerator AnimateRimBounce()
    {
        if (hoopRim == null) yield break;
        
        float elapsed = 0f;
        Vector3 startScale = originalRimScale;
        
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / bounceDuration;
            float bounceValue = bounceCurve.Evaluate(progress);
            
            hoopRim.localScale = startScale * bounceValue;
            
            yield return null;
        }
        
        hoopRim.localScale = originalRimScale;
    }
    
    private IEnumerator AnimateNetSway()
    {
        if (hoopNet == null) yield break;
        
        float elapsed = 0f;
        Vector3 startPos = originalNetPosition;
        
        while (elapsed < netSwayDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / netSwayDuration;
            
            float swayX = Mathf.Sin(progress * Mathf.PI * 4) * netSwayAmount * (1 - progress);
            float swayZ = Mathf.Cos(progress * Mathf.PI * 3) * netSwayAmount * 0.5f * (1 - progress);
            
            hoopNet.localPosition = startPos + new Vector3(swayX, 0, swayZ);
            
            yield return null;
        }
        
        hoopNet.localPosition = originalNetPosition;
    }
    
    private IEnumerator AnimateHoopGlow(Color color)
    {
        if (hoopLight == null) yield break;
        
        hoopLight.color = color;
        hoopLight.enabled = true;
        
        if (hoopRimMaterial != null)
        {
            hoopRimMaterial.EnableKeyword("_EMISSION");
        }
        
        float elapsed = 0f;
        
        while (elapsed < glowDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / glowDuration;
            float intensity = glowCurve.Evaluate(progress);
            
            hoopLight.intensity = intensity * 3f;
            
            if (hoopRimMaterial != null)
            {
                Color emissionColor = color * intensity;
                hoopRimMaterial.SetColor("_EmissionColor", emissionColor);
            }
            
            yield return null;
        }
        
        hoopLight.enabled = false;
        hoopLight.intensity = 0f;
        
        if (hoopRimMaterial != null)
        {
            hoopRimMaterial.SetColor("_EmissionColor", Color.black);
        }
    }
    
    public void PlayRimImpactEffect(Vector3 impactPoint)
    {
        if (!effectsEnabled) return;
        
        if (rimSparks != null)
        {
            rimSparks.transform.position = impactPoint;
            rimSparks.Play();
        }
        
        if (impactAudio != null && rimHitSounds.Length > 0)
        {
            AudioClip randomHit = rimHitSounds[Random.Range(0, rimHitSounds.Length)];
            impactAudio.PlayOneShot(randomHit);
        }
        
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity * 0.5f, shakeDuration * 0.5f);
        }
        
        OnRimImpact?.Invoke();
    }
    
    public void PlayBackboardImpactEffect(Vector3 impactPoint)
    {
        if (!effectsEnabled) return;
        
        if (backboardRipples != null)
        {
            backboardRipples.transform.position = impactPoint;
            backboardRipples.Play();
        }
        
        if (impactAudio != null && backboardHitSounds.Length > 0)
        {
            AudioClip randomHit = backboardHitSounds[Random.Range(0, backboardHitSounds.Length)];
            impactAudio.PlayOneShot(randomHit);
        }
        
        OnBackboardImpact?.Invoke();
    }
    
    public void SetEffectsEnabled(bool enabled)
    {
        effectsEnabled = enabled;
        
        if (!enabled)
        {
            StopAllEffects();
        }
    }
    
    public void SetLowDetailMode(bool lowDetail)
    {
        enableLowDetailMode = lowDetail;
        AdjustParticleDetailLevel();
    }
    
    private void AdjustParticleDetailLevel()
    {
        ParticleSystem[] allParticles = { scoreFireworks, swishStars, confettiEffect, rimSparks, backboardRipples };
        
        foreach (var ps in allParticles)
        {
            if (ps == null) continue;
            
            var main = ps.main;
            main.maxParticles = enableLowDetailMode ? main.maxParticles / 2 : maxParticles;
        }
    }
    
    private void StopAllEffects()
    {
        StopAllCoroutines();
        
        if (scoreFireworks != null) scoreFireworks.Stop();
        if (swishStars != null) swishStars.Stop();
        if (confettiEffect != null) confettiEffect.Stop();
        if (rimSparks != null) rimSparks.Stop();
        if (backboardRipples != null) backboardRipples.Stop();
        
        if (hoopRim != null) hoopRim.localScale = originalRimScale;
        if (hoopNet != null) hoopNet.localPosition = originalNetPosition;
        
        if (hoopLight != null)
        {
            hoopLight.enabled = false;
            hoopLight.intensity = 0f;
        }
        
        if (hoopRimMaterial != null)
        {
            hoopRimMaterial.SetColor("_EmissionColor", Color.black);
        }
    }
    
    public bool EffectsEnabled => effectsEnabled;
    public bool LowDetailMode => enableLowDetailMode;
    
    private void OnDestroy()
    {
        if (hoopRimMaterial != null)
        {
            Destroy(hoopRimMaterial);
        }
    }
}