using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class HandCombatState : IState<CombatData, NPCGlobalData>
    {
        private readonly MonoBehaviour _controllerMonoBehaviour;
        public bool IsLocked { get; private set; }

        public HandCombatState(MonoBehaviour controllerMonoBehaviour)
        {
            _controllerMonoBehaviour = controllerMonoBehaviour;
        }

        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            Debug.Log($"{GetType().Name} Enter");

            globalData.NavAgent.isStopped = true;
            IsLocked = false;
        }

        public void Tick(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }

            Vector3 directionToTarget = globalData.CurrentTarget.position - globalData.NpcTransform.position;
            directionToTarget.y = 0f;
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                globalData.NpcTransform.rotation = Quaternion.RotateTowards(globalData.NpcTransform.rotation, targetRotation, 720f * deltaTime);
            }
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            globalData.NavAgent.isStopped = false;
            IsLocked = false;
        }
    }
}
