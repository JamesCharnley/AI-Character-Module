using System.Collections;
using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class CombatCircleState : IState<CombatData, NPCGlobalData>
    {
        private readonly MonoBehaviour _controllerMonoBehaviour;
        public bool IsLocked { get; private set; }
        private const float IdleDurationSeconds = 8f;
        private const float OrbitMinDistanceFromNpc = 8f;
        private const float OrbitMaxDistanceFromNpc = 15f;
        private const float OrbitStopDistanceThreshold = 3.75f;

        private float _idleTimer;
        private float _orbitTargetRadius;
        private bool IsIdle;
        private bool IsOrbiting = false;
        private bool WaitingForZeroSpeed = false;

        public CombatCircleState(MonoBehaviour controllerMonoBehaviour)
        {
            _controllerMonoBehaviour = controllerMonoBehaviour;
        }

        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            Debug.Log($"{GetType().Name} Enter");
            
            _idleTimer = IdleDurationSeconds;
            localData.CombatCircleElapsedSeconds = 0f;
            globalData.Anim.SetBool("IsOrbiting01", true);
            globalData.Anim.SetTrigger("Idle");
            IsIdle = true;
            IsOrbiting = false;

            if (globalData.NavAgent.speed != 0)
            {
                WaitingForZeroSpeed = true;
                _controllerMonoBehaviour.StartCoroutine(WaitForZeroSpeed(globalData, localData));
            }
            else
            {
                Setup(localData, globalData);
            }
            IsLocked = false;
        }

        private void Setup(CombatData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.isStopped = false;
            localData.CombatCircleEntryDistanceToTarget = globalData.CurrentTarget == null
                ? 0f
                : Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position);
            globalData.CombatCircleEntryDistanceToTarget = localData.CombatCircleEntryDistanceToTarget;
            _orbitTargetRadius = globalData.CurrentTarget == null
                ? 0f
                : Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position);
            Debug.Log(_orbitTargetRadius);
        }

        public void Tick(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (WaitingForZeroSpeed) return;
            localData.CombatCircleElapsedSeconds += deltaTime;

            if (globalData.CurrentTarget == null)
            {
                return;
            }

            if (IsIdle)
            {
                WhileIdle(localData, globalData, deltaTime);
            }

            if (IsOrbiting)
            {
                WhileOrbiting(localData, globalData, deltaTime);
            }
        }

        private void WhileOrbiting(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (!globalData.NavAgent.hasPath)
            {
                return;
            }

            if (globalData.NavAgent.remainingDistance >= OrbitStopDistanceThreshold)
            {
                return;
            }

            _idleTimer = IdleDurationSeconds;
            IsOrbiting = false;
            IsIdle = true;
            globalData.Anim.SetTrigger("Idle");
        }

        private void WhileIdle(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.NavAgent.speed == 0)
            {
                if (globalData.NavAgent.hasPath)
                {
                    globalData.NavAgent.ResetPath();
                    globalData.NavAgent.isStopped = true;
                }
            }
            _idleTimer -= deltaTime;

            if (!isFacingPlayer)
            {
                TurnToTarget(globalData);
            }

            if (_idleTimer > 0f)
            {
                return;
            }

            bool orbitClockwise = Random.value < 0.5f;
            localData.CircleClockwise = orbitClockwise;
            globalData.Anim.SetTrigger(orbitClockwise ? "OrbitClockwise" : "OrbitAntiClockwise");
            bool foundOrbitPosition = globalData.TryFindPositionOnTargetRadius(
                _orbitTargetRadius,
                globalData.CurrentTarget.position - Vector3.up,
                OrbitMinDistanceFromNpc,
                OrbitMaxDistanceFromNpc,
                out Vector3 orbitDestination,
                orbitClockwise);
            Debug.Log($"{GetType().Name} TryFindPositionOnTargetRadius status: {foundOrbitPosition}");
            if (!foundOrbitPosition)
            {
                _idleTimer = IdleDurationSeconds;
                IsIdle = true;
                IsOrbiting = false;
                isFacingPlayer = false;
                return;
            }

            globalData.NavAgent.SetDestination(orbitDestination);

            _idleTimer = IdleDurationSeconds;
            IsIdle = false;
            IsOrbiting = true;
            isFacingPlayer = false;
            globalData.NavAgent.isStopped = false;
        }

        private bool isFacingPlayer = false;
        void TurnToTarget(NPCGlobalData _globalData)
        {
            if (!isFacingPlayer)
            {
                Vector3 toPlayer = (_globalData.CurrentTarget.position - Vector3.up) -
                                   _globalData.NpcTransform.position;

// Ignore vertical difference if needed
                toPlayer.y = 0;

                float dot = Vector3.Dot(_globalData.NpcTransform.right, toPlayer);

                if (dot > 0.1f)
                {
                    // Player is to the RIGHT of NPC
                    _globalData.Anim.SetBool("IsTurningRight", true);
                }
                else if (dot < -0.1f)
                {
                    // Player is to the LEFT of NPC
                    _globalData.Anim.SetBool("IsTurningLeft", true);
                }
                else
                {
                    _globalData.Anim.SetBool("IsTurningLeft", false);
                    _globalData.Anim.SetBool("IsTurningRight", false);

                    isFacingPlayer = true;
                }

                if (toPlayer.sqrMagnitude > 0.001f && !isFacingPlayer)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(toPlayer);

                    _globalData.NpcTransform.rotation = Quaternion.RotateTowards(
                        _globalData.NpcTransform.rotation,
                        targetRotation,
                        120 * Time.deltaTime
                    );
                }
            }
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            globalData.Anim.SetBool("IsOrbiting01", false);
            globalData.NavAgent.ResetPath();
        }

        IEnumerator WaitForZeroSpeed(NPCGlobalData _globalData, CombatData _localData)
        {
            yield return new WaitUntil(() => _globalData.NavAgent.speed == 0);
            Setup(_localData, _globalData);
            WaitingForZeroSpeed = false;
        }
    }
}
