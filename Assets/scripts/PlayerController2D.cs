using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float jumpSpeed = 14f;
    public float groundDist = 0.6f;     // half the player's height + a little
    public LayerMask groundLayer;

    public float railJumpSpeed = 12f;
    public float railSpeedBoost = 1.2f; // jumping off a rail = speed x this
    public string railTag = "Rail";

    Rigidbody2D rb;
    float x, gravity, railSpeed, railCooldown;
    bool space, onRail;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        gravity = rb.gravityScale;
    }

    void Update()
    {
        x = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space)) space = true;
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
            else
            {
                rb.velocity = new Vector2(railSpeed, 0); // y locked, keep sliding
            }
        }
        else
        {
            bool grounded = Physics2D.Raycast(rb.position, Vector2.down, groundDist, groundLayer);
            Vector2 v = rb.velocity;

            // Normal speed on the ground. In the air, keep extra rail speed until you land.
            if (grounded || Mathf.Abs(v.x) <= moveSpeed) v.x = x * moveSpeed;
            if (space && grounded) v.y = jumpSpeed;

            rb.velocity = v;
        }
        space = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (onRail || railCooldown > 0 || !other.CompareTag(railTag) || rb.velocity.y > 0) return;

        onRail = true;
        railSpeed = rb.velocity.x;
        if (Mathf.Abs(railSpeed) < moveSpeed) railSpeed = (railSpeed < 0 ? -1 : 1) * moveSpeed;
        rb.gravityScale = 0;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (onRail && other.CompareTag(railTag)) LeaveRail(); // ran off the end = fall
    }

    void LeaveRail()
    {
        onRail = false;
        rb.gravityScale = gravity;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDist);
    }
}