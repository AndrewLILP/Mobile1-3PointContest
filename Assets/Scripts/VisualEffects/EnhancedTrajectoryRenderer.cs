using UnityEngine;

public class EnhancedTrajectoryRenderer : MonoBehaviour
{
    [Header("Base Trajectory Settings")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int trajectoryPoints = 50;
    [SerializeField] private float timeStep = 0.1f;
    [SerializeField] private float maxTrajectoryTime = 5f;
    
    [Header("Visual Enhancements")]
    [SerializeField] private bool enableGradientTrajectory = true;
    [SerializeField] private Gradient trajectoryGradient = new Gradient();
    [SerializeField] private AnimationCurve widthCurve = new AnimationCurve(
        new Keyframe(0, 1f), new Keyframe(1, 0.3f)
    );
    [SerializeField] private float baseWidth = 0.1f;
    
    [Header("Arc Visualization")]
    [SerializeField] private bool showArcIndicators = true;
    [SerializeField] private GameObject arcIndicatorPrefab;
    [SerializeField] private int arcIndicatorCount = 5;
    [SerializeField] private Color arcIndicatorColor = Color.white;
    
    [Header("Target Prediction")]
    [SerializeField] private bool enableTargetPrediction = true;
    [SerializeField] private GameObject targetIndicator;
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private float targetIndicatorSize = 0.5f;
    
    [Header("Power Visualization")]
    [SerializeField] private bool enablePowerVisualization = true;
    [SerializeField] private Color lowPowerColor = Color.red;
    [SerializeField] private Color mediumPowerColor = Color.yellow;
    [SerializeField] private Color highPowerColor = Color.green;
    [SerializeField] private float minPowerThreshold = 5f;
    [SerializeField] private float maxPowerThreshold = 20f;
    
    [Header("Animation")]
    [SerializeField] private bool enableTrajectoryAnimation = true;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool enablePulseEffect = true;
    [SerializeField] private float pulseFrequency = 3f;
    [SerializeField] private float pulseAmplitude = 0.2f;
    
    [Header("Mobile Optimization")]
    [SerializeField] private bool enableMobileOptimization = true;
    [SerializeField] private int mobileTrajectoryPoints = 25;
    [SerializeField] private int mobileArcIndicators = 3;
    
    private TrajectoryRenderer originalTrajectoryRenderer;
    private Transform[] arcIndicators;
    private Material trajectoryMaterial;
    private bool isVisible = false;
    private float animationTime = 0f;
    private Vector3 lastVelocity;
    private Vector3 currentStartPosition;
    private Vector3[] trajectoryPositions;
    private Vector3[] optimizedPositions;
    
    private void Awake()
    {
        if (trajectoryLine == null)
        {
            trajectoryLine = GetComponent<LineRenderer>();
            if (trajectoryLine == null)
            {
                trajectoryLine = gameObject.AddComponent<LineRenderer>();
            }
        }
        
        originalTrajectoryRenderer = GetComponent<TrajectoryRenderer>();
        
        SetupTrajectoryLine();
        SetupArcIndicators();
        SetupTargetIndicator();
        SetupMobileOptimization();
        
        int pointCount = enableMobileOptimization && Application.isMobilePlatform ? 
            mobileTrajectoryPoints : trajectoryPoints;
        trajectoryPositions = new Vector3[pointCount];
    }
    
    private void Start()
    {
        HideTrajectory();
    }
    
    private void Update()
    {
        if (isVisible && enableTrajectoryAnimation)
        {
            UpdateTrajectoryAnimation();
        }
    }
    
    private void SetupTrajectoryLine()
    {
        trajectoryLine.useWorldSpace = true;
        trajectoryLine.startWidth = baseWidth;
        trajectoryLine.endWidth = baseWidth * 0.3f;
        trajectoryLine.sortingOrder = 10;
        
        trajectoryMaterial = new Material(Shader.Find("Sprites/Default"));
        trajectoryLine.material = trajectoryMaterial;
        
        if (enableGradientTrajectory)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(Color.white, 0f);
            colorKeys[1] = new GradientColorKey(Color.white, 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0.3f, 1f);
            
            trajectoryGradient.SetKeys(colorKeys, alphaKeys);
            trajectoryLine.colorGradient = trajectoryGradient;
        }
        
        trajectoryLine.enabled = false;
    }
    
    private void SetupArcIndicators()
    {
        if (!showArcIndicators) return;
        
        int indicatorCount = enableMobileOptimization && Application.isMobilePlatform ? 
            mobileArcIndicators : arcIndicatorCount;
        
        arcIndicators = new Transform[indicatorCount];
        
        for (int i = 0; i < indicatorCount; i++)
        {
            GameObject indicator;
            
            if (arcIndicatorPrefab != null)
            {
                indicator = Instantiate(arcIndicatorPrefab, transform);
            }
            else
            {
                indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.transform.SetParent(transform);
                indicator.transform.localScale = Vector3.one * 0.1f;
                
                Collider col = indicator.GetComponent<Collider>();
                if (col != null) Destroy(col);
                
                Renderer renderer = indicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Sprites/Default"));
                    mat.color = arcIndicatorColor;
                    renderer.material = mat;
                }
            }
            
            indicator.name = $"ArcIndicator_{i}";
            indicator.SetActive(false);
            arcIndicators[i] = indicator.transform;
        }
    }
    
    private void SetupTargetIndicator()
    {
        if (!enableTargetPrediction) return;
        
        if (targetIndicator == null)
        {
            targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetIndicator.transform.SetParent(transform);
            targetIndicator.transform.localScale = new Vector3(targetIndicatorSize, 0.02f, targetIndicatorSize);
            
            Collider col = targetIndicator.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            Renderer renderer = targetIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(1f, 1f, 1f, 0.7f);
                renderer.material = mat;
            }
        }
        
        targetIndicator.name = "TrajectoryTargetIndicator";
        targetIndicator.SetActive(false);
    }
    
    private void SetupMobileOptimization()
    {
        if (!enableMobileOptimization || !Application.isMobilePlatform) return;
        
        trajectoryPoints = mobileTrajectoryPoints;
        arcIndicatorCount = mobileArcIndicators;
        enableTrajectoryAnimation = false;
        enablePulseEffect = false;
    }
    
    public void ShowTrajectory(Vector3 startPosition, Vector3 velocity)
    {
        isVisible = true;
        currentStartPosition = startPosition;
        lastVelocity = velocity;
        
        trajectoryLine.enabled = true;
        UpdateTrajectory(startPosition, velocity);
        
        if (showArcIndicators && arcIndicators != null)
        {
            foreach (var indicator in arcIndicators)
            {
                indicator.gameObject.SetActive(true);
            }
        }
        
        if (enableTargetPrediction && targetIndicator != null)
        {
            targetIndicator.SetActive(true);
        }
    }
    
    public void HideTrajectory()
    {
        isVisible = false;
        trajectoryLine.enabled = false;
        
        if (arcIndicators != null)
        {
            foreach (var indicator in arcIndicators)
            {
                indicator.gameObject.SetActive(false);
            }
        }
        
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(false);
        }
    }
    
    public void UpdateTrajectory(Vector3 startPosition, Vector3 velocity)
    {
        if (!isVisible) return;
        
        currentStartPosition = startPosition;
        lastVelocity = velocity;
        
        CalculateTrajectoryPoints(startPosition, velocity);
        UpdateLineRenderer();
        
        if (showArcIndicators)
        {
            UpdateArcIndicators();
        }
        
        if (enableTargetPrediction)
        {
            UpdateTargetPrediction();
        }
        
        if (enablePowerVisualization)
        {
            UpdatePowerVisualization(velocity.magnitude);
        }
    }
    
    private void CalculateTrajectoryPoints(Vector3 startPosition, Vector3 velocity)
    {
        Vector3 currentPosition = startPosition;
        Vector3 currentVelocity = velocity;
        int pointsCalculated = 0;
        
        for (int i = 0; i < trajectoryPositions.Length; i++)
        {
            trajectoryPositions[i] = currentPosition;
            pointsCalculated++;
            
            currentPosition += currentVelocity * timeStep;
            currentVelocity += Physics.gravity * timeStep;
            
            if (currentPosition.y < -5f)
            {
                break;
            }
            
            if (i * timeStep > maxTrajectoryTime)
            {
                break;
            }
        }
        
        if (pointsCalculated < trajectoryPositions.Length)
        {
            optimizedPositions = new Vector3[pointsCalculated];
            System.Array.Copy(trajectoryPositions, optimizedPositions, pointsCalculated);
        }
        else
        {
            optimizedPositions = trajectoryPositions;
        }
    }
    
    private void UpdateLineRenderer()
    {
        trajectoryLine.positionCount = optimizedPositions.Length;
        trajectoryLine.SetPositions(optimizedPositions);
        
        if (widthCurve != null)
        {
            AnimationCurve widthAnimation = new AnimationCurve();
            for (int i = 0; i < optimizedPositions.Length; i++)
            {
                float t = (float)i / (optimizedPositions.Length - 1);
                float width = baseWidth * widthCurve.Evaluate(t);
                widthAnimation.AddKey(t, width);
            }
            trajectoryLine.widthCurve = widthAnimation;
        }
    }
    
    private void UpdateArcIndicators()
    {
        if (arcIndicators == null || optimizedPositions.Length == 0) return;
        
        int step = Mathf.Max(1, optimizedPositions.Length / arcIndicators.Length);
        
        for (int i = 0; i < arcIndicators.Length; i++)
        {
            int positionIndex = Mathf.Min(i * step, optimizedPositions.Length - 1);
            arcIndicators[i].position = optimizedPositions[positionIndex];
            
            if (enableTrajectoryAnimation)
            {
                float animOffset = Mathf.Sin(animationTime + i * 0.5f) * 0.05f;
                arcIndicators[i].position += Vector3.up * animOffset;
            }
        }
    }
    
    private void UpdateTargetPrediction()
    {
        if (targetIndicator == null || optimizedPositions.Length == 0) return;
        
        Vector3 targetPosition = optimizedPositions[optimizedPositions.Length - 1];
        
        RaycastHit hit;
        if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
        {
            targetPosition = hit.point;
        }
        
        targetIndicator.transform.position = targetPosition + Vector3.up * 0.01f;
        
        if (enablePulseEffect)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseFrequency) * pulseAmplitude;
            targetIndicator.transform.localScale = Vector3.one * targetIndicatorSize * pulse;
        }
    }
    
    private void UpdatePowerVisualization(float power)
    {
        Color powerColor;
        
        if (power < minPowerThreshold)
        {
            powerColor = lowPowerColor;
        }
        else if (power > maxPowerThreshold)
        {
            powerColor = highPowerColor;
        }
        else
        {
            float t = (power - minPowerThreshold) / (maxPowerThreshold - minPowerThreshold);
            powerColor = Color.Lerp(lowPowerColor, mediumPowerColor, t * 2f);
            if (t > 0.5f)
            {
                powerColor = Color.Lerp(mediumPowerColor, highPowerColor, (t - 0.5f) * 2f);
            }
        }
        
        if (trajectoryMaterial != null)
        {
            trajectoryMaterial.color = powerColor;
        }
        
        if (enableGradientTrajectory)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(powerColor, 0f);
            colorKeys[1] = new GradientColorKey(powerColor * 0.7f, 1f);
            
            trajectoryGradient.SetKeys(colorKeys, trajectoryGradient.alphaKeys);
            trajectoryLine.colorGradient = trajectoryGradient;
        }
    }
    
    private void UpdateTrajectoryAnimation()
    {
        animationTime += Time.deltaTime * animationSpeed;
        
        if (enablePulseEffect)
        {
            float pulse = 1f + Mathf.Sin(animationTime * pulseFrequency) * pulseAmplitude;
            trajectoryLine.startWidth = baseWidth * pulse;
            trajectoryLine.endWidth = baseWidth * 0.3f * pulse;
        }
    }
    
    // Public methods called by BasketballVFXManager
    public void SetTrajectoryColor(Color color)
    {
        if (trajectoryMaterial != null)
        {
            trajectoryMaterial.color = color;
        }
    }
    
    public void SetTrajectoryWidth(float width)
    {
        baseWidth = width;
        trajectoryLine.startWidth = width;
        trajectoryLine.endWidth = width * 0.3f;
    }
    
    public void SetArcIndicatorColor(Color color)
    {
        arcIndicatorColor = color;
        
        if (arcIndicators != null)
        {
            foreach (var indicator in arcIndicators)
            {
                Renderer renderer = indicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }
    }
    
    public void EnableLowDetailMode(bool enable)
    {
        enableMobileOptimization = enable;
        
        if (enable)
        {
            trajectoryPoints = mobileTrajectoryPoints;
            showArcIndicators = false;
            enableTrajectoryAnimation = false;
            enablePulseEffect = false;
        }
    }
    
    public Vector3 GetTrajectoryEndPoint(Vector3 startPosition, Vector3 velocity)
    {
        CalculateTrajectoryPoints(startPosition, velocity);
        return optimizedPositions.Length > 0 ? optimizedPositions[optimizedPositions.Length - 1] : startPosition;
    }
    
    public bool IsVisible => isVisible;
    public int TrajectoryPointCount => optimizedPositions?.Length ?? 0;
    public Vector3 LastVelocity => lastVelocity;
    
    private void OnDestroy()
    {
        if (trajectoryMaterial != null)
        {
            Destroy(trajectoryMaterial);
        }
    }
}