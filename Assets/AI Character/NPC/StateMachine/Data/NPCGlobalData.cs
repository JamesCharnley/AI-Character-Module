using UnityEngine;
using UnityEngine.AI;
using AICharacterModule.NPC;

namespace AICharacterModule.NPC.StateMachine.Data
{
    /// <summary>
    /// Data shared by all states in all sub-state machines.
    /// Owned by the master state machine.
    /// </summary>
    public struct AttackAnimationData
    {
        public string StateName;
        public float SecondsUntilAction;
        public float TargetDistanceOnAction;
        public float DistanceToAction;
    }
    public class NPCGlobalData
    {
        public NPCBehaviourController BehaviourController;
        public Transform NpcTransform;
        public NavMeshAgent NavAgent;
        private Vector3 TargetVelocity;
        private Vector3 TargetPrevPosition;
        public Transform CurrentTarget
        {
            get => currentTarget;
            set
            {
                currentTarget = value;
                if (currentTarget.TryGetComponent(out CharacterController characterController))
                {
                    PlayerCharacterController = characterController;
                }
            }
        }

        private Transform currentTarget;
        public CharacterController PlayerCharacterController;
        public Animator Anim;
        public bool IsAttacking;
        public float Health = 100f;
        public float DetectionRange = 70f;
        public float AttackRange = 2f;
        public Vector3 NpcLastVelocity;
        public float PredictTargetDistanceInTime(float _time)
        {
            Vector3 predictedPlayerPosition = (PlayerCharacterController.transform.position + Vector3.down) +
                                               TargetVelocity * _time;
            Debug.Log(TargetVelocity);
            Debug.Log(Vector3.Distance(NavAgent.transform.position, predictedPlayerPosition));
            return Vector3.Distance(NavAgent.transform.position, predictedPlayerPosition);
        }

        public Vector3 GetTargetVelocity()
        {
            return TargetVelocity;
        }

        public void Tick(float _deltaTime)
        {
            TargetVelocity = (PlayerCharacterController.transform.position - TargetPrevPosition) / Time.deltaTime;
            TargetPrevPosition = PlayerCharacterController.transform.position;
        }
    }
}
