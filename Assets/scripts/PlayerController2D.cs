using UnityEngine;

// Left/right + jump on the ground.
// Press Space in the air near a rail to grab it and ride it with your speed.
// Press Space on the rail to jump off and keep your speed (up to a limit).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    public float groundAccel = 80f;
    public float airAccel = 30f;
    public float jumpSpeed = 14f;

    [Header("Ground Check")]
    public Transform groundCheck;          // empty object at the player's feet
    public Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    public LayerMask groundLayer;

    [Header("Rail")]
    public float grabRadius = 1.5f;        // how close a rail must be to grab it
    public float playerRailOffset = 0.5f;  // how high above the rail you ride
    public float minRailSpeed = 4f;        // slowest speed when you grab on
    public float maxRailSpeed = 25f;
    public float railGravity = 20f;        // faster going downhill, slower uphill
    public float railJumpSpeed = 10f;      // upward pop when jumping off
    public float maxLaunchSpeed = 20f;     // momentum limit after leaving a rail
    public float regrabCooldown = 0.25f;   // wait before grabbing again

    Rigidbody2D rb;
    float inputX;
    bool spacePressed;
    bool grounded;

    Rail rail;
    float railDist;
    float railSpeed;
    float savedGravity;
    float cooldownTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        savedGravity = rb.gravityScale;
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space)) spacePressed = true;
    }

    void FixedUpdate()
    {
        cooldownTimer -= Time.fixedDeltaTime;
        grounded = groundCheck != null &&
                   Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (rail != null) RailMove();
        else NormalMove();

        spacePressed = false;
    }

    // ---------------- Normal movement ----------------
    void NormalMove()
    {
        Vector2 v = rb.velocity;
        float target = inputX * moveSpeed;

        // In the air, if you're going faster than walking (from a rail), keep that
        // speed unless you push the other way.
        bool fastInAir = !grounded && Mathf.Abs(v.x) > moveSpeed &&
                         (inputX == 0f || Mathf.Sign(inputX) == Mathf.Sign(v.x));

        if (!fastInAir)
        {
            float accel = grounded ? groundAccel : airAccel;
            v.x = Mathf.MoveTowards(v.x, target, accel * Time.fixedDeltaTime);
        }

        if (spacePressed)
        {
            if (grounded)
            {
                v.y = jumpSpeed;
            }
            else if (cooldownTimer <= 0f && TryGrabRail(v))
            {
                return; // now on a rail
            }
        }

        rb.velocity = v;
    }

    // ---------------- Grab a rail ----------------
    bool TryGrabRail(Vector2 velocity)
    {
        Vector2 feet = rb.position - Vector2.up * playerRailOffset;
        Rail best = null;
        float bestSq = grabRadius * grabRadius;
        float bestDist = 0f;

        foreach (Rail r in Rail.All)
        {
            if (!r.IsValid) continue;
            float d = r.ClosestDistance(feet, out Vector2 p);
            float sq = (p - feet).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = r;
                bestDist = d;
            }
        }

        if (best == null) return false;

        rail = best;
        railDist = bestDist;

        // Keep your momentum: only the part of your speed that goes along the rail.
        Vector2 tangent = rail.GetTangent(railDist);
        railSpeed = Vector2.Dot(velocity, tangent);
        if (Mathf.Abs(railSpeed) < minRailSpeed)
        {
            float dir = Mathf.Abs(railSpeed) > 0.01f ? Mathf.Sign(railSpeed)
                      : (Mathf.Sign(inputX * tangent.x) < 0f ? -1f : 1f);
            railSpeed = minRailSpeed * dir;
        }

        savedGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        return true;
    }

    // ---------------- Ride the rail ----------------
    void RailMove()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 tangent = rail.GetTangent(railDist);

        // Slopes: speed up going down, slow down going up.
        railSpeed += -railGravity * tangent.y * dt;
        railSpeed = Mathf.Clamp(railSpeed, -maxRailSpeed, maxRailSpeed);
        railDist += railSpeed * dt;

        // Fell off the end of the rail: keep going.
        if (railDist <= 0f || railDist >= rail.Length)
        {
            LeaveRail(tangent * railSpeed);
            return;
        }

        // Jumped off: keep speed plus a little pop up.
        if (spacePressed)
        {
            LeaveRail(tangent * railSpeed + Vector2.up * railJumpSpeed);
            return;
        }

        Vector2 target = rail.GetPoint(railDist) + Vector2.up * playerRailOffset;
        rb.velocity = (target - rb.position) / dt;
    }

    void LeaveRail(Vector2 launch)
    {
        rail = null;
        rb.gravityScale = savedGravity;
        rb.velocity = Vector2.ClampMagnitude(launch, maxLaunchSpeed);
        cooldownTimer = regrabCooldown;
    }

    // Shows the ground check box and grab range in the Scene view.
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position - Vector3.up * playerRailOffset, grabRadius);
    }
}
