using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class ChaseState : IState<NavigationData, NPCGlobalData>
    {
        private const float AttackEstimateMinSeconds = 2.5f;
        private const float AttackEstimateMaxSeconds = 3.5f;
        private NavigationData _localData;
        private NPCGlobalData _globalData;

        public void Enter(NavigationData localData, NPCGlobalData globalData)
        {
            _localData = localData;
            _globalData = globalData;

            globalData.NavAgent.isStopped = false;
            localData.ResetArrivalEstimateTracking();
            globalData.Anim.SetTrigger("Chase");
            SubscribeToChaseAnimationCycleEndingEvent(globalData);
        }

        public void Tick(NavigationData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }

            globalData.NavAgent.SetDestination(globalData.CurrentTarget.position);
            localData.UpdateRemainingDistanceHistory(globalData.NavAgent.remainingDistance, deltaTime);
        }

        public void Exit(NavigationData localData, NPCGlobalData globalData)
        {
            UnsubscribeFromChaseAnimationCycleEndingEvent(globalData);
            globalData.NavAgent.ResetPath();
            _localData = null;
            _globalData = null;
        }

        private void SubscribeToChaseAnimationCycleEndingEvent(NPCGlobalData globalData)
        {
            if (globalData.BehaviourController == null)
            {
                return;
            }

            globalData.BehaviourController.chaseAnimationCycleEndingEvent -= OnChaseAnimationCycleEnding;
            globalData.BehaviourController.chaseAnimationCycleEndingEvent += OnChaseAnimationCycleEnding;
        }

        private void UnsubscribeFromChaseAnimationCycleEndingEvent(NPCGlobalData globalData)
        {
            if (globalData.BehaviourController == null)
            {
                return;
            }

            globalData.BehaviourController.chaseAnimationCycleEndingEvent -= OnChaseAnimationCycleEnding;
        }

        private void OnChaseAnimationCycleEnding()
        {
            if (_localData == null || _globalData == null || _globalData.IsAttacking || _globalData.Anim == null)
            {
                return;
            }

            float estimatedSecondsToDestination = _localData.GetEstimatedSecondsToDestination();
            if (float.IsInfinity(estimatedSecondsToDestination) || float.IsNaN(estimatedSecondsToDestination))
            {
                return;
            }

            if (estimatedSecondsToDestination >= AttackEstimateMinSeconds && estimatedSecondsToDestination <= AttackEstimateMaxSeconds)
            {
                _globalData.Anim.SetTrigger("Attack");
                _globalData.IsAttacking = true;
            }
        }
    }
}
