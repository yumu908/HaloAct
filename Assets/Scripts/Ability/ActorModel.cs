using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Ability
{
    public enum ActorType
    {
        PLAYER,
        Enemy,
    }

    public class ActorModel : MonoBehaviour
    {
        [FolderPath] public string NodePath;
        [FolderPath] public string BehaviorPath;
        [HideInInspector] public AbilityBehaviorTree tree;
        [HideInInspector] public HitBox HitBox;
        [HideInInspector] public HurtBox HurtBox;
        public ActorType ActorType;
        public ActorModel Target;

        /// <summary>
        /// 缓存时间，用于计算帧数
        /// </summary>
        private float cacheTime;
        /// <summary>
        /// 当前运行的帧数
        /// </summary>
        private int curFrame;
        private float fps;

        public bool IsDead;
        public bool IsInvincible;
        public bool IsGround;
        public bool IsAerial;
        float cacheAerialTime;
        float delayAerialTime = 0.5f;

        [HideInInspector] public PlayerGameInput GameInput;
        CharacterController characterController;
        public float Gravity = -9.8f;
        public Vector3 Frictional;
        public Vector2 InputDir;
        public Vector3 EulerAngles;
        public Quaternion Rotation;
        public Vector3 Velocity;
        [HideInInspector] public GroundChecker groundChecker;

        private void Awake()
        {
            fps = 1.0f / GameManager_Settings.TargetFraneRate;
            curFrame = 1;
        }

        void Start()
        {
            tree = new AbilityBehaviorTree();
            tree.Init(NodePath, BehaviorPath);
            tree.Enter(this);

            HitBox = GetComponentInChildren<HitBox>();
            HitBox.Init();
            HitBox.Exit(); // 默认隐藏

            HurtBox = GetComponentInChildren<HurtBox>();
            HurtBox.Init();
            HurtBox.Enter(this);

            characterController = GetComponent<CharacterController>();
            GameInput = GetComponent<PlayerGameInput>();
            groundChecker = GetComponent<GroundChecker>();
        }

        void Update()
        {
            cacheTime += Time.deltaTime;

            // 超过fps执行一次Tick
            while (cacheTime > fps)
            {
                tree.Tick(curFrame);
                curFrame += 1;
                cacheTime -= fps;
            }

            UpdatePhysics();
        }

        private void UpdatePhysics()
        {
            UpdateVelocity();
            CheckGround();
        }

        private void CheckGround()
        {
            IsGround = groundChecker.CheckGround();
            if (IsGround)
            {
                Velocity.y = 0;
                IsAerial = false;
                cacheAerialTime = 0;
            }
            else
            {
                // 延迟一段时间后才算空中
                if (!IsAerial)
                {
                    cacheAerialTime += Time.deltaTime;
                }

                if (cacheAerialTime > delayAerialTime)
                {
                    IsAerial = true;
                }
            }
        }

        private void UpdateVelocity()
        {
            characterController.Move(Velocity * Time.deltaTime);
            characterController.transform.rotation = Rotation;
            Velocity.y += Gravity * Time.deltaTime;
            Velocity.y = Mathf.Clamp(Velocity.y, -20, 20);

            // 用来处理速度的衰减，速度不断变小并无限接近0
            Velocity.Scale(Frictional);
        }

        internal void DeathCheck()
        {

        }

        private void OnDrawGizmos()
        {
            if (Target != null && ActorType == ActorType.Enemy)
            {
                Gizmos.DrawLine(transform.position, Target.transform.position);
                // Target.transform.LookAt(dir);

                // var dir = Target.transform.position - transform.position;
                // dir.y = 0;
                // float angle = Vector3.Angle(transform.forward, dir);
                // EulerAngles = angle * Vector3.up;
            }
        }
    }
}