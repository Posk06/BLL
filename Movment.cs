using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
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
        grounded = Physics.Raycast(transform.position + cap.center, Vector3.down, cap.height * 0.5f + 0.1f, whatisGround);

        myInput();
        speedControl();

        if (grounded) { rb.drag = groundDrag; } else { rb.drag = 0f; }
        if (new Vector2(rb.velocity.x, rb.velocity.z).magnitude == 0) { standing = true; } else { standing = false; }
        if (rb.velocity.y != 0) { falling = true; standing = false; } else { falling = false; }
        if (standing && sprinting) { resetSprint(); }
        if (!Input.GetKey(duckKey) && ducking) { resetDuck(); }
        if(standing) { rb.drag = 1000; }

        animator.SetBool("isWalking", !standing && !falling);
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
        rb.drag = groundDrag;
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
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void resetJump()
    {
        readytoJump = true;
    }

    private void sprint()
    {
        moveSpeed *= 1.5f;
    }

    private void resetSprint()
    {
        sprinting = false;
        moveSpeed /= 1.5f;
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

