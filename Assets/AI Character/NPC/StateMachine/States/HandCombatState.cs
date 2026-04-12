using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class HandCombatState : IState<CombatData, NPCGlobalData>
    {
        private readonly MonoBehaviour _controllerMonoBehaviour;

        private bool IsIdle = false;
        private bool IsMovingCloser = false;
        public bool IsLocked { get; private set; }
        
        private AttackAnimationData[] attackAnimations;

        public HandCombatState(MonoBehaviour controllerMonoBehaviour)
        {
            _controllerMonoBehaviour = controllerMonoBehaviour;
            
            attackAnimations = new[]
            {
                new AttackAnimationData()
                {
                    StateName = "PowerKick",
                    TargetDistanceOnAction = 1.35f,
                    SecondsUntilAction = 0.26f,
                    DistanceToAction = 3f
                },
                new AttackAnimationData()
                {
                    StateName = "RightStraightPunch01",
                    TargetDistanceOnAction = 1.2f,
                    SecondsUntilAction = 0.5f,
                    DistanceToAction = 5.75f
                    
                },
                new AttackAnimationData()
                {
                    StateName = "LeftStraightPunch01",
                    TargetDistanceOnAction = 1.05f,
                    SecondsUntilAction = 1,
                    DistanceToAction = 5f
                },
                new AttackAnimationData() // cross
                {
                    StateName = "PunchCombo01",
                    TargetDistanceOnAction = 0.9f,
                    SecondsUntilAction = 0.44f,
                    DistanceToAction = 3.65f
                    
                }
            };
        }

        public void Enter(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            Debug.Log($"{GetType().Name} Enter");
            IsIdle = true;
            IsMovingCloser = false;
            globalData.Anim.SetTrigger("Idle");
            globalData.NavAgent.isStopped = true;
            IsLocked = false;
        }

        public void Tick(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (globalData.CurrentTarget == null)
            {
                return;
            }
            
            float distanceToTarget =
                Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position);

            if (!IsIdle && distanceToTarget < 1.5f)
            {
                IsIdle = true;
                IsMovingCloser = false;
            }
            else if (IsIdle && distanceToTarget > 2)
            {
                IsMovingCloser = true;
                IsIdle = false;
            }
            Animator anim = globalData.Anim;
            if (IsIdle)
            {
                anim.SetBool("Combat01_MoveCloserLong", false);  
                anim.SetBool("Combat01_MoveCloserShort", false); 
                anim.SetBool("Combat01_MoveCloserXLong", false); 
                Vector3 directionToTarget = globalData.CurrentTarget.position - globalData.NpcTransform.position;
                directionToTarget.y = 0f;
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                    globalData.NpcTransform.rotation = Quaternion.RotateTowards(globalData.NpcTransform.rotation, targetRotation, 720f * deltaTime);
                }

                if (!globalData.IsAttacking)
                {
                    EvaluateAttacks(localData, globalData);
                }
            }

            if (IsMovingCloser)
            {
                globalData.NavAgent.isStopped = false;
                globalData.NavAgent.SetDestination(globalData.CurrentTarget.position - Vector3.up);
                if (distanceToTarget > 3)
                {
                    anim.SetBool("Combat01_MoveCloserShort", false);   
                    anim.SetBool("Combat01_MoveCloserLong", false);   
                    anim.SetBool("Combat01_MoveCloserXLong", true);  
                }
                else if (distanceToTarget > 2)
                {
                    anim.SetBool("Combat01_MoveCloserShort", false);   
                    anim.SetBool("Combat01_MoveCloserLong", true);   
                    anim.SetBool("Combat01_MoveCloserXLong", false);   
                }
                else if (distanceToTarget <= 2)
                {
                    anim.SetBool("Combat01_MoveCloserShort", true);   
                    anim.SetBool("Combat01_MoveCloserLong", false);
                    anim.SetBool("Combat01_MoveCloserXLong", false);   
                }
            }
            
        }

        void EvaluateAttacks(CombatData _localData, NPCGlobalData _globalData)
        {
            float distanceToTarget = Vector3.Distance(_globalData.NpcTransform.position,
                _globalData.CurrentTarget.position - Vector3.up);

            AttackAnimationData bestAttack = new();
            float shortestDiff = 9999f;
            
            foreach (AttackAnimationData attackAnimationData in attackAnimations)
            {
                float diff = Mathf.Abs(distanceToTarget - attackAnimationData.TargetDistanceOnAction);
                if (diff < shortestDiff)
                {
                    shortestDiff = diff;
                    bestAttack = attackAnimationData;
                }
            }

            _globalData.IsAttacking = true;
            _globalData.Anim.SetTrigger(bestAttack.StateName);
        }

        public void Exit(CombatData localData, NPCGlobalData globalData)
        {
            IsLocked = true;
            globalData.NavAgent.isStopped = false;
            Animator anim = globalData.Anim;
            anim.SetBool("Combat01_MoveCloserShort", false);   
            anim.SetBool("Combat01_MoveCloserLong", false);
            anim.SetBool("Combat01_MoveCloserXLong", false);  
            IsLocked = false;
        }
    }
}
