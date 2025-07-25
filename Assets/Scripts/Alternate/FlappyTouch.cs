using UnityEngine;

public class FlappyTouch : MonoBehaviour
{

    [SerializeField] private float forceAmount = 5f;
    [SerializeField] private float constantRightSpeed = 3f;
    [SerializeField] private HapticFeedback haptic;

    private Rigidbody2D rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
#if UNITY_STANDALONE
        HandleMouseInput();
#elif UNITY_ANDROID
        HandleTouchInput();
#endif
    }

    private void FixedUpdate()
    {
        // Apply constant rightward speed
        Vector2 velocity = rb.linearVelocity;
        velocity.x = constantRightSpeed;
        rb.linearVelocity = velocity;
    }


    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AddForceUpwards();
            haptic.TriggerVibration();
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            AddForceUpwards();
        }
    }

    void AddForceUpwards()
    {
        rb.AddForce(Vector2.up * forceAmount, ForceMode2D.Impulse);
    }
}