using System.Collections;
using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;
using UnityEngine.AI;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class HandCombatState : IState<CombatData, NPCGlobalData>
    {
        private readonly MonoBehaviour _controllerMonoBehaviour;

        private bool IsIdle = false;
        private bool IsMoving = false;
        public bool IsLocked { get; private set; }
        
        private AttackAnimationData[] attackAnimations;
        private bool WasAttacking = false;
        private bool AttackCoolingDown = false;
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
            IsMoving = false;
            globalData.Anim.SetBool("InCombat01", true);
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
                Vector3.Distance(globalData.NpcTransform.position, globalData.CurrentTarget.position - Vector3.up);

            if (!IsIdle && distanceToTarget < 1.5f && distanceToTarget > 0.6f)
            {
                IsIdle = true;
                IsMoving = false;
            }
            else if (IsIdle && distanceToTarget > 2 || distanceToTarget <= 0.6f)
            {
                IsMoving = true;
                IsIdle = false;
            }
            Animator anim = globalData.Anim;
            if (IsIdle)
            {
                globalData.NavAgent.updateRotation = true;
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

                if (!globalData.IsAttacking && !globalData.IsDodging && globalData.NavAgent.speed < 0.1f)
                {
                    if (ShouldDodge(localData, globalData, Time.deltaTime))
                    {
                        globalData.IsDodging = true;
                        globalData.AnimationController.PlayDodgeAnimation(globalData.BehaviourController.GetCurrentIncomingAttack.HitZoneData.LocalOffset);
                    }
                    else if (!WasAttacking && !AttackCoolingDown)
                    {
                        EvaluateAttacks(localData, globalData);
                    }
                    else if(!AttackCoolingDown)
                    {
                        AttackCoolingDown = true;
                        _controllerMonoBehaviour.StartCoroutine(AttackCooldown());
                    }
                    
                }
            }

            if (IsMoving)
            {
                
                globalData.NavAgent.isStopped = false;
                globalData.NavAgent.SetDestination(globalData.CurrentTarget.position - Vector3.up);
                if (distanceToTarget > 3)
                {
                    anim.SetBool("Combat01_MoveCloserShort", false);   
                    anim.SetBool("Combat01_MoveCloserLong", false);   
                    anim.SetBool("Combat01_MoveCloserXLong", true);  
                    anim.SetBool("Combat01_MoveAwayXLong", false); 
                    globalData.NavAgent.updateRotation = true;
                }
                else if (distanceToTarget > 2)
                {
                    anim.SetBool("Combat01_MoveCloserShort", false);   
                    anim.SetBool("Combat01_MoveCloserLong", true);   
                    anim.SetBool("Combat01_MoveCloserXLong", false);   
                    anim.SetBool("Combat01_MoveAwayXLong", false); 
                    globalData.NavAgent.updateRotation = true;
                }
                else if (distanceToTarget <= 2 && distanceToTarget > 0.6f)
                {
                    anim.SetBool("Combat01_MoveCloserShort", true);   
                    anim.SetBool("Combat01_MoveCloserLong", false);
                    anim.SetBool("Combat01_MoveCloserXLong", false);  
                    anim.SetBool("Combat01_MoveAwayXLong", false); 
                    globalData.NavAgent.updateRotation = true;
                }
                if(distanceToTarget < 1)
                {
                    Vector3 sourcePos = globalData.NpcTransform.position + (globalData.NpcTransform.position -
                                                                            (globalData.CurrentTarget.position -
                                                                             Vector3.up)).normalized * 3;
                    Debug.DrawLine(sourcePos, sourcePos + Vector3.up * 5, Color.green, 1);
                    if (NavMesh.SamplePosition(sourcePos, out NavMeshHit hit, 1, NavMesh.AllAreas))
                    {
                        globalData.NavAgent.updateRotation = false;
                        globalData.NavAgent.SetDestination(hit.position);
                        anim.SetBool("Combat01_MoveCloserShort", false);   
                        anim.SetBool("Combat01_MoveCloserLong", false);
                        anim.SetBool("Combat01_MoveCloserXLong", false); 
                        anim.SetBool("Combat01_MoveAwayXLong", true); 
                    }
                }
                
            }

            WasAttacking = globalData.IsAttacking;

        }

        bool ShouldDodge(CombatData localData, NPCGlobalData globalData, float deltaTime)
        {
            IncomingAttackData data = globalData.BehaviourController.GetCurrentIncomingAttack;
            if (data.Type == EAttackType.None)
            {
                return false;
            }

            float timePassed = Mathf.Abs(Time.time - data.TimeStamp);
            if (data.Type == EAttackType.Melee)
            {
                if (timePassed < 0.2f)
                {
                    return true;
                }
            }
            
            return false;
        }

        IEnumerator AttackCooldown()
        {
            yield return new WaitForSeconds(2);
            AttackCoolingDown = false;
        }

        void EvaluateAttacks(CombatData _localData, NPCGlobalData _globalData)
        {
            float distanceToTarget = Vector3.Distance(_globalData.NpcTransform.position,
                _globalData.CurrentTarget.position - Vector3.up);

            AttackAnimationData bestAttack = new();
            float shortestDiff = 9999f;

            float buff = 0;
            if (_globalData.GetTargetVelocity().magnitude > 0.1f)
            {
                buff = -0.25f;
            }
            if (_globalData.GetTargetVelocity().magnitude < -0.1f)
            {
                buff = 0.25f;
            }

            distanceToTarget += buff;
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
            globalData.Anim.SetBool("InCombat01", false);
            IsLocked = false;
        }
    }
}
