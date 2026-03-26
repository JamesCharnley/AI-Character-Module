using System.Collections;
using AICharacterModule.NPC.StateMachine.Core;
using AICharacterModule.NPC.StateMachine.Data;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.States
{
    public class PatrolState : IState<NavigationData, NPCGlobalData>
    {
        private bool isIdle = false;
        public void Enter(NavigationData localData, NPCGlobalData globalData)
        {
            if (localData.PatrolPoint == Vector3.zero)
            {
                localData.PatrolPoint = globalData.NpcTransform.position + Vector3.right * 4f;
            }

            globalData.NavAgent.isStopped = false;
            globalData.NavAgent.SetDestination(localData.PatrolPoint);
            localData.ResetArrivalEstimateTracking();
            globalData.Anim.SetTrigger("Walk");
        }

        public void Tick(NavigationData localData, NPCGlobalData globalData, float deltaTime)
        {
            if (!isIdle && !globalData.NavAgent.pathPending && globalData.NavAgent.remainingDistance <= localData.ReachedThreshold)
            {
                globalData.NpcTransform.GetComponent<MonoBehaviour>().StartCoroutine(WaitForSeconds(5, localData, globalData));
                isIdle = true;
                globalData.Anim.SetTrigger("Idle");
            }
        }

        public void Exit(NavigationData localData, NPCGlobalData globalData)
        {
            globalData.NavAgent.ResetPath();
        }

        IEnumerator WaitForSeconds(float _seconds, NavigationData localData, NPCGlobalData globalData)
        {
            yield return new WaitForSeconds(_seconds);
            localData.PatrolPoint = globalData.NpcTransform.position + Random.insideUnitSphere * 20f;
            localData.PatrolPoint.y = globalData.NpcTransform.position.y;
            globalData.NavAgent.SetDestination(localData.PatrolPoint);
            localData.ResetArrivalEstimateTracking();
            isIdle = false;
            globalData.Anim.SetTrigger("Walk");
        }
    }
}
