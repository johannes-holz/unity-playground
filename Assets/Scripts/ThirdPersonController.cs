﻿using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

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

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        public bool Sliding = false;
        public float CurSlideVelocity = 0f;
        public float MaxSlideVelocity = 3f;
        public bool WillSlideOnSlopes = true;
        public float SlopeLimit = 40f;
        private Vector3 hitNormal;
        public float debuggoYo = 0.2f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        // debug stuff
        private Vector3 _wallJumpSpherePos;
        private float _wallJumpSphereRad;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // Kappa. Put this in a gamecontroller
            if (_input.toggleFullscreen)
            {
                Screen.fullScreen = !Screen.fullScreen;
            }


            // just put here to debug. should be in walljump if condition in jump and gravity
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            float yDir = Mathf.Atan2(-inputDirection.x, -inputDirection.z) * Mathf.Rad2Deg +
                          _mainCamera.transform.eulerAngles.y;
            Vector3 worldDir = Quaternion.Euler(new Vector3(0f, yDir, 0f)) * new Vector3(0f, 0f, 1f);
            _wallJumpSphereRad = _controller.radius * 1.2f;
            _wallJumpSpherePos = new Vector3(transform.position.x, transform.position.y + _wallJumpSphereRad, transform.position.z) + worldDir * _wallJumpSphereRad;

            SomeAction();
            JumpAndGravity();
            GroundedCheck();
            Move();

            CameraRotation();
        }

        private void LateUpdate()
        {
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void SomeAction()
        {
            if (_input.something)
            {
                Vector3 pos = gameObject.transform.position;
                Debug.LogError(pos);
                _controller.enabled = false;
                gameObject.transform.position = new Vector3(0, 0, 0);

                _controller.enabled = true;

                _input.something = false;
            }
        }

        public void ParentTransform(Transform trans)
        {
            _controller.enabled = false;
            transform.parent = trans;
            _controller.enabled = true;
        }

        Ray rayYo;
        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            //bool oldGrounded = Grounded;
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            //Collider[] cols = Physics.OverlapSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            //foreach (Collider col in cols) {
            //    Vector3 cPoint = col.ClosestPoint(spherePosition);
            //   // Gizmos.DrawSphere(cPoint, 0.2f);
            //    var dir = (spherePosition - cPoint).normalized;
            //    var ray = new Ray(spherePosition, dir);
            //    rayYo = ray;
            //    var hasHit = Physics.Raycast(ray, out var hitInfo, float.MaxValue);

            //}
            //if (oldGrounded != Grounded)
            //{
            //    Debug.Log("Grounded status changed: " + Grounded);
            //}
            Debug.DrawRay(spherePosition, Vector3.down, Color.red);
            //if (Grounded && Physics.Raycast(spherePosition, Vector3.down, out RaycastHit slopeHit, GroundedRadius, GroundLayers)) {
            //    hitNormal = slopeHit.normal;
            //    Sliding = Vector3.Angle(hitNormal, Vector3.up) > SlopeLimit;

            spherePosition.y += GroundedRadius;
            //if (Grounded && Physics.SphereCast(spherePosition, GroundedRadius, Vector3.down, out RaycastHit slopeHit, GroundedRadius, GroundLayers))
            //{
            //    hitNormal = slopeHit.normal;
            //    Sliding = Vector3.Angle(hitNormal, Vector3.up) > SlopeLimit;
            bool ignoreFirstZero = true;
            if (Grounded)
            {
                RaycastHit[] hits = Physics.SphereCastAll(spherePosition, GroundedRadius, Vector3.down, debuggoYo, GroundLayers);
                float minSlope = 200f;
                string debuggo = "";
                foreach (RaycastHit slopeHit in hits)
                {
                    float angle = Vector3.Angle(slopeHit.normal, Vector3.up);
                    debuggo += angle + ", ";
                    if (angle < 0.01 & ignoreFirstZero)
                    {
                        ignoreFirstZero = false;
                        continue;
                    }
                    if (angle < minSlope)
                    {
                        minSlope = Mathf.Min(minSlope, angle);
                        hitNormal = slopeHit.normal;
                    }
                }
                minSlope = minSlope > 180f ? 0f : minSlope;
                Debug.Log(debuggo);
                //Debug.Log(Vector3.Angle(hitNormal, Vector3.up));
                Sliding = minSlope > SlopeLimit;
            } else
            {
                Sliding = false;
            }



            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }


        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // TODO: need camspeed settings per device (mouse/controller)
                // Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 2.0f : Time.deltaTime;

                float camSpeed = 1.0f;

                _cinemachineTargetYaw += camSpeed * _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += camSpeed * _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                if (_input.aim)
                {
                    Debug.Log("AIMING");
                    transform.rotation = Quaternion.Euler(0.0f, _mainCamera.transform.eulerAngles.y, 0.0f);
                }
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            float targetSlideVelocity = Sliding ? MaxSlideVelocity : 0;
            if (CurSlideVelocity < targetSlideVelocity - speedOffset ||
                CurSlideVelocity > targetSlideVelocity + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                CurSlideVelocity = Mathf.Lerp(CurSlideVelocity, targetSlideVelocity,
                    Time.deltaTime * 1.0f);

                // round speed to 3 decimal places
                CurSlideVelocity = Mathf.Round(CurSlideVelocity * 1000f) / 1000f;
            }
            else
            {
                CurSlideVelocity = targetSlideVelocity;
            }

            // move the player
            if (targetSlideVelocity > 0.2f)
            {
                Vector3 slideDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);

                slideDirection = Vector3.ProjectOnPlane(_controller.velocity, hitNormal);
                if (slideDirection.magnitude > 0)
                {
                    //slideDirection = Vector3.MoveTowards(slideDirection, Vector3.zero, 0.1f * Time.deltaTime);
                    slideDirection *= (1f - 0.1f * Time.deltaTime);
                }
                _controller.Move(slideDirection * Time.deltaTime);

                //_controller.Move(slideDirection.normalized * (CurSlideVelocity * Time.deltaTime) + slideDirection.normalized * (_speed * Time.deltaTime));
            } else {

                //Physics.SyncTransforms();
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            }
            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }

                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // check if walljumping
                if (_input.jump && _input.move != Vector2.zero)
                {
                     

                    bool foundWall = Physics.CheckSphere(_wallJumpSpherePos, _wallJumpSphereRad, GroundLayers,
                QueryTriggerInteraction.Ignore);

                    if (foundWall)
                    {
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    }

                    Debug.Log("Try walljumping");
                }



                // reset the jump timeout timer
                    _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Color transparentBlue = new Color(0.0f, 0.0f, 1.0f, 0.5f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);


            //Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            //float yDir = Mathf.Atan2(-inputDirection.x, -inputDirection.z) * Mathf.Rad2Deg +
            //              _mainCamera.transform.eulerAngles.y;
            //Vector3 worldDir = Quaternion.Euler(new Vector3(0f, yDir, 0f)) * new Vector3(0f, 0f, 1f);
            //float rad = _controller.radius * 1.2f;

            if (true || _input.move != Vector2.zero)
            {
                Gizmos.color = transparentBlue;
                Gizmos.DrawSphere(_wallJumpSpherePos, _wallJumpSphereRad);
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}