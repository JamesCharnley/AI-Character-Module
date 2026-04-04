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



        public bool TryFindPositionOnTargetRadius(
            float targetRadius,
            Vector3 targetPosition,
            float minDistanceFromNpc,
            float maxDistanceFromNpc,
            out Vector3 bestPosition,
            bool? orbitClockwise = null,
            float radiusErrorMargin = 0.5f,
            int sampleCount = 32)
        {
            bestPosition = Vector3.zero;

            if (NavAgent == null || targetRadius <= 0f || maxDistanceFromNpc < 0f || minDistanceFromNpc > maxDistanceFromNpc)
            {
                return false;
            }

            Vector3 npcPosition = NpcTransform != null ? NpcTransform.position : NavAgent.transform.position;
            float targetNpcDistance = (minDistanceFromNpc + maxDistanceFromNpc) * 0.5f;
            Vector3 toNpcFromTarget = npcPosition - targetPosition;
            toNpcFromTarget.y = 0f;
            toNpcFromTarget = toNpcFromTarget.sqrMagnitude > 0.0001f ? toNpcFromTarget.normalized : Vector3.forward;
            float bestScore = float.MaxValue;
            bool foundValidPoint = false;
            float angleStep = Mathf.PI * 2f / Mathf.Max(1, sampleCount);

            for (int i = 0; i < sampleCount; i++)
            {
                float angle = i * angleStep;
                Vector3 radialOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * targetRadius;
                Vector3 candidate = targetPosition + radialOffset;
                if (orbitClockwise.HasValue)
                {
                    Vector3 toCandidateFromTarget = candidate - targetPosition;
                    toCandidateFromTarget.y = 0f;
                    if (toCandidateFromTarget.sqrMagnitude < 0.0001f)
                    {
                        continue;
                    }

                    float signedAngle = Vector3.SignedAngle(toNpcFromTarget, toCandidateFromTarget.normalized, Vector3.up);
                    bool isClockwiseDirection = signedAngle < 0f;
                    if (isClockwiseDirection != orbitClockwise.Value)
                    {
                        continue;
                    }
                }

                float distanceFromNpc = Vector3.Distance(npcPosition, candidate);
                if (distanceFromNpc < minDistanceFromNpc || distanceFromNpc > maxDistanceFromNpc)
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, radiusErrorMargin, NavMesh.AllAreas))
                {
                    continue;
                }

                float radiusError = Mathf.Abs(Vector3.Distance(navHit.position, targetPosition) - targetRadius);
                if (radiusError > radiusErrorMargin)
                {
                    continue;
                }

                NavMeshPath candidatePath = new NavMeshPath();
                if (!NavMesh.CalculatePath(npcPosition, navHit.position, NavMesh.AllAreas, candidatePath) ||
                    candidatePath.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float score = Mathf.Abs(distanceFromNpc - targetNpcDistance) + radiusError;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestPosition = navHit.position;
                foundValidPoint = true;
            }

            return foundValidPoint;
        }

        public void Tick(float _deltaTime)
        {
            TargetVelocity = (PlayerCharacterController.transform.position - TargetPrevPosition) / Time.deltaTime;
            TargetPrevPosition = PlayerCharacterController.transform.position;
        }
    }
}
