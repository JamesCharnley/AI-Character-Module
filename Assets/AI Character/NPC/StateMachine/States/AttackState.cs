using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class AttackState : IState<CombatData, NPCGlobalData>
    {
        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            localData.CooldownTimer = 0f;
            globalData.IsAttacking = true;
            globalData.NavAgent.isStopped = true;
            globalData.NavAgent.ResetPath();
        }

        public void Tick(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }

            localData.CooldownTimer -= deltaTime;
            if (localData.CooldownTimer > 0f)
            {
                return;
            }

            Debug.Log($"NPC attacks target for {localData.DamagePerHit} damage.");
            localData.CooldownTimer = localData.CooldownSeconds;
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.isStopped = false;
        }
    }
}
