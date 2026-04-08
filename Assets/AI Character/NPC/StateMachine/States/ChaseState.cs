using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using Unity.VisualScripting;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public sealed class ChaseState : IState<NavigationData, NPCGlobalData>
    {
        private readonly MonoBehaviour _controllerMonoBehaviour;
        private AttackAnimationData[] attackAnimations;
        private NavigationData _localData;
        private NPCGlobalData _globalData;
        public bool IsLocked { get; private set; }

        public ChaseState(MonoBehaviour controllerMonoBehaviour)
        {
            _controllerMonoBehaviour = controllerMonoBehaviour;
            Debug.Log("Chase state construct");
            attackAnimations = new[]
            {
                new AttackAnimationData()
                {
                    StateName = "LongRightSwipe01",
                    TargetDistanceOnAction = 1.35f,
                    SecondsUntilAction = 0.5f,
                    DistanceToAction = 5.75f
                    
                },
                new AttackAnimationData()
                {
                    StateName = "ChasePush01",
                    TargetDistanceOnAction = 1.2f,
                    SecondsUntilAction = 1,
                    DistanceToAction = 5f
                },
                new AttackAnimationData() // cross
                {
                    StateName = "ChasePush02",
                    TargetDistanceOnAction = 1.1f,
                    SecondsUntilAction = 0.44f,
                    DistanceToAction = 3.65f
                    
                },
                new AttackAnimationData()
                {
                    StateName = "ChasePush03",
                    TargetDistanceOnAction = 1.15f,
                    SecondsUntilAction = 0.26f,
                    DistanceToAction = 3f
                    
                },
                new AttackAnimationData()
                {
                    StateName = "SlideRightSwipe01",
                    TargetDistanceOnAction = 1.15f,
                    SecondsUntilAction = 0.8f,
                    DistanceToAction = 5f
                    
                }
            };
        }

        public void Enter(NavigationData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            Debug.Log($"{GetType().Name} Enter");
            _localData = localData;
            _globalData = globalData;

            globalData.NavAgent.isStopped = false;
            globalData.Anim.SetBool("IsChasing01", true);
            SubscribeToChaseAnimationCycleEndingEvent(globalData);
            IsLocked = false;
        }

        private bool hasAttacked = false;
        public void Tick(NavigationData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }
            //if (hasAttacked && !globalData.IsAttacking)
            //{
            //    hasAttacked = false;
            //    globalData.NavAgent.Warp(new Vector3(0, 0.5f, 0));
            //}
            //if(globalData.NavAgent.remainingDistance < 0.5f)
            //{
            //    globalData.NavAgent.Warp(new Vector3(0, 0.5f, 0));
            //}
            //OnChaseAnimationCycleEnding();
            globalData.NavAgent.SetDestination(globalData.CurrentTarget.position);

            float distanceToTarget = Vector3.Distance(globalData.CurrentTarget.transform.position + Vector3.down,
                _globalData.NavAgent.transform.position);
            if (distanceToTarget < 3 && !globalData.IsAttacking)
            {
                if (globalData.GetTargetVelocity().magnitude < 0.1f)
                {
                    globalData.IsAttacking = true;
                    globalData.Anim.SetTrigger("ChasePush03");
                }
                else
                {
                    Vector3 towards = (globalData.NavAgent.transform.position - globalData.CurrentTarget.position)
                        .normalized;
                    float dot = Vector3.Dot(globalData.GetTargetVelocity().normalized, towards);
                    if (dot >= 0)
                    {
                        globalData.IsAttacking = true;
                        globalData.Anim.SetTrigger("ChasePush03");
                    }
                }
                
            }
            if (distanceToTarget < 1f)
            {
                if (globalData.CurrentTarget.TryGetComponent(out PlayerController player))
                {
                    player.AddImpulse(globalData.NavAgent.transform.forward * 40);
                    hasAttacked = true;
                }
            }

            
        }

        public void Exit(NavigationData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            Debug.Log("Exit Chase");
            UnsubscribeFromChaseAnimationCycleEndingEvent(globalData);
            globalData.Anim.SetBool("IsChasing01", false);
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
           // Debug.Log("Subscribe cycle");
            globalData.BehaviourController.chaseAnimationCycleEndingEvent += OnChaseAnimationCycleEnding;
        }

        private void UnsubscribeFromChaseAnimationCycleEndingEvent(NPCGlobalData globalData)
        {
            if (globalData.BehaviourController == null)
            {
                Debug.Log("UnSubscribe cycle Failed");
                return;
            }
           // Debug.Log("UnSubscribe cycle");
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
                if (Mathf.Abs(predictedDistanceFromTargetOnAction - animationData.DistanceToAction) < 0.1f)
                {
                    _globalData.Anim.SetTrigger(animationData.StateName);
                    _globalData.IsAttacking = true;
                    break;
                }
            }
            
        }
    }
}
