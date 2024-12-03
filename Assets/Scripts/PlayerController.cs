using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum FacingDirection
    {
        left, right
    }
    FacingDirection direction;
    //set motion properties
    //public enum MotionProperties
    //{
    //    Idle, MaxSpeed, Acceleration, Deceleration, Turning
    //}

    Rigidbody2D rb;
    //public MotionProperties properties;
    //public float speed;
    public float maxSpeed;
    public float accelerationTime;
    public float decelerationTime;
    private float acceleration;
    private float deceleration;
    //public Vector2 direction;

    //for easier find player movement issue
    [SerializeField] Vector2 currentVelocity;

    //Jump variables
    public float apexHeight;
    public float apexTime;
    public float terminalSpeed;
    public float coyoteTime;

    private float jumpGravity;
    public float gravity;
    private float initialJumpVelocity;
    private bool isJumping = false;
    bool isInCoyoteTime = false;
    bool isReallyGrounded = true;

    //Dash
    public float timesMaxSpeed;  //several times the max speed
    public float dashTime;       //how long the dash movement take
    public float dashCoolDown;   //not let player can dash all the time so add cool down timer
    bool isDashing = false;
    bool canDash = true;

    //Double Jump
    [SerializeField] bool canDoubleJump = false;
    //To stop first jump's fall corountine
    Coroutine falling = null;

    //Hit bounce
    Collider2D player;
    public float hitAnimationDuration;
    bool isHit = false;

    // Start is called before the first frame update
    void Start()
    {
        acceleration = maxSpeed / accelerationTime;
        deceleration = maxSpeed / decelerationTime;

        rb = GetComponent<Rigidbody2D>();
        //Jump calculate
        rb.gravityScale=gravity;
        jumpGravity = -2 * apexHeight / Mathf.Pow(apexTime, 2);
        initialJumpVelocity = 2*apexHeight/apexTime;

        player = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //The input from the player needs to be determined and then passed in the to the MovementUpdate which should
        //manage the actual movement of the character.
        Vector2 playerInput = new Vector2(Input.GetAxis("Horizontal"), 0);
        MovementUpdate(playerInput);
    }
  
    private void MovementUpdate(Vector2 playerInput)
    {
         currentVelocity = rb.velocity;

        //calculate dash before walk
        if (isDashing)
        {
            //stop gravity so player won't fall while dashing in air.
            rb.gravityScale = 0;
            //dash at a fixed speed. 
            currentVelocity.x = timesMaxSpeed * maxSpeed * playerInput.x;
        }
        //player start to move when there's player input
        else if (playerInput != Vector2.zero)
        {
            //get player direction
            direction = playerInput.x < 0 ? FacingDirection.left : FacingDirection.right;
            //player accelerate 
            currentVelocity.x += acceleration * playerInput.x * Time.deltaTime;
            //limit player's speed
            currentVelocity.x = Mathf.Clamp(currentVelocity.x, -maxSpeed, maxSpeed); 
        }
        else if (Mathf.Abs(currentVelocity.x) > 0)
        {
            //during decelerate stage, make sure player won't move to other diraction
            currentVelocity.x =currentVelocity.normalized.x * Mathf.Clamp(Mathf.Abs(currentVelocity.x) - deceleration * Time.deltaTime, 0, maxSpeed);
        }

        //Jump
        //when player on ground,start to jump when there's player input
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            isJumping = true;
            //when player jump off the ground,end coyote time
            isInCoyoteTime = false;
            //during first jump, let player can double jump
            canDoubleJump = true;
            //turn off gravity while jumping
            rb.gravityScale = 0;
            //give player the initial jump velocity
            currentVelocity.y = initialJumpVelocity;
            //call Fall coroutine. 
            falling=StartCoroutine(Fall());
        }
        //Double jump
        else if (Input.GetKeyDown(KeyCode.Space) && canDoubleJump)
        {
            isJumping = true;
            //so player won't do infinity jump
            canDoubleJump =false;
            rb.gravityScale = 0;
            currentVelocity.y = initialJumpVelocity;
            //stop first jump's falling stage
            if (falling != null) StopCoroutine(falling);
            StartCoroutine(Fall());
        }

        if (isJumping)
        {
            //upward phase of the jump
            currentVelocity.y += jumpGravity * Time.deltaTime;
        }
        
        //Dash
        if (Input.GetKeyDown(KeyCode.LeftControl) && canDash)
        {       
            StartCoroutine(Dash());
        }
        //player fallen speed won't exceed terminal speed
        currentVelocity.y = Mathf.Clamp(currentVelocity.y, -terminalSpeed, initialJumpVelocity);
        //update rigidbody's volocity
        rb.velocity = currentVelocity;        
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if hit the spines, play hit animation
        if (collision.gameObject.CompareTag("Danger"))
        {
            isHit = true;
            StartCoroutine(HitAnimation());
        }
    }

    IEnumerator Fall()
    {
        //after apex time, recall gravity
        yield return new WaitForSeconds(apexTime);
        rb.gravityScale = gravity;        
        isJumping = false;
    }
    IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;
        yield return new WaitForSeconds (dashTime);
        isDashing = false;
        //when end dash, recall rigidbody's gravity
        rb.gravityScale = gravity;
        //make sure player can't dash all the time
        yield return new WaitForSeconds(dashCoolDown);
        canDash = true;
    }
    IEnumerator HitAnimation()
    {
        yield return new WaitForSeconds(hitAnimationDuration);
        isHit = false;
    }
    public bool IsWalking()
    {
        return rb.velocity.x != 0;
    }
    public bool IsDashing()
    {
        return isDashing;
    }
    public bool IsHit()
    {
        return isHit;
    }
    public bool IsGrounded()
    {
        Debug.DrawRay(transform.position, Vector2.down * 0.7f, Color.red);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.7f,1<<0|0<<2);
        if(isReallyGrounded != (hit.collider != null))
        {
            StartCoroutine(coyoteTimeCount()); 
        }
        isReallyGrounded = hit.collider != null;
        return isInCoyoteTime || isReallyGrounded;
    }
    IEnumerator coyoteTimeCount()
    {
        isInCoyoteTime = true;
        yield return new WaitForSeconds(coyoteTime);
        isInCoyoteTime = false;
    }
    public FacingDirection GetFacingDirection()
    {
        return direction; 
    }
}
