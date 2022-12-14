using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnotherCCScript : MonoBehaviour
{
    public CharacterController _controller;

    public Vector3 origPosition;
    public Vector3 targetPosition;
    public float maxDist = 10f;

    public float actionCooldown = 5f;
    float actionCooldownCur;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;
    private float fallTimeoutDelta;
    
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;
    public float verticalVelocity;
    private float terminalVelocity = 53.0f;


    public float walkSpeed = 2f;
    public float speed;

    public float acceleration = 4f;
    public float rotAcceleration = 2f;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (actionCooldownCur < 0)
        {
            //Debug.Log("NPC Do something new!");
            actionCooldownCur = actionCooldown + actionCooldownCur;
            SetNewDestination();

        }
        else
        {
            actionCooldownCur -= Time.deltaTime;
        }

        GroundedCheck();
        JumpAndGravity();
        Move();

    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);

        bool oldGrounded = Grounded;
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);
        if (oldGrounded != Grounded)
        {
            Debug.Log(gameObject.name + "'s grounded status changed: " + Grounded);
        }


        // update animator if using character
        //if (hasAnimator)
        //{
        //    animator.SetBool(animIDGrounded, Grounded);
        //}
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            fallTimeoutDelta = FallTimeout;

            // update animator if using character
            //if (_hasAnimator)
            //{
            //    _animator.SetBool(_animIDJump, false);
            //    _animator.SetBool(_animIDFreeFall, false);
            //}

            // stop our velocity dropping infinitely when grounded
            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            // Jump
            //if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            //{
            //    // the square root of H * -2 * G = how much velocity needed to reach desired height
            //    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            //    // update animator if using character
            //    if (_hasAnimator)
            //    {
            //        _animator.SetBool(_animIDJump, true);
            //    }
            //}

            //// jump timeout
            //if (_jumpTimeoutDelta >= 0.0f)
            //{
            //    _jumpTimeoutDelta -= Time.deltaTime;
            //}
        }
        else
        {
            // reset the jump timeout timer
            //_jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                //if (hasAnimator)
                //{
                //    _animator.SetBool(_animIDFreeFall, true);
                //}
            }

            // if we are not grounded, do not jump
            //_input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private void Move()
    {
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        Vector3 horDir = targetPosition - transform.position;
        horDir.y = 0;

        //Debug.Log("Horizontal direction: " + horDir + ", Magnitude: " + horDir.magnitude);

        float targetSpeed = horDir.magnitude > 0.5 ? walkSpeed : 0;
        horDir = horDir.normalized;

        float speedOffset = 0.1f;
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            //speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed,
            //    Time.deltaTime * speedChangeRate);

            // would have to divide with the abs of the difference for constant changerate
            // but it looks good like this imo
            speed += (targetSpeed - currentHorizontalSpeed) * Time.deltaTime * acceleration;
            // round speed to 3 decimal places
            //speed = Mathf.Round(speed * 1000f) / 1000f;
            // Debug.Log("speed " + speed);
        }
        else
        {
            speed = targetSpeed;
        }

        speed = targetSpeed;

        //controllerTransform = _controller.transform.position;


        //animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
        //if (animationBlend < 0.01f) animationBlend = 0f;

        bool walking = speed > 0.15 ? true : false;

        //find the vector pointing from our position to the target
        //Vector3 direction = horDir.normalized;

        //create the rotation we need to be in to look at the target
        Quaternion lookRotation = Quaternion.LookRotation(horDir);

        //rotate us over time according to speed until we are in the required rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotAcceleration);



        // horDir * (speed * Time.deltaTime) + 
        _controller.Move(transform.forward * (speed * Time.deltaTime) + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);
        //if (hasAnimator)
        //{
        //    animator.SetBool(animIDWalking, walking);
        //}
    }


    private void SetNewDestination()
    {

        targetPosition = origPosition + new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1)) * maxDist / Mathf.Sqrt(2);
    }
}
