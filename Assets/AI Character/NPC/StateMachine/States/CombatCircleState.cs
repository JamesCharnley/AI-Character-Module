using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class CombatCircleState : IState<CombatData, NPCGlobalData>
    {
        private const float IdleDurationSeconds = 4f;
        private const float OrbitDurationSeconds = 10f;

        private float _orbitDirection = 1f;
        private float _idleTimer;
        private float _orbitTimer;
        private bool IsIdle;
        private bool IsOrbiting = false;

        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.isStopped = false;
            _orbitDirection = localData.CircleClockwise ? -1f : 1f;
            _idleTimer = IdleDurationSeconds;
            _orbitTimer = OrbitDurationSeconds;
            IsIdle = true;
            IsOrbiting = false;
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
            Vector3 targetPosition = globalData.CurrentTarget.position;
            Vector3 toNpc = (globalData.NpcTransform.position - targetPosition).normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, toNpc) * _orbitDirection;
            Vector3 orbitDestination = targetPosition + (toNpc * 25f) + (tangent * 6f);
            globalData.NavAgent.SetDestination(orbitDestination);

            _orbitTimer -= deltaTime;

            if (_orbitTimer > 0f)
            {
                return;
            }

            _orbitTimer = OrbitDurationSeconds;
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
            _orbitDirection = localData.CircleClockwise ? -1f : 1f;
            globalData.Anim.SetTrigger(orbitClockwise ? "OrbitClockwise" : "OrbitAntiClockwise");

            _idleTimer = IdleDurationSeconds;
            _orbitTimer = OrbitDurationSeconds;
            IsIdle = false;
            IsOrbiting = true;
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.ResetPath();
        }
    }
}
