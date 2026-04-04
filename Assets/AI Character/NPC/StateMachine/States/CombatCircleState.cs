using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class CombatCircleState : IState<CombatData, NPCGlobalData>
    {
        private const float IdleDurationSeconds = 4f;
        private const float OrbitTargetRadius = 25f;
        private const float OrbitMinDistanceFromNpc = 15f;
        private const float OrbitMaxDistanceFromNpc = 30f;
        private const float OrbitStopDistanceThreshold = 5f;

        private float _idleTimer;
        private bool IsIdle;
        private bool IsOrbiting = false;

        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            Debug.Log($"{GetType().Name} Enter");
            globalData.NavAgent.isStopped = false;
            _idleTimer = IdleDurationSeconds;
            IsIdle = true;
            IsOrbiting = false;
            globalData.Anim.SetBool("IsOrbiting01", true);
            globalData.Anim.SetTrigger("Idle");
        }

        public void Tick(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
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
            _idleTimer -= deltaTime;

            if (_idleTimer > 0f)
            {
                return;
            }

            bool orbitClockwise = Random.value < 0.5f;
            localData.CircleClockwise = orbitClockwise;
            globalData.Anim.SetTrigger(orbitClockwise ? "OrbitClockwise" : "OrbitAntiClockwise");
            bool foundOrbitPosition = globalData.TryFindPositionOnTargetRadius(
                OrbitTargetRadius,
                globalData.CurrentTarget.position,
                OrbitMinDistanceFromNpc,
                OrbitMaxDistanceFromNpc,
                out Vector3 orbitDestination,
                orbitClockwise);
            if (!foundOrbitPosition)
            {
                _idleTimer = IdleDurationSeconds;
                IsIdle = true;
                IsOrbiting = false;
                return;
            }

            globalData.NavAgent.SetDestination(orbitDestination);

            _idleTimer = IdleDurationSeconds;
            IsIdle = false;
            IsOrbiting = true;
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            globalData.Anim.SetBool("IsOrbiting01", false);
            globalData.NavAgent.ResetPath();
        }
    }
}
