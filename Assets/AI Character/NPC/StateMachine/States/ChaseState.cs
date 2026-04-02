using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public sealed class ChaseState : IState<NavigationData, NPCGlobalData>
    {
        private AttackAnimationData[] attackAnimations;
        private NavigationData _localData;
        private NPCGlobalData _globalData;

        public ChaseState()
        {
            Debug.Log("Chase state construct");
            attackAnimations = new[]
            {
                new AttackAnimationData()
                {
                    StateName = "LongRightSwipe01",
                    TargetDistanceOnAction = 1.35f,
                    SecondsUntilAction = 0.5f,
                    DistanceToAction = 3.5f
                    
                },
                new AttackAnimationData()
                {
                    StateName = "ChasePush01",
                    TargetDistanceOnAction = 1.2f,
                    SecondsUntilAction = 0.7F,
                    DistanceToAction = 1.91f
                },
                new AttackAnimationData()
                {
                    StateName = "ChasePush02",
                    TargetDistanceOnAction = 1.1f,
                    SecondsUntilAction = 0.44f,
                    DistanceToAction = 2.41f
                    
                },
                new AttackAnimationData()
                {
                    StateName = "SlideRightSwipe01",
                    TargetDistanceOnAction = 1.15f,
                    SecondsUntilAction = 0.8f,
                    DistanceToAction = 2.7f
                    
                }
            };
        }

        public void Enter(NavigationData localData, NPCGlobalData globalData)
        {
            Debug.Log("Enter Chase");
            _localData = localData;
            _globalData = globalData;

            globalData.NavAgent.isStopped = false;
            globalData.Anim.SetBool("IsChasing01", true);
            SubscribeToChaseAnimationCycleEndingEvent(globalData);
        }

        public void Tick(NavigationData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }
            
            globalData.NavAgent.SetDestination(globalData.CurrentTarget.position);
        }

        public void Exit(NavigationData localData, NPCGlobalData globalData)
        {
            Debug.Log("Exit Chase");
            UnsubscribeFromChaseAnimationCycleEndingEvent(globalData);
            globalData.NavAgent.ResetPath();
            _localData = null;
            _globalData = null;
        }

        private void SubscribeToChaseAnimationCycleEndingEvent(NPCGlobalData globalData)
        {
            if (globalData.BehaviourController == null)
            {
                Debug.Log("Subscribe cycle Failed");
                return;
            }
            Debug.Log("Subscribe cycle");
            globalData.BehaviourController.chaseAnimationCycleEndingEvent += OnChaseAnimationCycleEnding;
        }

        private void UnsubscribeFromChaseAnimationCycleEndingEvent(NPCGlobalData globalData)
        {
            if (globalData.BehaviourController == null)
            {
                Debug.Log("UnSubscribe cycle Failed");
                return;
            }
            Debug.Log("UnSubscribe cycle");
            globalData.BehaviourController.chaseAnimationCycleEndingEvent -= OnChaseAnimationCycleEnding;
        }

        private void OnChaseAnimationCycleEnding()
        {
            if (_localData == null || _globalData == null || _globalData.IsAttacking || _globalData.Anim == null)
            {
                Debug.Log("OnChaseAnimationCycleEnding Failed");
                return;
            }

            foreach (AttackAnimationData animationData in attackAnimations)
            {
                float predictedDistanceFromTargetOnAction = _globalData.PredictTargetDistanceInTime(animationData.SecondsUntilAction);
                if (Mathf.Abs(predictedDistanceFromTargetOnAction - animationData.DistanceToAction) < 0.3f)
                {
                    _globalData.Anim.SetTrigger(animationData.StateName);
                    _globalData.IsAttacking = true;
                }
            }
            
        }
    }
}
