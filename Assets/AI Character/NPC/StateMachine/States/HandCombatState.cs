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

        public HandCombatState(MonoBehaviour controllerMonoBehaviour)
        {
            _controllerMonoBehaviour = controllerMonoBehaviour;
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

            if (distanceToTarget < 2)
            {
                IsIdle = true;
                IsMovingCloser = false;
            }
            else
            {
                IsMovingCloser = true;
                IsIdle = false;
            }
            Animator anim = globalData.Anim;
            if (IsIdle)
            {
                anim.SetBool("Combat01_MoveCloserLong", false);  
                anim.SetBool("Combat01_MoveCloserShort", false); 
                Vector3 directionToTarget = globalData.CurrentTarget.position - globalData.NpcTransform.position;
                directionToTarget.y = 0f;
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                    globalData.NpcTransform.rotation = Quaternion.RotateTowards(globalData.NpcTransform.rotation, targetRotation, 720f * deltaTime);
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
