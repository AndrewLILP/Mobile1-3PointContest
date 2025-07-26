using UnityEngine;

public class BallVisualEffects : MonoBehaviour
{
    [Header("Ball Trail Effects")]
    [SerializeField] private TrailRenderer ballTrail;
    [SerializeField] private float trailWidth = 0.1f;
    [SerializeField] private float trailTime = 0.5f;
    [SerializeField] private Color trailColor = Color.white;
    [SerializeField] private AnimationCurve trailWidthCurve = new AnimationCurve(
        new Keyframe(0, 1), new Keyframe(1, 0)
    );
    
    [Header("Ball Glow Effect")]
    [SerializeField] private GameObject glowEffect;
    [SerializeField] private Light ballLight;
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private bool enableDynamicGlow = true;
    
    [Header("Speed-Based Effects")]
    [SerializeField] private float minSpeedForEffects = 2f;
    [SerializeField] private float maxSpeedForEffects = 15f;
    [SerializeField] private ParticleSystem speedParticles;
    [SerializeField] private AudioSource whooshAudio;
    [SerializeField] private AudioClip[] whooshSounds;
    
    [Header("Launch Charge Effect")]
    [SerializeField] private ParticleSystem chargeEffect;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private Color chargeColor = Color.cyan;
    
    [Header("Impact Effects")]
    [SerializeField] private ParticleSystem impactSparks;
    [SerializeField] private AudioClip[] impactSounds;
    [SerializeField] private float impactForceThreshold = 5f;
    
    // Components
    private Rigidbody ballRigidbody;
    private MeshRenderer ballRenderer;
    private Material ballMaterial;
    private Material originalMaterial;
    private Material trailMaterial; // ✅ ADD: Separate trail material
    
    // State
    private bool isCharging = false;
    private float chargeStartTime;
    private Vector3 lastVelocity;
    private bool effectsActive = true;
    
    // Events
    public System.Action<float> OnSpeedChanged; // normalized speed (0-1)
    public System.Action OnLaunchCharged;
    public System.Action OnBallImpact;
    
    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        ballRenderer = GetComponent<MeshRenderer>();
        
        // ✅ FIX: Properly handle material creation
        if (ballRenderer != null)
        {
            originalMaterial = ballRenderer.sharedMaterial; // Use sharedMaterial for reading
            
            // Only create new material instance if we're not in prefab mode
            if (Application.isPlaying && !IsPrefabAsset())
            {
                ballMaterial = new Material(originalMaterial);
                ballRenderer.material = ballMaterial; // This creates an instance
            }
        }
        
        SetupTrailRenderer();
        SetupGlowEffect();
        SetupParticleEffects();
    }
    
    // ✅ ADD: Helper method to check if this is a prefab asset
    private bool IsPrefabAsset()
    {
        return gameObject.scene.name == null;
    }
    
    private void Start()
    {
        ConnectToGameSystems();
    }
    
    private void Update()
    {
        if (!effectsActive) return;
        
        UpdateSpeedBasedEffects();
        UpdateGlowEffect();
        UpdateChargeEffect();
    }
    
    private void SetupTrailRenderer()
    {
        if (ballTrail == null)
        {
            GameObject trailObj = new GameObject("BallTrail");
            trailObj.transform.SetParent(transform);
            trailObj.transform.localPosition = Vector3.zero;
            ballTrail = trailObj.AddComponent<TrailRenderer>();
        }
        
        ballTrail.time = trailTime;
        ballTrail.startWidth = trailWidth;
        ballTrail.endWidth = 0f;
        ballTrail.widthCurve = trailWidthCurve;
        
        // ✅ FIX: Create trail material properly
        trailMaterial = CreateTrailMaterial();
        ballTrail.material = trailMaterial;
        ballTrail.enabled = false;
    }
    
    private void SetupGlowEffect()
    {
        if (glowEffect == null)
        {
            glowEffect = new GameObject("BallGlow");
            glowEffect.transform.SetParent(transform);
            glowEffect.transform.localPosition = Vector3.zero;
        }
        
        if (ballLight == null)
        {
            ballLight = glowEffect.AddComponent<Light>();
        }
        
        ballLight.type = LightType.Point;
        ballLight.color = glowColor;
        ballLight.intensity = 0f;
        ballLight.range = 3f;
        ballLight.enabled = false;
    }
    
    private void SetupParticleEffects()
    {
        if (speedParticles != null)
        {
            var main = speedParticles.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 2f;
            main.maxParticles = 20;
            main.startColor = Color.white;
            
            var emission = speedParticles.emission;
            emission.enabled = false;
            
            var shape = speedParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
        }
        
        if (chargeEffect != null)
        {
            var main = chargeEffect.main;
            main.startLifetime = 1f;
            main.startSpeed = 1f;
            main.maxParticles = 15;
            main.startColor = chargeColor;
            
            var emission = chargeEffect.emission;
            emission.enabled = false;
            
            chargeEffect.Stop();
        }
        
        if (impactSparks != null)
        {
            var main = impactSparks.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 5f;
            main.maxParticles = 10;
            
            var emission = impactSparks.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 10)
            });
            
            impactSparks.Stop();
        }
    }
    
    private Material CreateTrailMaterial()
    {
        Material trailMat = new Material(Shader.Find("Sprites/Default"));
        trailMat.color = trailColor;
        return trailMat;
    }
    
    private void ConnectToGameSystems()
    {
        BallLauncher launcher = FindFirstObjectByType<BallLauncher>();
        if (launcher != null)
        {
            launcher.OnBallLaunched += HandleBallLaunched;
        }
    }
    
    private void UpdateSpeedBasedEffects()
    {
        if (ballRigidbody == null) return;
        
        Vector3 velocity = ballRigidbody.linearVelocity;
        float speed = velocity.magnitude;
        float normalizedSpeed = Mathf.Clamp01((speed - minSpeedForEffects) / (maxSpeedForEffects - minSpeedForEffects));
        
        bool shouldShowTrail = speed > minSpeedForEffects;
        if (ballTrail.enabled != shouldShowTrail)
        {
            ballTrail.enabled = shouldShowTrail;
        }
        
        if (shouldShowTrail && trailMaterial != null)
        {
            ballTrail.startWidth = trailWidth * normalizedSpeed;
            Color trailColorWithAlpha = trailColor;
            trailColorWithAlpha.a = normalizedSpeed;
            trailMaterial.color = trailColorWithAlpha; // ✅ FIX: Use our trail material instance
        }
        
        if (speedParticles != null)
        {
            var emission = speedParticles.emission;
            emission.enabled = normalizedSpeed > 0.3f;
            
            if (emission.enabled)
            {
                emission.rateOverTime = normalizedSpeed * 20f;
            }
        }
        
        if (whooshAudio != null && whooshSounds.Length > 0 && normalizedSpeed > 0.7f)
        {
            if (!whooshAudio.isPlaying)
            {
                AudioClip randomWhoosh = whooshSounds[Random.Range(0, whooshSounds.Length)];
                whooshAudio.PlayOneShot(randomWhoosh, normalizedSpeed);
            }
        }
        
        OnSpeedChanged?.Invoke(normalizedSpeed);
        lastVelocity = velocity;
    }
    
    private void UpdateGlowEffect()
    {
        if (!enableDynamicGlow || ballLight == null) return;
        
        float speed = ballRigidbody != null ? ballRigidbody.linearVelocity.magnitude : 0f;
        float normalizedSpeed = Mathf.Clamp01((speed - minSpeedForEffects) / (maxSpeedForEffects - minSpeedForEffects));
        
        bool shouldGlow = normalizedSpeed > 0.2f;
        ballLight.enabled = shouldGlow;
        
        if (shouldGlow)
        {
            ballLight.intensity = glowIntensity * normalizedSpeed;
            ballLight.color = Color.Lerp(glowColor, Color.white, normalizedSpeed);
        }
    }
    
    private void UpdateChargeEffect()
    {
        if (!isCharging || chargeEffect == null) return;
        
        float chargeProgress = (Time.time - chargeStartTime) / maxChargeTime;
        chargeProgress = Mathf.Clamp01(chargeProgress);
        
        var main = chargeEffect.main;
        main.startColor = Color.Lerp(chargeColor, Color.white, chargeProgress);
        
        var emission = chargeEffect.emission;
        emission.rateOverTime = chargeProgress * 30f;
        
        if (chargeProgress >= 1f)
        {
            OnLaunchCharged?.Invoke();
        }
    }
    
    public void StartChargeEffect()
    {
        isCharging = true;
        chargeStartTime = Time.time;
        
        if (chargeEffect != null)
        {
            chargeEffect.Play();
            var emission = chargeEffect.emission;
            emission.enabled = true;
        }
    }
    
    public void StopChargeEffect()
    {
        isCharging = false;
        
        if (chargeEffect != null)
        {
            chargeEffect.Stop();
            var emission = chargeEffect.emission;
            emission.enabled = false;
        }
    }
    
    private void HandleBallLaunched()
    {
        StopChargeEffect();
        
        if (ballLight != null)
        {
            ballLight.enabled = true;
            ballLight.intensity = glowIntensity;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!effectsActive) return;
        
        float impactForce = collision.impulse.magnitude;
        
        if (impactForce > impactForceThreshold)
        {
            PlayImpactEffect(collision.contacts[0].point, collision.contacts[0].normal);
            OnBallImpact?.Invoke();
        }
    }
    
    private void PlayImpactEffect(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (impactSparks != null)
        {
            impactSparks.transform.position = impactPoint;
            impactSparks.transform.rotation = Quaternion.LookRotation(impactNormal);
            impactSparks.Play();
        }
        
        if (whooshAudio != null && impactSounds.Length > 0)
        {
            AudioClip randomImpact = impactSounds[Random.Range(0, impactSounds.Length)];
            whooshAudio.PlayOneShot(randomImpact, 0.7f);
        }
    }
    
    public void SetEffectsActive(bool active)
    {
        effectsActive = active;
        
        if (!active)
        {
            if (ballTrail != null) ballTrail.enabled = false;
            if (ballLight != null) ballLight.enabled = false;
            if (speedParticles != null) speedParticles.Stop();
            if (chargeEffect != null) chargeEffect.Stop();
        }
    }
    
    // ✅ FIX: Safe trail color setting
    public void SetTrailColor(Color color)
    {
        trailColor = color;
        
        // Only modify materials during play mode and not on prefabs
        if (Application.isPlaying && !IsPrefabAsset())
        {
            if (trailMaterial != null)
            {
                trailMaterial.color = color;
            }
            else if (ballTrail != null)
            {
                // Create material if it doesn't exist
                trailMaterial = CreateTrailMaterial();
                ballTrail.material = trailMaterial;
            }
        }
    }
    
    public void SetGlowColor(Color color)
    {
        glowColor = color;
        if (ballLight != null)
        {
            ballLight.color = color;
        }
    }
    
    public float CurrentSpeed => ballRigidbody != null ? ballRigidbody.linearVelocity.magnitude : 0f;
    public bool IsCharging => isCharging;
    public bool EffectsActive => effectsActive;
    
    private void OnDestroy()
    {
        // ✅ FIX: Safely destroy created materials
        if (ballMaterial != null)
        {
            Destroy(ballMaterial);
        }
        
        if (trailMaterial != null)
        {
            Destroy(trailMaterial);
        }
    }
}