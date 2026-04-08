using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class ApproachCombatTargetState : IState<CombatData, NPCGlobalData>
    {
        private readonly MonoBehaviour _controllerMonoBehaviour;
        public bool IsLocked { get; private set; }

        public ApproachCombatTargetState(MonoBehaviour controllerMonoBehaviour)
        {
            _controllerMonoBehaviour = controllerMonoBehaviour;
        }

        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            Debug.Log($"{GetType().Name} Enter");

            if (globalData.CurrentTarget == null)
            {
                IsLocked = false;
                return;
            }

            globalData.NavAgent.isStopped = false;
            globalData.NavAgent.SetDestination(globalData.CurrentTarget.position);
            globalData.Anim.SetTrigger("CombatWalkApproach01");
            IsLocked = false;
        }

        public void Tick(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }

            globalData.NavAgent.SetDestination(globalData.CurrentTarget.position);
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
        }
    }
}
