using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class ChaseState : IState<NavigationData, NPCGlobalData>
    {
        private const float AttackEstimateMinSeconds = 2.5f;
        private const float AttackEstimateMaxSeconds = 3.5f;

        public void Enter(NavigationData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.isStopped = false;
            localData.ResetArrivalEstimateTracking();
        }

        public void Tick(NavigationData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }

            globalData.NavAgent.SetDestination(globalData.CurrentTarget.position);
            localData.UpdateRemainingDistanceHistory(globalData.NavAgent.remainingDistance, deltaTime);

            if (globalData.IsAttacking || globalData.Anim == null)
            {
                return;
            }

            float estimatedSecondsToDestination = localData.GetEstimatedSecondsToDestination();
            if (float.IsInfinity(estimatedSecondsToDestination) || float.IsNaN(estimatedSecondsToDestination))
            {
                return;
            }

            if (estimatedSecondsToDestination >= AttackEstimateMinSeconds && estimatedSecondsToDestination <= AttackEstimateMaxSeconds)
            {
                globalData.Anim.SetTrigger("Attack");
                globalData.IsAttacking = true;
            }
        }

        public void Exit(NavigationData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.ResetPath();
        }
    }
}
