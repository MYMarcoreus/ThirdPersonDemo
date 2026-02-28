using UnityEngine;
using Yy.Protocol.App;
// using Yy.Protocol.App;
// using Yy.Protocol.Core;

namespace Character
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]

    public class OtherPlayerController : MonoBehaviour
    {
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;


        [Header("Player ani_Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private CharacterController _controller;
        private Animator _animator;

        // 帧插值
        private Vector3 _position1;
        private Vector3 _position2;
        private Quaternion _rotation1;
        private Quaternion _rotation2;
        private float _speed1;
        private float _speed2;


        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();

            _position1 = gameObject.transform.position;
            _position2 = gameObject.transform.position;

            _rotation1 = gameObject.transform.rotation;
            _rotation2 = gameObject.transform.rotation;

            _speed1 = 0;
            _speed2 = 0;

            //AssignAnimationIDs
            _animIDSpeed       = Animator.StringToHash("Speed");
            _animIDGrounded    = Animator.StringToHash("Grounded");
            _animIDJump        = Animator.StringToHash("Jump");
            _animIDFreeFall    = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Update()
        {
            //GroundedCheck();
            UpdatePlayerMovement();
        }

        //private void GroundedCheck()
        //{
        //    Grounded = _animator.GetBool(_animIDGrounded);
        //}

        private void UpdatePlayerMovement()
        {
            // 当前状态
            _position1 = transform.position;
            _rotation1 = transform.rotation;

            // --- 位置插值 ---
            // float distance = Vector3.Distance(_position1, _position2);
            float posLerpFactor = Mathf.Clamp01(Time.deltaTime * 15f);
            Vector3 newPosition = Vector3.Lerp(_position1, _position2, posLerpFactor);
            Vector3 moveDelta = newPosition - _position1;

            // --- 旋转插值 ---
            // float angle = Quaternion.Angle(_rotation1, _rotation2);
            float rotLerpFactor = Mathf.Clamp01(Time.deltaTime * 15f);
            Quaternion newRotation = Quaternion.Slerp(_rotation1, _rotation2, rotLerpFactor);

            // --- 动画速度插值 ---
            _speed1 = Mathf.Lerp(_speed1, _speed2, 0.5f);

            // --- 应用所有变换 ---
            _controller?.Move(moveDelta);                        // 位置移动
            transform.rotation = newRotation;                    // 设置旋转
            _animator?.SetFloat(_animIDSpeed, _speed1);          // 设置动画速度
        }


        // 接收Proto结构，设置变量
        public void SetMove(PlayerMove move)
        {
            (_position2, _rotation2) = Utils.Struct.ParseNetTransform(move.Transform);
            _speed2       = move.AniSpeed;
            _animator?.SetFloat(_animIDMotionSpeed, move.AniMotionSpeed);
        }

        // 接收Proto结构，设置变量
        public void SetJumpAndGravity(PlayerJumpAndGravity jump)
        {
            // Debug.Log($"设置Animator标志：{jump.AniIsJump}, {jump.AniIsJump}, {jump.AniIsGround}");
            _animator?.SetBool(_animIDJump, jump.AniIsJump);
            _animator?.SetBool(_animIDFreeFall, jump.AniIsFreefall);
            _animator?.SetBool(_animIDGrounded, jump.AniIsGround);
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
            if (!(animationEvent.animatorClipInfo.weight > 0.5f)) return;

            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }


    }
}
