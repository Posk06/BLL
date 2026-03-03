using UnityEngine;

public class Movment : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readytoJump = true;
    bool sprinting = false;
    bool standing = true;
    bool ducking = false;
    bool falling = false;
    [Header("Key Binds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode duckKey = KeyCode.C;

    [Header("Groundcheck")]
    public LayerMask whatisGround;
    bool grounded;

    [Header("Object Relations")]
    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;
    CapsuleCollider cap;
    Animator animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cap = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        grounded = Physics.Raycast(new Vector3(transform.position.x, cap.transform.position.y, transform.position.z), Vector3.down, cap.height * 0.5f + 0.1f, whatisGround);

        myInput();
        speedControl();

        if (grounded) { rb.linearDamping = groundDrag; } else { rb.linearDamping = 0f; }
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f || flatVel.magnitude > 0.1f) { standing = false; } else { standing = true;  }
        if (!grounded) { falling = true; standing = false;} else { falling = false; }
        if (standing && sprinting) { resetSprint(); }
        if (!Input.GetKey(duckKey) && ducking) { resetDuck(); }

        animator.SetBool("isWalking", !standing && grounded);
        animator.SetBool("isRunning", sprinting);
    }

    private void FixedUpdate()
    {
        movePlayer();
    }

    private void myInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && readytoJump && grounded)
        {
            readytoJump = false;

            jump();

            Invoke(nameof(resetJump), jumpCooldown);
        }

        if (Input.GetKey(sprintKey) && !sprinting && !ducking)
        {
            sprinting = true;

            sprint();

        }

        if (Input.GetKey(duckKey) && !ducking)
        {
            if (sprinting)
            {
                ducking = true;

                duck();
            }
            else
            {
                ducking = true;

            duck();
            }
        }
    }

    private void movePlayer()
    {
        rb.linearDamping = groundDrag;
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void speedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void resetJump()
    {
        readytoJump = true;
    }

    private void sprint()
    {
        moveSpeed *= 3f;
    }

    private void resetSprint()
    {
        sprinting = false;
        moveSpeed /= 3f;
    }

    private void duck()
    {
        cap.height *= 0.5f;
    }

    private void resetDuck()
    {
        ducking = false;

        cap.height *= 2f;
    }

}   

