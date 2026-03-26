using UnityEngine;
using UnityEngine.AI;

namespace AICharacterModule.NPC.StateMachine.Data
{
    /// <summary>
    /// Data shared by all states in all sub-state machines.
    /// Owned by the master state machine.
    /// </summary>
    public class NPCGlobalData
    {
        public Transform NpcTransform;
        public NavMeshAgent NavAgent;
        public Transform CurrentTarget;
        public Animator Anim;
        public bool IsAttacking;
        public float Health = 100f;
        public float DetectionRange = 10f;
        public float AttackRange = 2f;
    }
}
