using UnityEngine;
using System.Collections;

public class LaunchChargeEffect : MonoBehaviour
{
    [Header("Charge Visual Effects")]
    [SerializeField] private ParticleSystem chargeParticles;
    [SerializeField] private LineRenderer chargeRing;
    [SerializeField] private Light chargeLight;
    [SerializeField] private Transform chargeCenter;
    
    [Header("Charge Settings")]
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float minChargeIntensity = 0.1f;
    [SerializeField] private float maxChargeIntensity = 1f;
    [SerializeField] private AnimationCurve chargeCurve = new AnimationCurve(
        new Keyframe(0, 0), 
        new Keyframe(0.7f, 0.8f), 
        new Keyframe(1, 1)
    );
    
    [Header("Visual Properties")]
    [SerializeField] private Color startChargeColor = Color.cyan;
    [SerializeField] private Color fullChargeColor = Color.yellow;
    [SerializeField] private Color overchargeColor = Color.red;
    [SerializeField] private float ringRadius = 1f;
    [SerializeField] private int ringSegments = 32;
    
    [Header("Audio")]
    [SerializeField] private AudioSource chargeAudio;
    [SerializeField] private AudioClip chargeStartSound;
    [SerializeField] private AudioClip chargingLoopSound;
    [SerializeField] private AudioClip chargeCompleteSound;
    [SerializeField] private AudioClip overchargeSound;
    
    [Header("Haptic Feedback")]
    [SerializeField] private HapticFeedback hapticFeedback;
    [SerializeField] private bool enableChargeHaptics = true;
    [SerializeField] private float hapticInterval = 0.5f;
    
    [Header("Mobile Optimization")]
    [SerializeField] private bool enableLowDetailMode = false;
    [SerializeField] private int mobileParticleCount = 15;
    [SerializeField] private int fullParticleCount = 30;
    
    private bool isCharging = false;
    private float chargeStartTime;
    private float lastHapticTime;
    private Coroutine chargeCoroutine;
    private BallLauncher ballLauncher;
    
    public System.Action OnChargeStarted;
    public System.Action<float> OnChargeProgress;
    public System.Action OnChargeComplete;
    public System.Action OnOvercharge;
    public System.Action OnChargeCancelled;
    
    private void Awake()
    {
        ballLauncher = FindFirstObjectByType<BallLauncher>();
        
        SetupChargeEffects();
        SetupAudio();
        
        if (hapticFeedback == null)
        {
            hapticFeedback = FindFirstObjectByType<HapticFeedback>();
        }
    }
    
    private void Start()
    {
        ConnectToBallLauncher();
        SetEffectsVisible(false);
    }
    
    private void SetupChargeEffects()
    {
        if (chargeCenter == null)
        {
            chargeCenter = transform;
        }
        
        if (chargeParticles != null)
        {
            var main = chargeParticles.main;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.maxParticles = enableLowDetailMode ? mobileParticleCount : fullParticleCount;
            main.startColor = startChargeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = chargeParticles.emission;
            emission.enabled = false;
            
            var shape = chargeParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = ringRadius * 0.8f;
            
            chargeParticles.Stop();
        }
        
        if (chargeRing == null)
        {
            GameObject ringObj = new GameObject("ChargeRing");
            ringObj.transform.SetParent(chargeCenter);
            ringObj.transform.localPosition = Vector3.zero;
            chargeRing = ringObj.AddComponent<LineRenderer>();
        }
        
        chargeRing.useWorldSpace = false;
        chargeRing.startWidth = 0.05f;
        chargeRing.endWidth = 0.05f;
        chargeRing.positionCount = ringSegments + 1;
        chargeRing.material = CreateChargeMaterial();
        chargeRing.enabled = false;
        
        CreateRingPositions();
        
        if (chargeLight == null)
        {
            GameObject lightObj = new GameObject("ChargeLight");
            lightObj.transform.SetParent(chargeCenter);
            lightObj.transform.localPosition = Vector3.zero;
            chargeLight = lightObj.AddComponent<Light>();
        }
        
        chargeLight.type = LightType.Point;
        chargeLight.color = startChargeColor;
        chargeLight.intensity = 0f;
        chargeLight.range = 3f;
        chargeLight.enabled = false;
    }
    
    private Material CreateChargeMaterial()
    {
        Material chargeMat = new Material(Shader.Find("Sprites/Default"));
        chargeMat.color = startChargeColor;
        return chargeMat;
    }
    
    private void CreateRingPositions()
    {
        Vector3[] positions = new Vector3[ringSegments + 1];
        
        for (int i = 0; i <= ringSegments; i++)
        {
            float angle = i * 2f * Mathf.PI / ringSegments;
            positions[i] = new Vector3(
                Mathf.Cos(angle) * ringRadius,
                0f,
                Mathf.Sin(angle) * ringRadius
            );
        }
        
        chargeRing.SetPositions(positions);
    }
    
    private void SetupAudio()
    {
        if (chargeAudio == null)
        {
            chargeAudio = gameObject.AddComponent<AudioSource>();
        }
        
        chargeAudio.playOnAwake = false;
        chargeAudio.loop = false;
    }
    
    private void ConnectToBallLauncher()
    {
        // This method can be called by the BallLauncher to connect events
    }
    
    public void StartCharge()
    {
        if (isCharging) return;
        
        isCharging = true;
        chargeStartTime = Time.time;
        lastHapticTime = 0f;
        
        SetEffectsVisible(true);
        
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
        }
        chargeCoroutine = StartCoroutine(ChargeUpdateCoroutine());
        
        PlayChargeStartSound();
        
        OnChargeStarted?.Invoke();
    }
    
    public void StopCharge()
    {
        if (!isCharging) return;
        
        isCharging = false;
        
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
        }
        
        SetEffectsVisible(false);
        
        if (chargeAudio != null && chargeAudio.isPlaying)
        {
            chargeAudio.Stop();
        }
        
        OnChargeCancelled?.Invoke();
    }
    
    public void CompleteCharge()
    {
        if (!isCharging) return;
        
        PlayChargeCompleteSound();
        
        if (enableChargeHaptics && hapticFeedback != null)
        {
            hapticFeedback.TriggerVibration();
        }
        
        StopCharge();
        OnChargeComplete?.Invoke();
    }
    
    private IEnumerator ChargeUpdateCoroutine()
    {
        while (isCharging)
        {
            float chargeTime = Time.time - chargeStartTime;
            float chargeProgress = Mathf.Clamp01(chargeTime / maxChargeTime);
            float curveProgress = chargeCurve.Evaluate(chargeProgress);
            
            UpdateChargeVisuals(chargeProgress, curveProgress);
            
            if (chargeProgress >= 1f)
            {
                HandleOvercharge();
            }
            
            if (enableChargeHaptics && hapticFeedback != null)
            {
                if (Time.time - lastHapticTime > hapticInterval)
                {
                    if (chargeProgress > 0.3f)
                    {
                        hapticFeedback.TriggerVibration();
                        lastHapticTime = Time.time;
                    }
                }
            }
            
            OnChargeProgress?.Invoke(chargeProgress);
            
            yield return null;
        }
    }
    
    private void UpdateChargeVisuals(float progress, float curveProgress)
    {
        Color currentColor;
        if (progress < 0.8f)
        {
            currentColor = Color.Lerp(startChargeColor, fullChargeColor, progress / 0.8f);
        }
        else
        {
            float overchargeProgress = (progress - 0.8f) / 0.2f;
            currentColor = Color.Lerp(fullChargeColor, overchargeColor, overchargeProgress);
        }
        
        if (chargeParticles != null)
        {
            var main = chargeParticles.main;
            main.startColor = currentColor;
            
            var emission = chargeParticles.emission;
            emission.rateOverTime = curveProgress * 50f;
            
            if (!chargeParticles.isPlaying && progress > 0.1f)
            {
                chargeParticles.Play();
            }
        }
        
        if (chargeRing != null)
        {
            chargeRing.material.color = currentColor;
            
            float ringScale = 1f + (curveProgress * 0.3f);
            chargeRing.transform.localScale = Vector3.one * ringScale;
            
            float ringWidth = 0.02f + (curveProgress * 0.08f);
            chargeRing.startWidth = ringWidth;
            chargeRing.endWidth = ringWidth;
        }
        
        if (chargeLight != null)
        {
            chargeLight.color = currentColor;
            chargeLight.intensity = curveProgress * 2f;
        }
    }
    
    private void HandleOvercharge()
    {
        if (chargeParticles != null)
        {
            var main = chargeParticles.main;
            main.startColor = overchargeColor;
        }
        
        if (overchargeSound != null && chargeAudio != null)
        {
            chargeAudio.PlayOneShot(overchargeSound);
        }
        
        OnOvercharge?.Invoke();
    }
    
    private void SetEffectsVisible(bool visible)
    {
        if (chargeParticles != null)
        {
            if (visible)
            {
                var emission = chargeParticles.emission;
                emission.enabled = true;
            }
            else
            {
                chargeParticles.Stop();
            }
        }
        
        if (chargeRing != null)
        {
            chargeRing.enabled = visible;
        }
        
        if (chargeLight != null)
        {
            chargeLight.enabled = visible;
            if (!visible) chargeLight.intensity = 0f;
        }
    }
    
    private void PlayChargeStartSound()
    {
        if (chargeStartSound != null && chargeAudio != null)
        {
            chargeAudio.PlayOneShot(chargeStartSound);
        }
        
        if (chargingLoopSound != null && chargeAudio != null)
        {
            chargeAudio.clip = chargingLoopSound;
            chargeAudio.loop = true;
            chargeAudio.Play();
        }
    }
    
    private void PlayChargeCompleteSound()
    {
        if (chargeCompleteSound != null && chargeAudio != null)
        {
            chargeAudio.Stop();
            chargeAudio.loop = false;
            chargeAudio.PlayOneShot(chargeCompleteSound);
        }
    }
    
    public void SetChargePosition(Vector3 position)
    {
        if (chargeCenter != null)
        {
            chargeCenter.position = position;
        }
    }
    
    public void SetLowDetailMode(bool lowDetail)
    {
        enableLowDetailMode = lowDetail;
        
        if (chargeParticles != null)
        {
            var main = chargeParticles.main;
            main.maxParticles = lowDetail ? mobileParticleCount : fullParticleCount;
        }
    }
    
    public void SetRingRadius(float radius)
    {
        ringRadius = radius;
        CreateRingPositions();
    }
    
    public bool IsCharging => isCharging;
    public float ChargeProgress => isCharging ? Mathf.Clamp01((Time.time - chargeStartTime) / maxChargeTime) : 0f;
    public float ChargeTimeRemaining => isCharging ? Mathf.Max(0f, maxChargeTime - (Time.time - chargeStartTime)) : 0f;
    
    private void OnDestroy()
    {
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
        }
    }
}