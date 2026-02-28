using Cinemachine;
using InputSystem;
using Manager;
using Network;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;
using Yy.Protocol.App;

// using Yy.Protocol.App;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace Character
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class SelfPlayerController : MonoBehaviour
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

        [Header("Player ani_Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

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

        public bool isSelf = false;

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

        private bool  _animFlagIsJump;
        //private bool Grounded;
        private bool  _animFlagIsFreeFall;
        private float _animFlagSpeed;
        private float _animFlagMotionSpeed;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

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
            if (!_mainCamera)
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
            
            //AssignAnimationIDs
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            // 获取更新前的Animator状态标志
            GetAnimatorFlags();

            //! 玩家移动，更新Animator标志
            JumpAndGravity();
            Move();

            //! 获取更新后的Animator状态标志
            GetAnimatorFlags();
        }
        
        private void LateUpdate()
        {
            CameraRotation();
        }

        public (PlayerMove, PlayerJumpAndGravity) PackNetSync()
        {
            (Vector3_net pos, Quaternion_net rot) = Utils.Struct.PackNetTransform(gameObject.transform);
            var transformNet = new Transform_net{Position = pos, Rotation = rot };
            PlayerMove move = new()
            {
                Transform = transformNet,
                AniSpeed       = _animFlagSpeed,
                AniMotionSpeed = _animFlagMotionSpeed,
            };
            
            PlayerJumpAndGravity jump = new()
            {
                AniIsJump     = _animFlagIsJump,
                AniIsGround   = Grounded,
                AniIsFreefall = _animFlagIsFreeFall,
            };
            
            return (move, jump);
        }
        
        private void GetAnimatorFlags()
        {
            _animFlagIsJump = _animator.GetBool(_animIDJump);
            Grounded = _animator.GetBool(_animIDGrounded);
            _animFlagIsFreeFall = _animator.GetBool(_animIDFreeFall);
            _animFlagSpeed = _animator.GetFloat(_animIDSpeed);
            _animFlagMotionSpeed = _animator.GetFloat(_animIDMotionSpeed);
        }
        
        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;   // 移动鼠标左右看
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier; // 移动鼠标上下看
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private bool GroundedCheck()
        {
            // set sphere position, with offset
            var pos = transform.position;
            var spherePosition = new Vector3(pos.x, pos.y - GroundedOffset, pos.z);
            bool newGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (newGrounded != Grounded)
            {
                Grounded = newGrounded;
                _animator.SetBool(_animIDGrounded, Grounded);
                return true;
            }

            return false;
        }

        private void Move()
        {
            // 若冲刺则按冲刺速度算
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // Vector2的==运算符使用近似比较，所以不容易出现浮点错误。如果没有输入，则比magnitude开销更少，将目标速度设置为0
            if (_input.move == Vector2.zero)
                targetSpeed = 0.0f;

            // 玩家当前水平速度
            var velocity = _controller.velocity;
            float currentHorizontalSpeed = new Vector3(velocity.x, 0.0f, velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 需要加速或减速到目标速度
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // 平滑地加速/减速
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // 将速度舍入到小数点后3位
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // Mathf.Lerp(a,b,t)：在a和b之间进行线性内插，t∈[0,1]表示与a和b之间的接近程度
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f)
                _animationBlend = 0f;

            // 取得输入方向（单位向量）
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 如果有玩家的移动输入
            if (_input.move != Vector2.zero)
            {
                // 计算目标旋转：目标旋转是当前相机的水平角度
                // 玩家的左右旋转其实就是水平面旋转，改变y旋转角
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;

                // 使得玩家的左右旋转平滑地转至目标值(在RotationSmoothTime时间内从transform.eulerAngles.y到_targetRotation)
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // 旋转到相对于相机位置的面输入方向
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // 移动玩家
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            var move_target = targetDirection.normalized * (_speed * Time.deltaTime) +
                              new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;

            //! 移动
            _controller.Move(move_target);

            // 更新animator的状态
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }

        private bool JumpAndGravity()
        {
            bool isStateChanged = GroundedCheck();

            // 接地
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // 更新animator的状态：无跳跃，无自由落体
                if (_animFlagIsJump)
                {
                    _animator.SetBool(_animIDJump, false); // jump = false
                    isStateChanged = true;
                }
                if(_animFlagIsFreeFall)
                {
                    _animator.SetBool(_animIDFreeFall, false); // freefall = false
                    isStateChanged = true;
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // ani_Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // 自由落体速度： v²=2gh
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 更新animator的状态：有跳跃
                    if(_animFlagIsJump != true)
                    {
                        _animator.SetBool(_animIDJump, true); // jump = true
                        isStateChanged = true;
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f) {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f) {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else {
                    // 更新animator的状态：有自由落体
                    if(_animFlagIsFreeFall != true)
                    {
                        _animator.SetBool(_animIDFreeFall, true); // freefall = true
                        isStateChanged = true;
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

            return isStateChanged;
        }

        // 将角度限制为[-360,+360]之间
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (!(animationEvent.animatorClipInfo.weight > 0.5f)) return;
            if (FootstepAudioClips.Length <= 0) return;

            var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
            AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
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