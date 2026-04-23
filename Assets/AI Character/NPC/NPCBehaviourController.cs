using AICharacterModule.NPC.StateMachine.Data;
using AICharacterModule.NPC.StateMachine.Managers;
using AICharacterModule.NPC.StateMachine.States;
using AICharacterModule.NPC.StateMachine.SubMachines;
using System;
using System.Collections.Generic;
using AICharacterModule.NPC.StateMachine.Core;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AICharacterModule.NPC
{
    /// <summary>
    /// Example wiring for a hierarchical NPC state machine.
    /// Master machine chooses between navigation and combat sub-machines.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCBehaviourController : MonoBehaviour, ITakeDamage, IHasHitZones
    {
        [SerializeField] private Transform target;

        [Header("Combat Transition Thresholds")]
        [SerializeField] private float combatCircleApproachDelaySeconds = 5f;
        [SerializeField] private float combatCircleMaxDistanceChange = 2f;
        [SerializeField] private float combatCircleMovedCloserDistance = 5f;
        [SerializeField] private float combatCircleMinDistanceAfterMoveCloser = 8f;
        [SerializeField] private float chaseToCombatMinDistance = 15f;
        [SerializeField] private float chaseToCombatMaxDistance = 20f;
        [SerializeField] private float chaseToCombatMaxTargetSpeed = 0.05f;
        [SerializeField] private float approachToHandCombatDistance = 6f;
        [SerializeField] private float handCombatExitDistance = 8f;
        [SerializeField] private float combatToChaseDistanceIncrease = 2.5f;
        [SerializeField] private float damageOverlapRadius = 0.75f;
        [SerializeField] private float damageAmount = 20f;
        [SerializeField] private LayerMask damageOverlapLayers = ~0;
        [SerializeField] private Vector3 defaultDamageOverlapOffset = new(0f, 1f, 1f);
        [SerializeField] private bool showDamageOverlapDebugSphere = true;
        [SerializeField] private Color damageOverlapDebugColor = new(1f, 0.2f, 0.2f, 0.75f);

        public event Action chaseAnimationCycleEndingEvent;

        private StateMachineManager<NPCGlobalData> _masterStateMachine;
        private StateManager<NavigationData, NPCGlobalData> _navigationStateManager;
        private StateManager<CombatData, NPCGlobalData> _combatStateManager;

        public bool IsNavigationStateLocked => _navigationStateManager?.IsCurrentStateLocked ?? false;
        public bool IsCombatStateLocked => _combatStateManager?.IsCurrentStateLocked ?? false;

        private void Awake()
        {
            var navAgent = GetComponent<NavMeshAgent>();

            var globalData = new NPCGlobalData
            {
                BehaviourController = this,
                NpcTransform = transform,
                NavAgent = navAgent,
                CurrentTarget = target,
                Anim = GetComponent<Animator>(),
                DetectionRange = navAgent.stoppingDistance + 60f,
                AttackRange = navAgent.stoppingDistance
            };

            _masterStateMachine = new StateMachineManager<NPCGlobalData>(globalData);

            // Navigation State machine
            _navigationStateManager = new StateManager<NavigationData, NPCGlobalData>(new NavigationData(globalData), _masterStateMachine);
            _navigationStateManager.RegisterState("Patrol", new PatrolState(this));
            _navigationStateManager.RegisterState("Chase", new ChaseState(this));
            _navigationStateManager.RegisterTransition(
                "Patrol",
                "Chase",
                (_, data) => HasTargetWithinRange(data, data.DetectionRange));
            
            
            var navigationSubMachine = new SubStateMachine<NavigationData, NPCGlobalData>("Navigation", "Chase", _navigationStateManager);
            
            
            // Combat state machine
            _combatStateManager = new StateManager<CombatData, NPCGlobalData>(new CombatData(), _masterStateMachine);
            _combatStateManager.RegisterState("CombatCircle", new CombatCircleState(this));
            _combatStateManager.RegisterState("ApproachCombatTarget", new ApproachCombatTargetState(this));
            _combatStateManager.RegisterState("HandCombat", new HandCombatState(this));
            _combatStateManager.RegisterTransition(
                "CombatCircle",
                "ApproachCombatTarget",
                ShouldApproachTargetAfterCirclingForDuration);
            _combatStateManager.RegisterTransition(
                "CombatCircle",
                "ApproachCombatTarget",
                ShouldApproachTargetWhenItMovesCloser);
            _combatStateManager.RegisterTransition(
                "ApproachCombatTarget",
                "HandCombat",
                ShouldEnterHandCombatFromApproach);
            _combatStateManager.RegisterTransition(
                "HandCombat",
                "ApproachCombatTarget",
                ShouldReturnToApproachFromHandCombat);

            var combatSubMachine = new SubStateMachine<CombatData, NPCGlobalData>("Combat", "CombatCircle", _combatStateManager);
            
            // Master state machine
            _masterStateMachine.RegisterSubMachine(navigationSubMachine);
            _masterStateMachine.RegisterSubMachine(combatSubMachine);

            _masterStateMachine.RegisterTransition(
                "Navigation",
                "Combat",
                data => _navigationStateManager.CurrentStateName == "Chase" && ShouldEnterCombatCircleFromChase(data));
            _masterStateMachine.RegisterTransition(
                "Combat",
                "Navigation",
                ShouldReturnToChaseWhenTargetMovesAwayFromCombatCircle);

            _masterStateMachine.SwitchTo("Navigation");
            
        }

        private void Start()
        {
            HitZones = new();
            foreach (HitZoneInfo zoneInfo in HitZonesCache)
            {
                HitZones.Add(zoneInfo);
            }
        }

        private void Update()
        {
            _masterStateMachine.GlobalData.NavAgent.speed = _masterStateMachine.GlobalData.Anim.GetFloat("Speed");
            _masterStateMachine.GlobalData.CurrentTarget = target;
            _masterStateMachine.Tick(Time.deltaTime);
            _masterStateMachine.GlobalData.Tick(Time.deltaTime);
            DoDamageCheck();
        }


        public void AttackAnimationCompleted()
        {
            if (_masterStateMachine == null)
            {
                Debug.Log("AttackAnimationCompleted failed");
                return;
            }
            Debug.Log("AttackAnimationCompleted");
            _masterStateMachine.GlobalData.IsAttacking = false;
        }

        public void DodgeAnimationCompleted()
        {
            _masterStateMachine.GlobalData.IsDodging = false;
        }

        public void ChaseAnimationCycleEnding()
        {
            chaseAnimationCycleEndingEvent?.Invoke();
        }


        [SerializeField] private Transform RightHandDamageBone;
        [SerializeField] private Transform LeftHandDamageBone;
        [SerializeField] private Transform LeftFootDamageBone;
        [SerializeField] private Transform RightFootDamageBone;
        
        private bool LeftHandDamageActive = false;
        private bool RightHandDamageActive = false;
        private bool LeftFootDamageActive = false;
        private bool RightFootDamageActive = false;

        void DoDamageCheck()
        {
            if (LeftHandDamageActive)
            {
                DoDamageOverlap(LeftHandDamageBone);
            }
            if (RightHandDamageActive)
            {
                DoDamageOverlap(RightHandDamageBone);
            }
            if (LeftFootDamageActive)
            {
                DoDamageOverlap(LeftFootDamageBone);
            }
            if (RightHandDamageActive)
            {
                DoDamageOverlap(RightFootDamageBone);
            }
        }
        public void DoDamageOverlapActivate(string _damagePointId)
        {
            switch (_damagePointId)
            {
                case "LeftHand":
                    LeftHandDamageActive = true;
                    break;
                case "RightHand":
                    RightHandDamageActive = true;
                    break;
                case "LeftFoot":
                    LeftFootDamageActive = true;
                    break;
                case "RightFoot":
                    RightFootDamageActive = true;
                    break;
                default:
                    RightHandDamageActive = true;
                    break;
            }
        }
        public void DoDamageOverlapDeactivate(string _damagePointId)
        {
            switch (_damagePointId)
            {
                case "LeftHand":
                    LeftHandDamageActive = false;
                    break;
                case "RightHand":
                    RightHandDamageActive = false;
                    break;
                case "LeftFoot":
                    LeftFootDamageActive = false;
                    break;
                case "RightFoot":
                    RightFootDamageActive = false;
                    break;
                default:
                    RightHandDamageActive = false;
                    break;
            }
        }

        public void DoDamageOverlap(Transform _originTransform)
        {
            
            Collider[] hitColliders = Physics.OverlapSphere(_originTransform.position, damageOverlapRadius, damageOverlapLayers);
            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.transform == transform)
                {
                    continue;
                }

                ITakeDamage takeDamage = hitCollider.GetComponentInParent<ITakeDamage>();
                if (takeDamage == null)
                {
                    continue;
                }

                takeDamage.TakeDamage(damageAmount, transform.forward, Vector3.zero);
            }
        }

        private Vector3 GetDamageOverlapCenter(Vector3 localOffset)
        {
            return transform.TransformPoint(localOffset);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDamageOverlapDebugSphere)
            {
                return;
            }
            if (LeftHandDamageActive)
            {
                Gizmos.color = damageOverlapDebugColor;
                Gizmos.DrawWireSphere(LeftHandDamageBone.position, damageOverlapRadius);
            }
            if (RightHandDamageActive)
            {
                Gizmos.color = damageOverlapDebugColor;
                Gizmos.DrawWireSphere(RightHandDamageBone.position, damageOverlapRadius);
            }
            if (LeftFootDamageActive)
            {
                Gizmos.color = damageOverlapDebugColor;
                Gizmos.DrawWireSphere(LeftFootDamageBone.position, damageOverlapRadius);
            }
            if (RightHandDamageActive)
            {
                Gizmos.color = damageOverlapDebugColor;
                Gizmos.DrawWireSphere(RightFootDamageBone.position, damageOverlapRadius);
            }

            
        }

        private static bool HasTargetWithinRange(NPCGlobalData data, float range)
        {
            if (data.CurrentTarget == null)
            {
                return false;
            }

            return Vector3.Distance(data.NpcTransform.position, data.CurrentTarget.position) <= range;
        }




        private bool ShouldApproachTargetAfterCirclingForDuration(CombatData localData, NPCGlobalData globalData)
        {
            if (globalData.CurrentTarget == null)
            {
                return false;
            }

            if (localData.CombatCircleElapsedSeconds < combatCircleApproachDelaySeconds)
            {
                return false;
            }

            float currentDistance = Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position);
            float distanceChange = Mathf.Abs(currentDistance - localData.CombatCircleEntryDistanceToTarget);
            if (distanceChange <= combatCircleMaxDistanceChange) Debug.Log("ShouldApproachTargetAfterCirclingForDuration");
            return distanceChange <= combatCircleMaxDistanceChange;
        }

        private bool ShouldApproachTargetWhenItMovesCloser(CombatData localData, NPCGlobalData globalData)
        {
            if (globalData.CurrentTarget == null)
            {
                return false;
            }

            float currentDistance = Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position - Vector3.up);
            float movedCloserDistance = localData.CombatCircleEntryDistanceToTarget - currentDistance;
            if (movedCloserDistance >= combatCircleMovedCloserDistance && currentDistance > combatCircleMinDistanceAfterMoveCloser) Debug.Log("ShouldApproachTargetWhenItMovesCloser");
            return movedCloserDistance >= combatCircleMovedCloserDistance && currentDistance > combatCircleMinDistanceAfterMoveCloser;
            
        }

        private bool ShouldEnterCombatCircleFromChase(NPCGlobalData data)
        {
            if (data.CurrentTarget == null)
            {
                return false;
            }

            float distance = Vector3.Distance(data.NpcTransform.position, data.CurrentTarget.position - Vector3.up);
            float targetSpeed = data.GetTargetVelocity().magnitude;

            return distance >= chaseToCombatMinDistance && distance <= chaseToCombatMaxDistance && targetSpeed < chaseToCombatMaxTargetSpeed;
        }

        private bool ShouldEnterHandCombatFromApproach(CombatData localData, NPCGlobalData globalData)
        {
            if (globalData.CurrentTarget == null)
            {
                return false;
            }

            float distance = Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position);
            return distance <= approachToHandCombatDistance;
        }

        private bool ShouldReturnToApproachFromHandCombat(CombatData localData, NPCGlobalData globalData)
        {
            if (globalData.CurrentTarget == null)
            {
                return false;
            }

            float distance = Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position);
            return distance > handCombatExitDistance;
        }

        private bool ShouldReturnToChaseWhenTargetMovesAwayFromCombatCircle(NPCGlobalData data)
        {
            if (data.CurrentTarget == null)
            {
                return false;
            }

            float currentDistance = Vector3.Distance(data.NpcTransform.position, data.CurrentTarget.position - Vector3.up);
            float distanceIncreaseSinceCombatCircleEnter =
                currentDistance - data.CombatCircleEntryDistanceToTarget;

            return distanceIncreaseSinceCombatCircleEnter >= combatToChaseDistanceIncrease;
            
        }
        
        // ANIMATOR CODE
        
        [Header("IK Targets")]
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandTarget;

        

        private static readonly int RightHandIKWeightHash = Animator.StringToHash("RightHandIKWeight");
        private static readonly int LeftHandIKWeightHash = Animator.StringToHash("LeftHandIKWeight");

        private void OnAnimatorMove()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_masterStateMachine.GlobalData.Anim.velocity);
            _masterStateMachine.GlobalData.Anim.SetFloat("Speed", Mathf.Abs(localVelocity.z));
            _masterStateMachine.GlobalData.NpcLastVelocity = _masterStateMachine.GlobalData.Anim.velocity;
        }
        private void OnAnimatorIK(int layerIndex)
        {
            Animator animator = _masterStateMachine.GlobalData.Anim;
            if (animator == null)
                return;

            float rightWeight = animator.GetFloat(RightHandIKWeightHash);
            float leftWeight = animator.GetFloat(LeftHandIKWeightHash);

            ApplyHandIK(AvatarIKGoal.RightHand, rightHandTarget, rightWeight);
            ApplyHandIK(AvatarIKGoal.LeftHand, leftHandTarget, leftWeight);
        }

        private void ApplyHandIK(AvatarIKGoal handGoal, Transform target, float weight)
        {
            Animator animator = _masterStateMachine.GlobalData.Anim;
            
            animator.SetIKPositionWeight(handGoal, weight);
            //animator.SetIKRotationWeight(handGoal, weight);

            if (target == null || weight <= 0f)
                return;

            animator.SetIKPosition(handGoal, target.position);
            //animator.SetIKRotation(handGoal, target.rotation);
        }

        public void IncomingAttack(Vector3 _offset)
        {
            Animator anim = _masterStateMachine.GlobalData.Anim;
            if (_masterStateMachine.GlobalData.IsAttacking || _masterStateMachine.GlobalData.IsDodging)
            {
                return;
            }

            _masterStateMachine.GlobalData.IsDodging = true;
            
            if (_offset.y == 1)
            {
                // duck
                anim.SetTrigger("DodgePunchDown");
                return;
            }
            if (_offset.y == -1)
            {
                // back
                anim.SetTrigger("DodgePunchBack");
                return;
            }
            if (_offset.z == -1)
            {
                // back
                anim.SetTrigger("DodgePunchBack");
                return;
            }
            if (_offset.x == 1)
            {
                // left
                anim.SetTrigger("DodgePunchLeft");
                return;
            }
            if (_offset.x == -1)
            {
                // right
                anim.SetTrigger("DodgePunchRight");
                return;
            }
        }

        [SerializeField] private float HorizontalCenterWidth = 0.4f;
        public Vector3 GetRelativePositionAxes(Transform referenceTransform, Vector3 worldPosition)
        {
            
            float x = 0, y = 0, z = 0;
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            
            if (localPosition.x > HorizontalCenterWidth / 2)
            {
                x = 1;
            }
            else if (localPosition.x < -HorizontalCenterWidth / 2)
            {
                x = -1;
            }
            y = localPosition.y > 1.3f ? 1 : -1;
            z = localPosition.z < 0 ? -1 : 1;
            return new Vector3(x, y, z);
        }

        public void TakeDamage(float _amount)
        {
            throw new NotImplementedException();
        }

        [SerializeField] private Transform TorsoeBone;
        public void TakeDamage(float _amount, Vector3 _direction, Vector3 _damagerPos)
        {
            Vector3 offset = GetRelativePositionAxes(TorsoeBone, _damagerPos);
            Animator anim = _masterStateMachine.GlobalData.Anim;
            //Debug.LogError($"TakeDamage: {offset}");

            // Expected axis mapping:
            // X: +1 left, -1 right
            // Y: +1 above, -1 below
            // Z: +1 front, -1 back

            if (offset.z == -1f)
            {
                if (offset.y == 1f)
                {
                    anim.SetTrigger("TorsoeDamageHigh_Back");
                    return;
                }

                if (offset.y == -1f)
                {
                    anim.SetTrigger("TorsoeDamageLow_Back");
                    return;
                }
            }

            if (offset == new Vector3(0, 1, 1))
            {
                // high front
                anim.SetTrigger("TorsoeDamageHigh_Front");
                return;
            }
            if (offset == new Vector3(1, 1, 1))
            {
                // High left
                anim.SetTrigger("TorsoeDamageHigh_Left");
                return;
            }
            if (offset == new Vector3(-1, 1, 1))
            {
                // High right
                anim.SetTrigger("TorsoeDamageHigh_Right");
                return;
            }
            if (offset == new Vector3(1, -1, 1))
            {
                // Low left
                anim.SetTrigger("TorsoeDamageLow_Left");
                return;
            }
            if (offset == new Vector3(-1, -1, 1))
            {
                // Low right
                anim.SetTrigger("TorsoeDamageLow_Right");
                return;
            }
            if (offset == new Vector3(0, -1, 1))
            {
                // Low front
                anim.SetTrigger("TorsoeDamageLow_Front");
                return;
            }
        }

        [SerializeField] private AudioClip[] HighBodyHitSounds;
        [SerializeField] private AudioClip[] LowBodyHitSounds;
        [SerializeField] private AudioClip[] FaceHitSounds;

        public void TakeDamage(float _amount, Vector3 _direction, HitZoneInfo _hitZoneInfo)
        {
            Vector3 offset = _hitZoneInfo.LocalOffset;
            Animator anim = _masterStateMachine.GlobalData.Anim;
            Debug.LogError($"TakeDamage: {offset}");

            // Expected axis mapping:
            // X: +1 left, -1 right
            // Y: +1 above, -1 below
            // Z: +1 front, -1 back

            if (offset.z == -1f)
            {
                if (offset.y == 1f)
                {
                    anim.SetTrigger("TorsoeDamageHigh_Back");
                    return;
                }

                if (offset.y == -1f)
                {
                    anim.SetTrigger("TorsoeDamageLow_Back");
                    return;
                }
            }

            if (offset == new Vector3(0, 1, 1))
            {
                // high front
                anim.SetTrigger("TorsoeDamageHigh_Front");
                PlayHighBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(1, 1, 1))
            {
                // High left
                anim.SetTrigger("TorsoeDamageHigh_Left");
                PlayHighBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(-1, 1, 1))
            {
                // High right
                anim.SetTrigger("TorsoeDamageHigh_Right");
                PlayHighBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(1, -1, 1))
            {
                // Low left
                anim.SetTrigger("TorsoeDamageLow_Left");
                PlayLowBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(-1, -1, 1))
            {
                // Low right
                anim.SetTrigger("TorsoeDamageLow_Right");
                PlayLowBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(0, -1, 1))
            {
                // Low front
                anim.SetTrigger("TorsoeDamageLow_Front");
                PlayLowBodyHit(_hitZoneInfo);
                return;
            }
        }

        void PlayLowBodyHit(HitZoneInfo _hitZone)
        {
            if (!_hitZone.SelfTransform.TryGetComponent(out AudioSource source))
            {
                return;
            }
            int rand = Random.Range(0, LowBodyHitSounds.Length);
            source.clip = LowBodyHitSounds[rand];
            source.Play();

        }
        void PlayHighBodyHit(HitZoneInfo _hitZone)
        {
            if (!_hitZone.SelfTransform.TryGetComponent(out AudioSource source))
            {
                return;
            }
            int rand = Random.Range(0, HighBodyHitSounds.Length);
            source.clip = HighBodyHitSounds[rand];
            source.Play();
        }
        
        [SerializeField] private HitZoneInfo[] HitZonesCache;
        public List<HitZoneInfo> HitZones { get; set; }
        public HitZoneInfo GetClosestHitzoneTransform(Vector3 _RelativeTo)
        {
            HitZoneInfo closestHitZone = new();
            float shortestDist = 999;
            foreach (HitZoneInfo hitZone in HitZones)
            {
                float dist = Vector3.Distance(hitZone.SelfTransform.position, _RelativeTo);
                if (dist < shortestDist)
                {
                    shortestDist = dist;
                    closestHitZone = hitZone;
                }
            }

            return closestHitZone;
        }

        [SerializeField] private AudioClip[] HeavyPunchWooshSounds;
        [SerializeField] private AudioClip[] LightPunchWooshSounds;
        [SerializeField] private AudioClip[] HeavyKickWooshSounds;
        [SerializeField] private AudioClip[] LightKickWooshSounds;

        public void PlayHeavyPunchWoosh(string _isLeftHand)
        {
            int maxEx = HeavyPunchWooshSounds.Length;
            AudioClip[] clips = HeavyPunchWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftHandDamageBone.GetComponent<AudioSource>()
                : RightHandDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();

        }
        public void PlayLightPunchWoosh(string _isLeftHand)
        {
            int maxEx = LightPunchWooshSounds.Length;
            AudioClip[] clips = LightPunchWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftHandDamageBone.GetComponent<AudioSource>()
                : RightHandDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();

        }

        public void PlayHeavyKickWoosh(string _isLeftHand)
        {
            int maxEx = HeavyKickWooshSounds.Length;
            AudioClip[] clips = HeavyKickWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftFootDamageBone.GetComponent<AudioSource>()
                : RightFootDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();
        }
        public void PlayLightKickWoosh(string _isLeftHand)
        {
            int maxEx = HeavyKickWooshSounds.Length;
            AudioClip[] clips = HeavyKickWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftFootDamageBone.GetComponent<AudioSource>()
                : RightFootDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();
        }
    }
    
}
