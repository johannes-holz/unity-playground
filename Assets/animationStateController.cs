using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationStateController : MonoBehaviour
{
    Animator animator;
    private bool hasAnimator;
    private int animIDSpeed;
    private int animIDWalking;

    CharacterController controller;

    public float actionCooldown = 5f;
    float actionCooldownCur;


    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;
    private float fallTimeoutDelta;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    public float verticalVelocity;

    public float walkSpeed = 2f;
    public float acceleration;
    public Vector3 origPosition;
    public float maxDist = 10f;
    public float speedChangeRate = 4f;
    public float rotationSpeed = 2f;
    private float terminalVelocity = 53.0f;

    public float speed;
    float animationBlend;

    public Vector3 targetPosition;

    public Vector3 controllerTransform;
    //float isOnroute;

    // Start is called before the first frame update
    void Start()
    {
        hasAnimator = TryGetComponent<Animator>(out animator);
        controller = GetComponent<CharacterController>();
        origPosition = gameObject.transform.position;
        actionCooldownCur = Random.Range(0, actionCooldown);


        AssignAnimationIDs();
    }

    private void AssignAnimationIDs()
    {
        //animIDSpeed = Animator.StringToHash("Speed");
        animIDWalking = Animator.StringToHash("isWalking");
    }

    // Update is called once per frame
    void Update()
    {
        if (actionCooldownCur < 0)
        {
            //Debug.Log("NPC Do something new!");
            actionCooldownCur = actionCooldown + actionCooldownCur;
            SetNewDestination();

        } else
        {
            actionCooldownCur -= Time.deltaTime;
        }

        GroundedCheck();
        JumpAndGravity();
        Move();
    }

    private void SetNewDestination()
    {

        targetPosition = origPosition + new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1)) * maxDist / Mathf.Sqrt(2);
        //targetPosition = Quaternion.Euler(0, Random.Range(-180, 180), 0) * new Vector3(1, 0, 1) * maxDist / Mathf.Sqrt(2);
        //targetPosition = controller.transform.position + new Vector3(maxDist, 0, maxDist);
        //transform.LookAt(targetPosition);
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
            Debug.Log("grounded status changed: " + Grounded);
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
                if (hasAnimator)
                {
                    //_animator.SetBool(_animIDFreeFall, true);
                }
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
        float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;

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
            speed += (targetSpeed - currentHorizontalSpeed) * Time.deltaTime * speedChangeRate;
            // round speed to 3 decimal places
            //speed = Mathf.Round(speed * 1000f) / 1000f;
           // Debug.Log("speed " + speed);
        }
        else
        {
            speed = targetSpeed;
        }

        controllerTransform = controller.transform.position;


        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;

        bool walking = speed > 0.15 ? true : false;

        //find the vector pointing from our position to the target
        //Vector3 direction = horDir.normalized;

        //create the rotation we need to be in to look at the target
        Quaternion lookRotation = Quaternion.LookRotation(horDir);

        //rotate us over time according to speed until we are in the required rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        

        // horDir * (speed * Time.deltaTime) + 
        controller.Move(transform.forward * (speed * Time.deltaTime) + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);
        if (hasAnimator)
        {
            animator.SetBool(animIDWalking, walking);
        }
    }

    private void SimpleMove()
    {

        //find the vector pointing from our position to the target
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        //create the rotation we need to be in to look at the target
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        //rotate us over time according to speed until we are in the required rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        float targetSpeed = (targetPosition - transform.position).magnitude > 1 ? walkSpeed : 0;

        controller.SimpleMove(forward * (targetSpeed * Time.deltaTime));
    }
}
