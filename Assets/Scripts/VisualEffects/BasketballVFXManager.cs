using UnityEngine;
using System.Collections.Generic;

public class BasketballVFXManager : MonoBehaviour
{
    [Header("Visual Effects Components")]
    [SerializeField] private BallVisualEffects ballVFX;
    [SerializeField] private HoopVisualEffects hoopVFX;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private LaunchChargeEffect launchCharge;
    [SerializeField] private EnhancedTrajectoryRenderer trajectoryRenderer;
    
    [Header("Game Integration")]
    [SerializeField] private BallLauncher ballLauncher;
    [SerializeField] private Hoop basketballHoop;
    [SerializeField] private ThreePointContest contestManager;
    
    [Header("Performance Settings")]
    [SerializeField] private VFXQualityLevel qualityLevel = VFXQualityLevel.High;
    [SerializeField] private bool autoDetectMobile = true;
    [SerializeField] private bool enableAllEffects = true;
    
    [Header("Effect Toggles")]
    [SerializeField] private bool enableBallEffects = true;
    [SerializeField] private bool enableHoopEffects = true;
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private bool enableLaunchEffects = true;
    [SerializeField] private bool enableTrajectoryEffects = true;
    
    [Header("Global Color Scheme")]
    [SerializeField] private ColorScheme currentColorScheme = ColorScheme.Default;
    [SerializeField] private VFXColorPalette colorPalette = new VFXColorPalette();
    
    [Header("Audio Integration")]
    [SerializeField] private AudioSource globalVFXAudio;
    [SerializeField] private AudioClip[] globalVFXSounds;
    
    private Dictionary<string, bool> effectStates = new Dictionary<string, bool>();
    private bool isInitialized = false;
    
    public System.Action<VFXQualityLevel> OnQualityChanged;
    public System.Action<ColorScheme> OnColorSchemeChanged;
    public System.Action<bool> OnEffectsToggled;
    
    public enum VFXQualityLevel
    {
        Low,
        Medium,
        High
    }
    
    public enum ColorScheme
    {
        Default,
        Neon,
        Retro,
        Minimal,
        Contest
    }
    
    [System.Serializable]
    public class VFXColorPalette
    {
        public Color primary = Color.white;
        public Color secondary = Color.cyan;
        public Color accent = Color.yellow;
        public Color success = Color.green;
        public Color warning = new Color(1f, 0.6f, 0f);
        public Color error = Color.red;
    }
    
    private void Awake()
    {
        InitializeVFXManager();
    }
    
    private void Start()
    {
        ConnectToGameSystems();
        ApplyInitialSettings();
        isInitialized = true;
    }
    
    private void InitializeVFXManager()
    {
        if (autoDetectMobile && Application.isMobilePlatform)
        {
            qualityLevel = VFXQualityLevel.Medium;
            Debug.Log("Mobile platform detected - setting VFX quality to Medium");
        }
        
        FindVFXComponents();
        InitializeEffectStates();
        SetupGlobalAudio();
    }
    
    private void FindVFXComponents()
    {
        if (ballVFX == null)
            ballVFX = FindFirstObjectByType<BallVisualEffects>();
        
        if (hoopVFX == null)
            hoopVFX = FindFirstObjectByType<HoopVisualEffects>();
        
        if (cameraShake == null)
        {
            cameraShake = FindFirstObjectByType<CameraShake>();
            if (cameraShake == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraShake = mainCam.gameObject.AddComponent<CameraShake>();
                }
            }
        }
        
        if (launchCharge == null)
            launchCharge = FindFirstObjectByType<LaunchChargeEffect>();
        
        if (trajectoryRenderer == null)
            trajectoryRenderer = FindFirstObjectByType<EnhancedTrajectoryRenderer>();
        
        if (ballLauncher == null)
            ballLauncher = FindFirstObjectByType<BallLauncher>();
        
        if (basketballHoop == null)
            basketballHoop = FindFirstObjectByType<Hoop>();
        
        if (contestManager == null)
            contestManager = FindFirstObjectByType<ThreePointContest>();
    }
    
    private void InitializeEffectStates()
    {
        effectStates["ballEffects"] = enableBallEffects;
        effectStates["hoopEffects"] = enableHoopEffects;
        effectStates["screenShake"] = enableScreenShake;
        effectStates["launchEffects"] = enableLaunchEffects;
        effectStates["trajectoryEffects"] = enableTrajectoryEffects;
    }
    
    private void SetupGlobalAudio()
    {
        if (globalVFXAudio == null)
        {
            globalVFXAudio = gameObject.AddComponent<AudioSource>();
        }
        
        globalVFXAudio.playOnAwake = false;
        globalVFXAudio.spatialBlend = 0f;
    }
    
    private void ConnectToGameSystems()
    {
        if (ballLauncher != null)
        {
            ballLauncher.OnBallLaunched += HandleBallLaunched;
            ballLauncher.OnShotTaken += HandleShotTaken;
        }
        
        if (basketballHoop != null)
        {
            basketballHoop.OnScore += HandleScore;
            basketballHoop.OnMiss += HandleMiss;
        }
        
        if (contestManager != null)
        {
            contestManager.OnScoreUpdated += HandleContestScoreUpdate;
            contestManager.OnPositionChanged += HandlePositionChanged;
            contestManager.OnContestComplete += HandleContestComplete;
        }
        
        ConnectVFXEvents();
    }
    
    private void ConnectVFXEvents()
    {
        if (ballVFX != null)
        {
            ballVFX.OnSpeedChanged += HandleBallSpeedChanged;
            ballVFX.OnBallImpact += HandleBallImpact;
        }
        
        if (hoopVFX != null)
        {
            hoopVFX.OnScoreEffectStarted += HandleScoreEffectStarted;
            hoopVFX.OnRimImpact += HandleRimImpact;
            hoopVFX.OnBackboardImpact += HandleBackboardImpact;
        }
        
        if (launchCharge != null)
        {
            launchCharge.OnChargeStarted += HandleChargeStarted;
            launchCharge.OnChargeComplete += HandleChargeComplete;
            launchCharge.OnOvercharge += HandleOvercharge;
        }
    }
    
    private void ApplyInitialSettings()
    {
        SetQualityLevel(qualityLevel);
        SetColorScheme(currentColorScheme);
        ApplyEffectToggles();
    }
    
    #region Event Handlers
    
    private void HandleBallLaunched()
    {
        if (enableLaunchEffects && launchCharge != null)
        {
            launchCharge.CompleteCharge();
        }
        
        PlayGlobalSound("launch");
    }
    
    private void HandleShotTaken(int shotsRemaining)
    {
        Debug.Log($"Shot taken - {shotsRemaining} shots remaining");
    }
    
    private void HandleScore(int points, bool isSwish)
    {
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.ShakeForScore(isSwish);
        }
        
        PlayGlobalSound(isSwish ? "swish" : "score");
        
        if (isSwish)
        {
            FlashColorScheme(ColorScheme.Contest, 1f);
        }
    }
    
    private void HandleMiss()
    {
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.ShakeForMiss();
        }
        
        PlayGlobalSound("miss");
    }
    
    private void HandleBallSpeedChanged(float normalizedSpeed)
    {
        if (normalizedSpeed > 0.8f)
        {
            SetGlobalEffectIntensity(1.2f);
        }
        else
        {
            SetGlobalEffectIntensity(1f);
        }
    }
    
    private void HandleBallImpact()
    {
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.PulseEffect(0.02f, 0.1f);
        }
    }
    
    private void HandleScoreEffectStarted(bool isSwish)
    {
        Debug.Log($"Score effect started - Swish: {isSwish}");
    }
    
    private void HandleRimImpact()
    {
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.ShakeForRimHit();
        }
    }
    
    private void HandleBackboardImpact()
    {
        PlayGlobalSound("backboard");
    }
    
    private void HandleChargeStarted()
    {
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.HideTrajectory();
        }
    }
    
    private void HandleChargeComplete()
    {
        PlayGlobalSound("charge_complete");
    }
    
    private void HandleOvercharge()
    {
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.PulseEffect(0.1f, 0.2f);
        }
        
        PlayGlobalSound("overcharge");
    }
    
    private void HandleContestScoreUpdate(int totalScore, int positionScore)
    {
        if (positionScore == 5)
        {
            TriggerCelebrationEffects();
        }
    }
    
    private void HandlePositionChanged(int position, int ballsRemaining, int positionScore)
    {
        FlashColorScheme(ColorScheme.Contest, 2f);
    }
    
    private void HandleContestComplete(int finalScore, System.Collections.Generic.List<int> positionScores)
    {
        TriggerContestCompleteEffects(finalScore);
    }
    
    #endregion
    
    #region Public API
    
    public void SetQualityLevel(VFXQualityLevel quality)
    {
        qualityLevel = quality;
        ApplyQualitySettings();
        OnQualityChanged?.Invoke(quality);
        Debug.Log($"VFX Quality set to: {quality}");
    }
    
    public void SetColorScheme(ColorScheme scheme)
    {
        currentColorScheme = scheme;
        UpdateColorPalette(scheme);
        ApplyColorScheme();
        OnColorSchemeChanged?.Invoke(scheme);
    }
    
    public void ToggleAllEffects(bool enable)
    {
        enableAllEffects = enable;
        enableBallEffects = enable;
        enableHoopEffects = enable;
        enableScreenShake = enable;
        enableLaunchEffects = enable;
        enableTrajectoryEffects = enable;
        
        ApplyEffectToggles();
        OnEffectsToggled?.Invoke(enable);
    }
    
    public void ToggleEffect(string effectName, bool enable)
    {
        if (effectStates.ContainsKey(effectName))
        {
            effectStates[effectName] = enable;
            ApplySpecificEffectToggle(effectName, enable);
        }
    }
    
    public void TriggerCelebrationEffects()
    {
        if (enableScreenShake && cameraShake != null)
        {
            cameraShake.ShakeCamera(0.3f, 1f);
        }
        
        FlashColorScheme(ColorScheme.Contest, 2f);
        PlayGlobalSound("celebration");
    }
    
    public void FlashColorScheme(ColorScheme scheme, float duration)
    {
        StartCoroutine(FlashColorSchemeCoroutine(scheme, duration));
    }
    
    #endregion
    
    #region Private Methods
    
    private void ApplyQualitySettings()
    {
        bool lowDetail = qualityLevel == VFXQualityLevel.Low;
        bool mediumDetail = qualityLevel == VFXQualityLevel.Medium;
        
        if (ballVFX != null)
        {
            ballVFX.SetEffectsActive(!lowDetail);
        }
        
        if (hoopVFX != null)
        {
            hoopVFX.SetLowDetailMode(lowDetail);
            hoopVFX.SetEffectsEnabled(!lowDetail || mediumDetail);
        }
        
        if (launchCharge != null)
        {
            launchCharge.SetLowDetailMode(lowDetail);
        }
        
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.EnableLowDetailMode(lowDetail);
        }
        
        if (cameraShake != null)
        {
            cameraShake.SetShakeEnabled(!lowDetail);
        }
    }
    
    private void UpdateColorPalette(ColorScheme scheme)
    {
        switch (scheme)
        {
            case ColorScheme.Default:
                colorPalette.primary = Color.white;
                colorPalette.secondary = Color.cyan;
                colorPalette.accent = Color.yellow;
                colorPalette.success = Color.green;
                colorPalette.warning = new Color(1f, 0.6f, 0f);
                colorPalette.error = Color.red;
                break;
                
            case ColorScheme.Neon:
                colorPalette.primary = new Color(0.2f, 1f, 0.8f);
                colorPalette.secondary = new Color(1f, 0.2f, 0.8f);
                colorPalette.accent = new Color(1f, 1f, 0.2f);
                colorPalette.success = new Color(0.2f, 1f, 0.2f);
                colorPalette.warning = new Color(1f, 0.6f, 0.2f);
                colorPalette.error = new Color(1f, 0.2f, 0.2f);
                break;
                
            case ColorScheme.Retro:
                colorPalette.primary = new Color(0.9f, 0.9f, 0.7f);
                colorPalette.secondary = new Color(0.8f, 0.5f, 0.2f);
                colorPalette.accent = new Color(0.9f, 0.7f, 0.2f);
                colorPalette.success = new Color(0.5f, 0.8f, 0.3f);
                colorPalette.warning = new Color(0.9f, 0.6f, 0.2f);
                colorPalette.error = new Color(0.8f, 0.3f, 0.2f);
                break;
                
            case ColorScheme.Minimal:
                colorPalette.primary = new Color(0.2f, 0.2f, 0.2f);
                colorPalette.secondary = new Color(0.6f, 0.6f, 0.6f);
                colorPalette.accent = new Color(0.8f, 0.8f, 0.8f);
                colorPalette.success = new Color(0.4f, 0.6f, 0.4f);
                colorPalette.warning = new Color(0.6f, 0.6f, 0.4f);
                colorPalette.error = new Color(0.6f, 0.4f, 0.4f);
                break;
                
            case ColorScheme.Contest:
                colorPalette.primary = new Color(1f, 0.8f, 0.2f);
                colorPalette.secondary = new Color(0.2f, 0.5f, 1f);
                colorPalette.accent = new Color(1f, 1f, 1f);
                colorPalette.success = new Color(0.2f, 0.8f, 0.2f);
                colorPalette.warning = new Color(1f, 0.5f, 0.2f);
                colorPalette.error = new Color(0.8f, 0.2f, 0.2f);
                break;
        }
    }
    
    private void ApplyColorScheme()
    {
        if (ballVFX != null)
        {
            ballVFX.SetTrailColor(colorPalette.primary);
            ballVFX.SetGlowColor(colorPalette.accent);
        }
        
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.SetTrajectoryColor(colorPalette.primary);
            trajectoryRenderer.SetArcIndicatorColor(colorPalette.secondary);
        }
    }
    
    private void ApplyEffectToggles()
    {
        if (ballVFX != null)
        {
            ballVFX.SetEffectsActive(enableBallEffects && enableAllEffects);
        }
        
        if (hoopVFX != null)
        {
            hoopVFX.SetEffectsEnabled(enableHoopEffects && enableAllEffects);
        }
        
        if (cameraShake != null)
        {
            cameraShake.SetShakeEnabled(enableScreenShake && enableAllEffects);
        }
    }
    
    private void ApplySpecificEffectToggle(string effectName, bool enable)
    {
        switch (effectName)
        {
            case "ballEffects":
                enableBallEffects = enable;
                if (ballVFX != null) ballVFX.SetEffectsActive(enable && enableAllEffects);
                break;
                
            case "hoopEffects":
                enableHoopEffects = enable;
                if (hoopVFX != null) hoopVFX.SetEffectsEnabled(enable && enableAllEffects);
                break;
                
            case "screenShake":
                enableScreenShake = enable;
                if (cameraShake != null) cameraShake.SetShakeEnabled(enable && enableAllEffects);
                break;
        }
    }
    
    private void SetGlobalEffectIntensity(float multiplier)
    {
        // Adjust global effect intensity for exciting moments
    }
    
    private void TriggerContestCompleteEffects(int finalScore)
    {
        if (finalScore >= 25)
        {
            FlashColorScheme(ColorScheme.Contest, 3f);
            if (cameraShake != null)
            {
                cameraShake.ShakeCamera(0.4f, 2f);
            }
        }
        
        PlayGlobalSound("contest_complete");
    }
    
    private void PlayGlobalSound(string soundName)
    {
        if (globalVFXAudio == null || globalVFXSounds.Length == 0) return;
        
        int soundIndex = soundName.GetHashCode() % globalVFXSounds.Length;
        if (soundIndex < 0) soundIndex = -soundIndex;
        
        globalVFXAudio.PlayOneShot(globalVFXSounds[soundIndex]);
    }
    
    private System.Collections.IEnumerator FlashColorSchemeCoroutine(ColorScheme scheme, float duration)
    {
        ColorScheme originalScheme = currentColorScheme;
        
        SetColorScheme(scheme);
        yield return new WaitForSeconds(duration);
        SetColorScheme(originalScheme);
    }
    
    #endregion
    
    #region Public Getters
    
    public VFXQualityLevel CurrentQuality => qualityLevel;
    public ColorScheme CurrentColorScheme => currentColorScheme;
    public bool AllEffectsEnabled => enableAllEffects;
    public bool IsInitialized => isInitialized;
    
    public bool GetEffectState(string effectName)
    {
        return effectStates.ContainsKey(effectName) ? effectStates[effectName] : false;
    }
    
    #endregion
}