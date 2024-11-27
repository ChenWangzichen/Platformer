using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
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
    }

    // Update is called once per frame
    void Update()
    {
        //The input from the player needs to be determined and then passed in the to the MovementUpdate which should
        //manage the actual movement of the character.
        Vector2 playerInput = new Vector2(Input.GetAxis("Horizontal"), 0);
        MovementUpdate(playerInput);
    }

    public Vector2 currentVelocity;
    private void MovementUpdate(Vector2 playerInput)
    {
         currentVelocity = rb.velocity;

        if (playerInput != Vector2.zero)
        {
            direction = playerInput.x < 0 ? FacingDirection.left : FacingDirection.right;
            currentVelocity += acceleration * playerInput * Time.deltaTime;
            currentVelocity.x = Mathf.Clamp(currentVelocity.x, -maxSpeed, maxSpeed);
        }
        else if (Mathf.Abs(currentVelocity.x) > 0)
        {
            currentVelocity.x =currentVelocity.normalized.x * Mathf.Clamp(Mathf.Abs(currentVelocity.x) - deceleration * Time.deltaTime, 0, maxSpeed);
        }
        //Jump

        if (Input.GetKey(KeyCode.Space) && IsGrounded())
        {
            isJumping = true;
            isInCoyoteTime = false;
            rb.gravityScale = 0;
            currentVelocity.y = initialJumpVelocity;
            StartCoroutine(Fall());
        }
        if (isJumping)
        {
            currentVelocity.y += jumpGravity * Time.deltaTime;
        }

        currentVelocity.y = Mathf.Clamp(currentVelocity.y, -terminalSpeed, initialJumpVelocity);
        
        rb.velocity = currentVelocity;
        
    }
    IEnumerator Fall()
    {
        yield return new WaitForSeconds(apexTime);
        rb.gravityScale = gravity;        
        isJumping = false;
    }
    public bool IsWalking()
    {
        return rb.velocity.x != 0;
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
