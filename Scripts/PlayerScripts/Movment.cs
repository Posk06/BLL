using UnityEngine;
using UnityEngine.UIElements;

public class Movment : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    float jumpCooldownCounter = 0f;
    public float airMultiplier;
    bool readytoJump = true;
    bool sprinting = false;
    bool walkingForward = false, walkingBackward = false, walkingRight = false, walkingLeft = false;
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

    public static Movment instance;

    float horizontalInput;
    float verticalInput;

    Rigidbody rb;
    CapsuleCollider cap;
    Animator animator;

    int forward = 1;
    int right_forward = 2;
    int right = 3;
    int right_back = 4;
    int back = 5;
    int left_back = 6;
    int left = 7;
    int left_forward = 8;
    int not_moving = 0;

    private void Start()
    {
        instance = this;
        rb = GetComponent<Rigidbody>();
        cap = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        grounded = Physics.Raycast(new Vector3(transform.position.x, transform.position.y, transform.position.z), Vector3.down, 0.04f, whatisGround);

        myInput();

        jumpCooldownCounter -= Time.deltaTime;

        if (grounded) { rb.linearDamping = groundDrag; } else { rb.linearDamping = groundDrag; }
        if(Input.GetKeyUp(jumpKey) && jumpCooldownCounter <= 0f) { readytoJump = true; }



    
    }

    private void FixedUpdate()
    {
        movePlayer();
    }

    private void myInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && readytoJump && grounded && sprinting)
        {
            readytoJump = false;

            sprintJump();

            jumpCooldownCounter = jumpCooldown;
        }

        if (Input.GetKey(sprintKey) && !sprinting && !ducking && walkingForward)
        {
            sprint();
        } 
        else if (Input.GetKeyUp(sprintKey) && sprinting || !walkingForward)
        {
            resetSprint();
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

        if(Input.GetKeyDown(KeyCode.F)) {
            Debug.Log("Saved");
            PlayerSaveSystem.Save();
        }

        if(Input.GetKeyDown(KeyCode.G)) {
            Debug.Log("Loaded");
            PlayerSaveSystem.Load();
        }
    }    

    private void movePlayer()
    {
        rb.linearDamping = groundDrag;

        if (verticalInput > 0.01f)
        {
            walkingBackward = false;
            walkingForward = true;

            if(horizontalInput > 0.01f)
            {
                walkingLeft = false;
                walkingRight = true;
                animator.SetInteger("Direction", right_forward);
            } else if(horizontalInput < -0.01f)
            {
                walkingRight = false;
                walkingLeft = true;
                animator.SetInteger("Direction", left_forward);
            } else
            {
                walkingRight = false;
                walkingLeft = false;
                animator.SetInteger("Direction", forward);
            }
        }
        else if (verticalInput < -0.01f)
        {
            walkingForward = false;
            walkingBackward = true;

            if(horizontalInput > 0.01f)
            {
                walkingLeft = false;
                walkingRight = true;
                animator.SetInteger("Direction", right_back);
            } else if(horizontalInput < -0.01f)
            {
                walkingRight = false;
                walkingLeft = true;
                animator.SetInteger("Direction", left_back);
            } else
            {
                walkingRight = false;
                walkingLeft = false;
                animator.SetInteger("Direction", back);
            }
        }

        else
        {
            if(horizontalInput > 0.01f)
            {
                walkingLeft = false;
                walkingRight = true;
                animator.SetInteger("Direction", right);
            } else if(horizontalInput < -0.01f)
            {
                walkingRight = false;
                walkingLeft = true;
                animator.SetInteger("Direction", left);
            } else
            {
                walkingRight = false;
                walkingLeft = false;
                animator.SetInteger("Direction", not_moving);
            }
        }

    }

    private void sprintJump()
    {
        animator.SetTrigger("jump");
    }

    private void resetJump()
    {
        if(Input.GetKeyUp(jumpKey))
        {
            readytoJump = true;
        }
    }

    private void sprint()
    {
        sprinting = true;
        animator.SetBool("isRunning", true);
    }

    private void resetSprint()
    {
        sprinting = false;
        animator.SetBool("isRunning", false);
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

    public void Save(ref PlayerSaveData data)
    {
        data.position = transform.position;
        data.rotation = transform.rotation;

    }

    public void Load(ref PlayerSaveData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotation;
    }

}

[System.Serializable]

public struct PlayerSaveData
{
    public Vector3 position;
    public Quaternion rotation;
}

