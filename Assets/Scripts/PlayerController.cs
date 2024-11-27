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
    //private void FixedUpdate()
    //{
    //    Vector2 playerInput = new Vector2(Input.GetAxis("Horizontal"), 0);
    //    MovementUpdate(playerInput);
    //}
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

        
        //old movement code{
        //rb = GetComponent<Rigidbody2D>();

        ////switch between different properties
        //switch (properties) {
        //    case MotionProperties.Idle:
        //        //when players Idle, no moving
        //        speed = 0;
        //        //if there's input, start to acceleration
        //        if (playerInput != Vector2.zero)
        //        {
        //            properties = MotionProperties.Acceleration;
        //        }
        //            break;
        //    case MotionProperties.MaxSpeed:
        //        //MaxSpeed -> player stay at max speed
        //        speed = maxSpeed;
        //        //if nothing pressed, start to deceleration
        //        if (playerInput == Vector2.zero)
        //        {
        //            properties = MotionProperties.Deceleration;
        //            break;
        //        }
        //        //make sure player moving direction is same with input
        //        direction = playerInput.normalized;
        //            break;
        //    case MotionProperties.Acceleration:
        //        //when player at Acceleration, accelerate
        //        speed += acceleration * Time.deltaTime;
        //        //if speed reach to max speed, player at MaxSpeed mode.
        //        if (speed >= maxSpeed)
        //        {
        //            properties = MotionProperties.MaxSpeed;
        //        }
        //        //if release the button during acceleration, at deceleration mode
        //        if (playerInput == Vector2.zero)
        //        {
        //            properties = MotionProperties.Deceleration;
        //            break;
        //        }
        //        direction = playerInput.normalized;
        //        break;
        //    case MotionProperties.Deceleration:
        //        //decelerate
        //        speed -= deceleration * Time.deltaTime;
        //        //when speed - deceleration < 0, player stay
        //        if (speed < deceleration*Time.deltaTime) 
        //        {
        //            properties = MotionProperties.Idle;
        //        }
        //        //if there's new input while decelerate, accelerate again.
        //        if (playerInput != Vector2.zero)
        //        {
        //            properties = MotionProperties.Acceleration;
        //        }
        //            break;
        //}

        //    //move the player
        //    rb.MovePosition(rb.position + speed * direction * Time.deltaTime);
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
