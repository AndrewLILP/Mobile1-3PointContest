using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VFXSetupHelper : MonoBehaviour
{
    [Header("Setup Options")]
    [SerializeField] private bool setupBallVFX = true;
    [SerializeField] private bool setupHoopVFX = true;
    [SerializeField] private bool setupCameraShake = true;
    [SerializeField] private bool setupLaunchCharge = true;
    [SerializeField] private bool setupTrajectory = true;
    [SerializeField] private bool setupVFXManager = true;
    
    [Header("Game Object References")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject hoopGameObject;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform launchPoint;
    
    [Header("Mobile Optimization")]
    [SerializeField] private bool optimizeForMobile = true;
    [SerializeField] private bool createLowQualityMaterials = true;
    
    [Header("Created Objects (Read-Only)")]
    [SerializeField] private List<GameObject> createdObjects = new List<GameObject>();
    
    #if UNITY_EDITOR
    [ContextMenu("Auto Setup All VFX")]
    public void AutoSetupAllVFX()
    {
        Debug.Log("Starting VFX Auto-Setup...");
        
        FindRequiredComponents();
        
        if (setupBallVFX) SetupBallVisualEffects();
        if (setupHoopVFX) SetupHoopVisualEffects();
        if (setupCameraShake) SetupCameraShake();
        if (setupLaunchCharge) SetupLaunchChargeEffect();
        if (setupTrajectory) SetupTrajectoryRenderer();
        if (setupVFXManager) SetupVFXManager();
        
        if (createLowQualityMaterials) CreateMobileMaterials();
        
        Debug.Log($"VFX Auto-Setup Complete! Created {createdObjects.Count} objects.");
    }
    
    [ContextMenu("Clean Up Created Objects")]
    public void CleanUpCreatedObjects()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                DestroyImmediate(createdObjects[i]);
            }
        }
        createdObjects.Clear();
        Debug.Log("Cleaned up all created VFX objects.");
    }
    #endif
    
    private void FindRequiredComponents()
    {
        if (ballPrefab == null)
        {
            ballPrefab = GameObject.FindWithTag("Ball");
            if (ballPrefab == null)
            {
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var obj in allObjects)
                {
                    if (obj.name.ToLower().Contains("ball"))
                    {
                        ballPrefab = obj;
                        break;
                    }
                }
            }
        }
        
        if (hoopGameObject == null)
        {
            Hoop hoop = FindFirstObjectByType<Hoop>();
            if (hoop != null)
            {
                hoopGameObject = hoop.gameObject;
            }
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (launchPoint == null)
        {
            BallLauncher launcher = FindFirstObjectByType<BallLauncher>();
            if (launcher != null)
            {
                launchPoint = launcher.transform.Find("LaunchPoint");
                if (launchPoint == null)
                {
                    launchPoint = launcher.transform;
                }
            }
        }
    }
    
    private void SetupBallVisualEffects()
    {
        if (ballPrefab == null)
        {
            Debug.LogWarning("Ball prefab not found. Skipping ball VFX setup.");
            return;
        }
        
        BallVisualEffects ballVFX = ballPrefab.GetComponent<BallVisualEffects>();
        if (ballVFX == null)
        {
            ballVFX = ballPrefab.AddComponent<BallVisualEffects>();
        }
        
        GameObject trailObj = CreateChildObject(ballPrefab, "BallTrail");
        TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
        ConfigureTrailRenderer(trail);
        
        GameObject glowObj = CreateChildObject(ballPrefab, "BallGlow");
        Light glowLight = glowObj.AddComponent<Light>();
        ConfigureGlowLight(glowLight);
        
        GameObject speedParticlesObj = CreateChildObject(ballPrefab, "SpeedParticles");
        ParticleSystem speedParticles = speedParticlesObj.AddComponent<ParticleSystem>();
        ConfigureSpeedParticles(speedParticles);
        
        GameObject chargeObj = CreateChildObject(ballPrefab, "ChargeEffect");
        ParticleSystem chargeParticles = chargeObj.AddComponent<ParticleSystem>();
        ConfigureChargeParticles(chargeParticles);
        
        GameObject impactObj = CreateChildObject(ballPrefab, "ImpactSparks");
        ParticleSystem impactParticles = impactObj.AddComponent<ParticleSystem>();
        ConfigureImpactParticles(impactParticles);
        
        Debug.Log("Ball VFX setup complete.");
    }
    
    private void SetupHoopVisualEffects()
    {
        if (hoopGameObject == null)
        {
            Debug.LogWarning("Hoop GameObject not found. Skipping hoop VFX setup.");
            return;
        }
        
        HoopVisualEffects hoopVFX = hoopGameObject.GetComponent<HoopVisualEffects>();
        if (hoopVFX == null)
        {
            hoopVFX = hoopGameObject.AddComponent<HoopVisualEffects>();
        }
        
        GameObject lightObj = CreateChildObject(hoopGameObject, "HoopLight");
        lightObj.transform.localPosition = Vector3.up * 0.5f;
        Light hoopLight = lightObj.AddComponent<Light>();
        ConfigureHoopLight(hoopLight);
        
        GameObject fireworksObj = CreateChildObject(hoopGameObject, "ScoreFireworks");
        fireworksObj.transform.localPosition = Vector3.up * 1f;
        ParticleSystem fireworks = fireworksObj.AddComponent<ParticleSystem>();
        ConfigureFireworksParticles(fireworks);
        
        GameObject starsObj = CreateChildObject(hoopGameObject, "SwishStars");
        starsObj.transform.localPosition = Vector3.up * 0.5f;
        ParticleSystem stars = starsObj.AddComponent<ParticleSystem>();
        ConfigureSwishStars(stars);
        
        GameObject confettiObj = CreateChildObject(hoopGameObject, "ConfettiEffect");
        confettiObj.transform.localPosition = Vector3.up * 2f;
        ParticleSystem confetti = confettiObj.AddComponent<ParticleSystem>();
        ConfigureConfettiParticles(confetti);
        
        GameObject rimSparksObj = CreateChildObject(hoopGameObject, "RimSparks");
        ParticleSystem rimSparks = rimSparksObj.AddComponent<ParticleSystem>();
        ConfigureRimSparks(rimSparks);
        
        GameObject backboardObj = CreateChildObject(hoopGameObject, "BackboardRipples");
        ParticleSystem backboard = backboardObj.AddComponent<ParticleSystem>();
        ConfigureBackboardRipples(backboard);
        
        Debug.Log("Hoop VFX setup complete.");
    }
    
    private void SetupCameraShake()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found. Skipping camera shake setup.");
            return;
        }
        
        CameraShake shake = mainCamera.GetComponent<CameraShake>();
        if (shake == null)
        {
            shake = mainCamera.gameObject.AddComponent<CameraShake>();
        }
        
        Debug.Log("Camera shake setup complete.");
    }
    
    private void SetupLaunchChargeEffect()
    {
        if (launchPoint == null)
        {
            Debug.LogWarning("Launch point not found. Skipping launch charge setup.");
            return;
        }
        
        GameObject chargeEffectObj = new GameObject("LaunchChargeEffect");
        chargeEffectObj.transform.position = launchPoint.position;
        createdObjects.Add(chargeEffectObj);
        
        LaunchChargeEffect chargeEffect = chargeEffectObj.AddComponent<LaunchChargeEffect>();
        
        GameObject chargeParticlesObj = CreateChildObject(chargeEffectObj, "ChargeParticles");
        ParticleSystem chargeParticles = chargeParticlesObj.AddComponent<ParticleSystem>();
        ConfigureLaunchChargeParticles(chargeParticles);
        
        Debug.Log("Launch charge effect setup complete.");
    }
    
    private void SetupTrajectoryRenderer()
    {
        BallLauncher launcher = FindFirstObjectByType<BallLauncher>();
        if (launcher == null)
        {
            Debug.LogWarning("BallLauncher not found. Skipping trajectory setup.");
            return;
        }
        
        TrajectoryRenderer existingTrajectory = launcher.GetComponent<TrajectoryRenderer>();
        if (existingTrajectory != null)
        {
            existingTrajectory.enabled = false;
        }
        
        EnhancedTrajectoryRenderer enhancedTrajectory = launcher.GetComponent<EnhancedTrajectoryRenderer>();
        if (enhancedTrajectory == null)
        {
            enhancedTrajectory = launcher.gameObject.AddComponent<EnhancedTrajectoryRenderer>();
        }
        
        GameObject trajectoryLineObj = CreateChildObject(launcher.gameObject, "TrajectoryLine");
        LineRenderer trajectoryLine = trajectoryLineObj.AddComponent<LineRenderer>();
        ConfigureTrajectoryLine(trajectoryLine);
        
        for (int i = 0; i < 5; i++)
        {
            GameObject arcIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            arcIndicator.name = $"ArcIndicator_{i}";
            arcIndicator.transform.SetParent(launcher.transform);
            arcIndicator.transform.localScale = Vector3.one * 0.1f;
            
            Collider col = arcIndicator.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            
            createdObjects.Add(arcIndicator);
        }
        
        GameObject targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetIndicator.name = "TargetIndicator";
        targetIndicator.transform.SetParent(launcher.transform);
        targetIndicator.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
        
        Collider targetCol = targetIndicator.GetComponent<Collider>();
        if (targetCol != null) DestroyImmediate(targetCol);
        
        createdObjects.Add(targetIndicator);
        
        Debug.Log("Enhanced trajectory setup complete.");
    }
    
    private void SetupVFXManager()
    {
        GameObject vfxManagerObj = new GameObject("VFXManager");
        createdObjects.Add(vfxManagerObj);
        
        BasketballVFXManager vfxManager = vfxManagerObj.AddComponent<BasketballVFXManager>();
        
        Debug.Log("VFX Manager setup complete.");
    }
    
    private void CreateMobileMaterials()
    {
        #if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        Material ballTrailMat = new Material(Shader.Find("Sprites/Default"));
        ballTrailMat.color = Color.white;
        AssetDatabase.CreateAsset(ballTrailMat, "Assets/Materials/BallTrail.mat");
        
        Material particleGlowMat = new Material(Shader.Find("Mobile/Particles/Additive"));
        particleGlowMat.color = Color.white;
        AssetDatabase.CreateAsset(particleGlowMat, "Assets/Materials/ParticleGlow.mat");
        
        Material trajectoryMat = new Material(Shader.Find("Sprites/Default"));
        trajectoryMat.color = Color.cyan;
        AssetDatabase.CreateAsset(trajectoryMat, "Assets/Materials/TrajectoryLine.mat");
        
        AssetDatabase.SaveAssets();
        Debug.Log("Mobile-optimized materials created in Assets/Materials/");
        #endif
    }
    
    private GameObject CreateChildObject(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = Vector3.zero;
        createdObjects.Add(child);
        return child;
    }
    
    // Configuration methods for particle systems and components
    private void ConfigureTrailRenderer(TrailRenderer trail)
    {
        trail.time = 0.5f;
        trail.startWidth = 0.1f;
        trail.endWidth = 0.02f;
        trail.material = Resources.Load<Material>("Materials/Default-Particle") ?? 
                        new Material(Shader.Find("Sprites/Default"));
        trail.enabled = false;
    }
    
    private void ConfigureGlowLight(Light light)
    {
        light.type = LightType.Point;
        light.color = Color.white;
        light.intensity = 0f;
        light.range = 3f;
        light.enabled = false;
    }
    
    private void ConfigureSpeedParticles(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 2f;
        main.maxParticles = optimizeForMobile ? 15 : 20;
        main.startColor = Color.white;
        
        var emission = particles.emission;
        emission.enabled = false;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        
        particles.Stop();
    }
    
    private void ConfigureChargeParticles(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 1f;
        main.startSpeed = 1f;
        main.maxParticles = optimizeForMobile ? 10 : 15;
        main.startColor = Color.cyan;
        
        var emission = particles.emission;
        emission.enabled = false;
        
        particles.Stop();
    }
    
    private void ConfigureImpactParticles(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 5f;
        main.maxParticles = 10;
        main.startColor = Color.yellow;
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });
        
        particles.Stop();
    }
    
    private void ConfigureHoopLight(Light light)
    {
        light.type = LightType.Point;
        light.color = Color.green;
        light.intensity = 0f;
        light.range = 5f;
        light.enabled = false;
    }
    
    private void ConfigureFireworksParticles(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 8f;
        main.maxParticles = optimizeForMobile ? 20 : 30;
        main.startColor = Color.white;
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)(optimizeForMobile ? 15 : 30)),
            new ParticleSystem.Burst(0.5f, (short)(optimizeForMobile ? 10 : 20))
        });
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        
        particles.Stop();
    }
    
    private void ConfigureSwishStars(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 2f;
        main.startSpeed = 3f;
        main.maxParticles = optimizeForMobile ? 10 : 15;
        main.startColor = new Color(1f, 0.8f, 0.2f);
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[] 
        { 
            new ParticleSystem.Burst(0f, (short)(optimizeForMobile ? 8 : 15)) 
        });
        
        particles.Stop();
    }
    
    private void ConfigureConfettiParticles(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 3f;
        main.startSpeed = 5f;
        main.maxParticles = optimizeForMobile ? 15 : 40;
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] 
        { 
            new ParticleSystem.Burst(0f, (short)(optimizeForMobile ? 20 : 40)) 
        });
        
        particles.Stop();
    }
    
    private void ConfigureRimSparks(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 8f;
        main.maxParticles = 15;
        main.startColor = Color.yellow;
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
        
        particles.Stop();
    }
    
    private void ConfigureBackboardRipples(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 1f;
        main.startSpeed = 2f;
        main.maxParticles = 10;
        main.startColor = Color.blue;
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 5) });
        
        particles.Stop();
    }
    
    private void ConfigureLaunchChargeParticles(ParticleSystem particles)
    {
        var main = particles.main;
        main.startLifetime = 1f;
        main.startSpeed = 1f;
        main.maxParticles = optimizeForMobile ? 15 : 30;
        main.startColor = Color.cyan;
        
        var emission = particles.emission;
        emission.enabled = false;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1f;
        
        particles.Stop();
    }
    
    private void ConfigureTrajectoryLine(LineRenderer line)
    {
        line.useWorldSpace = true;
        line.startWidth = 0.1f;
        line.endWidth = 0.03f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.material.color = Color.cyan;
        line.enabled = false;
    }
}