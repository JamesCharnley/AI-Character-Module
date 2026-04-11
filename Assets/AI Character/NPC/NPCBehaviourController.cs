using AICharacterModule.NPC.StateMachine.Data;
using AICharacterModule.NPC.StateMachine.Managers;
using AICharacterModule.NPC.StateMachine.States;
using AICharacterModule.NPC.StateMachine.SubMachines;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace AICharacterModule.NPC
{
    /// <summary>
    /// Example wiring for a hierarchical NPC state machine.
    /// Master machine chooses between navigation and combat sub-machines.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCBehaviourController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Combat Transition Thresholds")]
        [SerializeField] private float combatCircleApproachDelaySeconds = 5f;
        [SerializeField] private float combatCircleMaxDistanceChange = 2f;
        [SerializeField] private float combatCircleMovedCloserDistance = 5f;
        [SerializeField] private float combatCircleMinDistanceAfterMoveCloser = 8f;
        [SerializeField] private float chaseToCombatMinDistance = 10f;
        [SerializeField] private float chaseToCombatMaxDistance = 15f;
        [SerializeField] private float chaseToCombatMaxTargetSpeed = 0.05f;
        [SerializeField] private float approachToHandCombatDistance = 3f;
        [SerializeField] private float handCombatExitDistance = 4f;
        [SerializeField] private float combatToChaseDistanceIncrease = 2.5f;

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

        private void Update()
        {
            _masterStateMachine.GlobalData.NavAgent.speed = _masterStateMachine.GlobalData.Anim.GetFloat("Speed");
            _masterStateMachine.GlobalData.CurrentTarget = target;
            _masterStateMachine.Tick(Time.deltaTime);
            _masterStateMachine.GlobalData.Tick(Time.deltaTime);
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

        public void ChaseAnimationCycleEnding()
        {
            chaseAnimationCycleEndingEvent?.Invoke();
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

        private void OnAnimatorMove()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_masterStateMachine.GlobalData.Anim.velocity);
            _masterStateMachine.GlobalData.Anim.SetFloat("Speed", localVelocity.z);
            _masterStateMachine.GlobalData.NpcLastVelocity = _masterStateMachine.GlobalData.Anim.velocity;
        }
    }
}
