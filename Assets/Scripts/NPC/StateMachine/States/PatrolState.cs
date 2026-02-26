using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class PatrolState : IState<NavigationData, NPCGlobalData>
    {
        public void Enter(NavigationData localData, NPCGlobalData globalData)
        {
            if (localData.PatrolPoint == Vector3.zero)
            {
                localData.PatrolPoint = globalData.NpcTransform.position + Vector3.right * 4f;
            }

            globalData.NavAgent.isStopped = false;
            globalData.NavAgent.SetDestination(localData.PatrolPoint);
        }

        public void Tick(NavigationData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (!globalData.NavAgent.pathPending && globalData.NavAgent.remainingDistance <= localData.ReachedThreshold)
            {
                localData.PatrolPoint = globalData.NpcTransform.position + Random.insideUnitSphere * 4f;
                localData.PatrolPoint.y = globalData.NpcTransform.position.y;
                globalData.NavAgent.SetDestination(localData.PatrolPoint);
            }
        }

        public void Exit(NavigationData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.ResetPath();
        }
    }
}
