using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float groundAccel = 60f;     // lower = more slide on the ground
    public float airAccel = 25f;        // lower = harder to turn in the air
    public float jumpSpeed = 14f;
    public float coyoteTime = 0.1f;     // seconds you can still jump after leaving the ground
    float coyoteTimer;
    public float groundDist = 0.6f;     // half the player's height + a little
    public LayerMask groundLayer;

    public float railJumpSpeed = 12f;
    public float railSpeedBoost = 1.2f; // jumping off a rail = speed x this
    public float railAccel = 6f;        // how fast you speed up on a rail
    public float maxRailSpeed = 20f;    // top speed on a rail
    public LayerMask railLayer;

    Rigidbody2D rb;
    Collider2D col;
    public float deathY = -10f;
    float x, gravity, railSpeed, railCooldown;
    bool space, onRail;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // no clipping at high speed
        gravity = rb.gravityScale;
    }

    void Update()
    {
        x = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space)) space = true;

        if (transform.position.y <= deathY) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Face the way you're moving (flips children too)
        Vector3 s = transform.localScale;
        if (rb.velocity.x > 0.1f) s.x = Mathf.Abs(s.x);
        else if (rb.velocity.x < -0.1f) s.x = -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    void FixedUpdate()
    {
        railCooldown -= Time.fixedDeltaTime;
        if (onRail)
        {
            if (space)
            {
                LeaveRail();
                railCooldown = 0.25f; // don't grab the rail again right away
                rb.velocity = new Vector2(railSpeed * railSpeedBoost, railJumpSpeed);
            }
            else if (!Cast(rb.position, railLayer, groundDist))
            {
                LeaveRail(); // green line left the rail = fall
            }
            else
            {
                railSpeed = Mathf.MoveTowards(railSpeed, Mathf.Sign(railSpeed) * maxRailSpeed, railAccel * Time.fixedDeltaTime); // build speed while grinding
                rb.velocity = new Vector2(railSpeed, 0); // y locked, keep sliding
            }
        }
        else if (!TryGrabRail())
        {
            bool grounded = Cast(rb.position, groundLayer, groundDist);
            Vector2 v = rb.velocity;

            // Speed up / slow down smoothly. Holding forward in the air keeps your rail boost.
            float accel = grounded ? groundAccel : airAccel;
            bool keepBoost = !grounded && Mathf.Abs(v.x) > moveSpeed && x * v.x > 0;
            if (!keepBoost) v.x = Mathf.MoveTowards(v.x, x * moveSpeed, accel * Time.fixedDeltaTime);
            if (grounded) coyoteTimer = coyoteTime;
            else coyoteTimer -= Time.fixedDeltaTime;
            if (space && coyoteTimer > 0) { v.y = jumpSpeed; coyoteTimer = 0; } // can still jump just after walking off a ledge

            rb.velocity = v;
        }
        space = false;
    }

    bool TryGrabRail()
    {
        Vector2 v = rb.velocity;
        if (v.y > 0 || railCooldown > 0) return false;

        // Green line must hit a rail. Look further when falling fast so you can't skip past it.
        float reach = groundDist - v.y * Time.fixedDeltaTime;
        RaycastHit2D hit = Cast(rb.position, railLayer, reach);
        if (!hit || hit.point.y > col.bounds.min.y + 0.05f) return false; // rail must be under your feet

        onRail = true;
        railSpeed = Mathf.Abs(v.x) < moveSpeed ? (v.x < 0 ? -1 : 1) * moveSpeed : v.x;
        rb.gravityScale = 0;
        rb.velocity = new Vector2(railSpeed, 0);
        rb.position = new Vector2(rb.position.x, hit.point.y + col.bounds.extents.y); // sit on top, no sinking
        return true;
    }

    void LeaveRail()
    {
        onRail = false;
        rb.gravityScale = gravity;
    }

    // 5 lines under the player: both edges, the quarters, and the middle
    static readonly float[] rayX = { -0.95f, -0.5f, 0f, 0.5f, 0.95f };

    RaycastHit2D Cast(Vector2 from, LayerMask mask, float dist)
    {
        float w = col.bounds.extents.x;
        foreach (float f in rayX)
        {
            RaycastHit2D hit = Physics2D.Raycast(from + Vector2.right * w * f, Vector2.down, dist, mask);
            if (hit && hit.distance > 0) return hit; // any line touching counts
        }
        return default;
    }

    void OnDrawGizmos()
    {
        Collider2D c = col ? col : GetComponent<Collider2D>();
        if (!c) return;
        Gizmos.color = Color.green;
        foreach (float f in rayX)
        {
            Vector3 start = transform.position + Vector3.right * c.bounds.extents.x * f;
            Gizmos.DrawLine(start, start + Vector3.down * groundDist);
        }
    }
}