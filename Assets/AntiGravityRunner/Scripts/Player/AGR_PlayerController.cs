// ============================================================
// AGR_PlayerController.cs — FIXED: Swipe controls now work!
// ============================================================
// Player jumps over obstacles (normal jump, not gravity flip)
// Can also move left/right with A/D keys or swipe
// SWIPE FIX: Works in Unity Editor mobile simulation + real devices
// ATTACH THIS TO: Your Player GameObject (must have Rigidbody)
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AGR_PlayerController : MonoBehaviour
{
    [Header("Forward Movement")]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Left/Right Movement")]
    [SerializeField] private float laneSpeed = 8f;
    [SerializeField] private float maxX = 3f;

    [Header("Jump Settings")]
    [Tooltip("How high the player jumps")]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("Extra gravity for snappier feel")]
    [SerializeField] private float extraGravity = 30f;

    [Tooltip("Can double jump?")]
    [SerializeField] private int maxJumps = 1;

    private Rigidbody rb;
    private bool isDead = false;
    private bool gameStarted = false;
    private float horizontalInput = 0f;
    private int jumpsRemaining;
    private bool isGrounded = false;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideDuration = 0.35f; // Quicker, snappier slide
    private Vector3 originalScale;
    private Vector3 originalColliderSize;
    private Vector3 originalColliderCenter;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;
    private Animator anim;
    private bool dodgingLeft = false;
    private bool dodgingRight = false;

    // 3-Lane system for Swipe Mode
    private int currentLane = 0; // -1 = Left, 0 = Center, 1 = Right

    // ============ SWIPE FIX ============
    // Unified input tracking that works with BOTH touch AND mouse
    private Vector2 inputStartPos;
    private bool isTrackingInput = false;
    private bool swipeProcessed = false; // Prevent double-processing
    private float inputStartTime;
    private const float SWIPE_THRESHOLD = 30f; // Lowered from 50 for mobile sim
    private const float TAP_MAX_TIME = 0.3f; // Max time for a tap (seconds)
    private const float TAP_MAX_DISTANCE = 20f; // Max movement for a tap
    // ===================================

    public bool IsDead => isDead;
    public bool IsGravityFlipped => false; // Keep for compatibility
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }
    public bool GameStarted
    {
        get => gameStarted;
        set => gameStarted = value;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        jumpsRemaining = maxJumps;
        originalScale = transform.localScale;
        
        // FORCE override cached Inspector values
        jumpForce = 20f;
        extraGravity = 35f;
        maxJumps = 1; // Explicitly force this to 1 to disable double jumping
        
        // CRITICAL FIX: Destroy any Animator on the Player object itself!
        Animator selfAnim = GetComponent<Animator>();
        if (selfAnim != null) DestroyImmediate(selfAnim);
    }

    void Start()
    {
        // Removed, using lazy init via EnsureAnimator
    }

    private void EnsureAnimator()
    {
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.speed = 1.15f; // Global animation speed
            }
        }
    }

    void Update()
    {
        EnsureAnimator();

        if (isDead || !gameStarted) return;

        // === JUMP INPUT (Keyboard / Buttons) ===
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || AGR_MobileButtons.jumpPressed)
        {
            Jump();
        }

        // === SLIDE / FAST FALL (S key or mobile button) ===
        if (Input.GetKeyDown(KeyCode.S) || AGR_MobileButtons.fallPressed)
        {
            Slide();
        }

        // Handle slide duration
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                EndSlide();
            }
        }

        // === SWIPE / TOUCH INPUT ===
        if (AGR_SettingsManager.CurrentControl == AGR_SettingsManager.ControlType.Swipe ||
            AGR_SettingsManager.CurrentControl == AGR_SettingsManager.ControlType.Gyroscope)
        {
            HandleUnifiedSwipeInput();
        }

        // === LEFT/RIGHT INPUT (Keyboard always works) ===
        horizontalInput = Input.GetAxis("Horizontal");

        // Button mode: read mobile button states
        if (AGR_SettingsManager.CurrentControl == AGR_SettingsManager.ControlType.Buttons)
        {
            float mobileH = AGR_MobileButtons.leftHeld ? -1f : (AGR_MobileButtons.rightHeld ? 1f : 0f);
            if (mobileH != 0f) horizontalInput = mobileH;
        }

        // Trigger discrete strafe animations (Only for continuous steering)
        if (anim != null && AGR_SettingsManager.CurrentControl != AGR_SettingsManager.ControlType.Swipe)
        {
            if (horizontalInput < -0.1f && !dodgingLeft)
            {
                anim.SetTrigger("DodgeLeft");
                dodgingLeft = true;
                dodgingRight = false;
            }
            else if (horizontalInput > 0.1f && !dodgingRight)
            {
                anim.SetTrigger("DodgeRight");
                dodgingRight = true;
                dodgingLeft = false;
            }
            else if (Mathf.Abs(horizontalInput) < 0.1f)
            {
                dodgingLeft = false;
                dodgingRight = false;
            }
        }

        // Gyroscope mode
        if (AGR_SettingsManager.CurrentControl == AGR_SettingsManager.ControlType.Gyroscope)
        {
            if (SystemInfo.supportsGyroscope)
            {
                if (!Input.gyro.enabled) Input.gyro.enabled = true;
                float tilt = Input.gyro.gravity.x;
                if (Mathf.Abs(tilt) > 0.1f)
                {
                    horizontalInput = tilt * AGR_SettingsManager.GyroSensitivity * 2f;
                }
            }
        }

        // Ground check using raycast downward
        CheckGrounded();
    }

    // ============================================================
    // UNIFIED SWIPE INPUT — Works with Touch AND Mouse!
    // ============================================================
    // This replaces the old HandleTouchInput() method.
    // In Unity Editor: mouse drag = swipe, mouse click = tap/jump
    // On device: touch drag = swipe, touch tap = jump
    // ============================================================
    private void HandleUnifiedSwipeInput()
    {
        // --- TOUCH INPUT (Real device or Device Simulator) ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Don't process touches on UI elements
            if (touch.phase == TouchPhase.Began)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                inputStartPos = touch.position;
                isTrackingInput = true;
                swipeProcessed = false;
                inputStartTime = Time.unscaledTime;
            }

            if (touch.phase == TouchPhase.Ended && isTrackingInput)
            {
                ProcessSwipeEnd(touch.position);
                isTrackingInput = false;
            }

            // Cancel if touch was cancelled
            if (touch.phase == TouchPhase.Canceled)
            {
                isTrackingInput = false;
            }

            return; // Don't also process mouse when touches exist
        }

        // --- MOUSE INPUT (Editor testing & Device Simulator fallback) ---
        if (Input.GetMouseButtonDown(0))
        {
            // Don't process clicks on UI elements
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            inputStartPos = Input.mousePosition;
            isTrackingInput = true;
            swipeProcessed = false;
            inputStartTime = Time.unscaledTime;
        }

        if (Input.GetMouseButtonUp(0) && isTrackingInput)
        {
            ProcessSwipeEnd(Input.mousePosition);
            isTrackingInput = false;
        }
    }

    private void ProcessSwipeEnd(Vector2 endPos)
    {
        if (swipeProcessed) return;
        swipeProcessed = true;

        Vector2 swipeDelta = endPos - inputStartPos;
        float swipeTime = Time.unscaledTime - inputStartTime;
        float swipeDistance = swipeDelta.magnitude;

        // Scale threshold by screen DPI for consistent feel across devices
        float dpiScale = Mathf.Max(1f, Screen.dpi / 160f);
        float adjustedThreshold = SWIPE_THRESHOLD * dpiScale;

        // === TAP DETECTION (short time + small movement = jump) ===
        if (swipeTime < TAP_MAX_TIME && swipeDistance < TAP_MAX_DISTANCE * dpiScale)
        {
            Jump();
            return;
        }

        // === DIRECTIONAL SWIPE ===
        bool isHorizontal = Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y);

        if (isHorizontal && Mathf.Abs(swipeDelta.x) > adjustedThreshold)
        {
            // ONLY process horizontal swipes in pure Swipe mode (Gyro handles its own steering)
            if (AGR_SettingsManager.CurrentControl == AGR_SettingsManager.ControlType.Swipe)
            {
                // SWIPE LEFT or RIGHT — change lane
                if (swipeDelta.x > 0)
                {
                    currentLane = Mathf.Min(1, currentLane + 1);
                    if (anim != null) anim.SetTrigger("DodgeRight");
                    Debug.Log("SWIPE RIGHT → Lane " + currentLane);
                }
                else
                {
                    currentLane = Mathf.Max(-1, currentLane - 1);
                    if (anim != null) anim.SetTrigger("DodgeLeft");
                    Debug.Log("SWIPE LEFT → Lane " + currentLane);
                }
            }
        }
        else if (!isHorizontal && Mathf.Abs(swipeDelta.y) > adjustedThreshold)
        {
            if (swipeDelta.y > 0)
            {
                // SWIPE UP — Jump
                Jump();
                Debug.Log("SWIPE UP → Jump");
            }
            else
            {
                // SWIPE DOWN — Slide
                Slide();
                Debug.Log("SWIPE DOWN → Slide");
            }
        }
        else
        {
            // Ambiguous small movement — treat as tap/jump
            Jump();
        }
    }

    private void Jump()
    {
        if (jumpsRemaining > 0)
        {
            // Set velocity directly = INSTANT jump, no delay!
            Vector3 vel = rb.velocity;
            vel.y = jumpForce; 
            rb.velocity = vel;
            jumpsRemaining--;

            if (anim != null) 
            {
                anim.SetTrigger("Jump");
            }
            
            // Play SFX
            if (AGR_SFXManager.Instance != null) AGR_SFXManager.Instance.PlayJump();
        }
    }

    private void Slide()
    {
        // IN AIR: Slam down fast (no slide animation, just fast fall)
        if (!isGrounded)
        {
            Vector3 vel = rb.velocity;
            vel.y = -25f; // Slam down hard
            rb.velocity = vel;
            return; // Don't trigger ground slide
        }

        // ON GROUND: Trigger slide state
        if (!isSliding)
        {
            isSliding = true;
            slideTimer = slideDuration;

            // Trigger the slide ANIMATION (not just shrink!)
            if (anim != null)
            {
                anim.Play("Slide", -1, 0f); // Bypasses all blend delays!
                anim.speed = 1.5f; // Fast, snappy slide animation!
            }

            // Shrink the COLLIDER to pass under obstacles
            // But DON'T shrink the visual scale — the animation handles that!
            BoxCollider box = GetComponent<BoxCollider>();
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (box != null)
            {
                originalColliderSize = box.size;
                originalColliderCenter = box.center;
                box.size = new Vector3(box.size.x, box.size.y * 0.4f, box.size.z);
                box.center = new Vector3(box.center.x, box.center.y - box.size.y * 0.3f, box.center.z);
            }
            else if (capsule != null)
            {
                originalCapsuleHeight = capsule.height;
                originalCapsuleCenter = capsule.center;
                capsule.height *= 0.4f;
                capsule.center = new Vector3(capsule.center.x, capsule.center.y - capsule.height * 0.3f, capsule.center.z);
            }
            else
            {
                // Fallback: shrink scale if no proper collider found
                Vector3 targetScale = originalScale;
                targetScale.y = originalScale.y * 0.5f;
                transform.localScale = targetScale;
            }
        }
    }

    private void EndSlide()
    {
        isSliding = false;

        // Restore animation speed back to normal running speed
        if (anim != null && !isDead)
        {
            anim.speed = 1.15f; 
        }

        // Restore collider
        BoxCollider box = GetComponent<BoxCollider>();
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (box != null && originalColliderSize != Vector3.zero)
        {
            box.size = originalColliderSize;
            box.center = originalColliderCenter;
        }
        else if (capsule != null && originalCapsuleHeight > 0)
        {
            capsule.height = originalCapsuleHeight;
            capsule.center = originalCapsuleCenter;
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    private void CheckGrounded()
    {
        // Shoot a ray downward to check for ground
        bool wasGrounded = isGrounded;
        
        // Raycast down from the center of the player
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.7f);

        // Just landed — reset jumps
        if (!wasGrounded && isGrounded)
        {
            jumpsRemaining = maxJumps; // This will now correctly trigger as 1
        }
    }

    void FixedUpdate()
    {
        if (isDead || !gameStarted) return;

        // Move forward
        Vector3 vel = rb.velocity;
        vel.z = moveSpeed;
        
        // Handle Left/Right Movement based on exact control type
        if (AGR_SettingsManager.CurrentControl == AGR_SettingsManager.ControlType.Swipe)
        {
            // SWIPE MODE: Snap perfectly to 1 of 3 lanes
            vel.x = 0; // Let MoveTowards handle horizontal tracking
            float targetX = currentLane * maxX;
            
            Vector3 targetPos = transform.position;
            targetPos.x = Mathf.MoveTowards(targetPos.x, targetX, laneSpeed * 1.5f * Time.fixedDeltaTime);
            transform.position = targetPos;
        }
        else
        {
            // BUTTON/GYRO/KEYBOARD MODE: Continuous fluid steering
            vel.x = horizontalInput * laneSpeed;
        }

        // Apply constant extra gravity immediately when in the air for a snappy parkour jump
        if (!isGrounded)
        {
            vel.y -= extraGravity * Time.fixedDeltaTime;
        }

        rb.velocity = vel;

        // Clamp X position
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        transform.position = pos;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // STOP ALL MOVEMENT
        rb.velocity = Vector3.zero;
        rb.isKinematic = true; // Freeze the rigidbody completely!

        // STOP THE ANIMATOR — no more running animation!
        if (anim != null)
        {
            anim.speed = 0f; // Freeze animation in place
        }

        // End slide if sliding
        if (isSliding) EndSlide();

        Debug.Log("Player died! Game Over!");
    }

    public void ResetPlayer(Vector3 spawnPosition)
    {
        isDead = false;
        gameStarted = false;
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.isKinematic = false; // MUST unfreeze!
        
        if (anim != null) 
        {
            anim.speed = 1.15f;
            anim.Rebind();
            anim.Update(0f);
        }

        jumpsRemaining = maxJumps;
        currentLane = 0;
        EndSlide();
        dodgingLeft = false;
        dodgingRight = false;
    }
}
