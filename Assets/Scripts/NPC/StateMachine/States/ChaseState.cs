using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class ChaseState : IState<NavigationData, NPCGlobalData>
    {
        public void Enter(NavigationData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.isStopped = false;
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
            globalData.NavAgent.ResetPath();
        }
    }
}
