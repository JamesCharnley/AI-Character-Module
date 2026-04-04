using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class CombatCircleState : IState<CombatData, NPCGlobalData>
    {
        private float _orbitDirection = 1f;
        private bool IsIdle;
        private bool IsOrbiting = false;

        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.isStopped = false;
            _orbitDirection = localData.CircleClockwise ? -1f : 1f;
            IsIdle = true;
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
        }

        private void WhileIdle(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.ResetPath();
        }
    }
}
