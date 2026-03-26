using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class ChaseState : IState<NavigationData, NPCGlobalData>
    {
        private const float ChaseAttackEstimateMinSeconds = 3.1f;
        private const float ChaseAttackEstimateMaxSeconds = 5f;
        private const float PushStopAttackEstimateMinSeconds = 0;
        private const float PushStopAttackEstimateMaxSeconds = 3f;
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
            
            if (_globalData.NavAgent.remainingDistance < 0.7f)
            {
                _globalData.Anim.SetTrigger("PushStop");
                _globalData.IsAttacking = true;
            }

            globalData.NavAgent.SetDestination(globalData.CurrentTarget.position);

            float npcToTargetDistance = Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position);
            localData.UpdateAttackGapHistory(npcToTargetDistance, globalData.AttackRange, deltaTime);
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

            
            if (_globalData.NavAgent.remainingDistance < 3 && _globalData.NavAgent.remainingDistance > 2)
            {
                _globalData.Anim.SetTrigger("Attack");
                _globalData.IsAttacking = true;
            }

            return;
            float estimatedSecondsToAttack = _localData.GetEstimatedSecondsToAttack();
            if (float.IsInfinity(estimatedSecondsToAttack) || float.IsNaN(estimatedSecondsToAttack))
            {
                return;
            }
            
            if (estimatedSecondsToAttack >= PushStopAttackEstimateMinSeconds && estimatedSecondsToAttack <= PushStopAttackEstimateMaxSeconds)
            {
                _globalData.Anim.SetTrigger("PushStop");
                _globalData.IsAttacking = true;
            }

            if (estimatedSecondsToAttack >= ChaseAttackEstimateMinSeconds && estimatedSecondsToAttack <= ChaseAttackEstimateMaxSeconds)
            {
                _globalData.Anim.SetTrigger("Attack");
                _globalData.IsAttacking = true;
            }
        }
    }
}
