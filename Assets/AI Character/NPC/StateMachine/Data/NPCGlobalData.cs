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
            //Debug.Log(TargetVelocity);
            //Debug.Log(Vector3.Distance(NavAgent.transform.position, predictedPlayerPosition));
            return Vector3.Distance(NavAgent.transform.position, predictedPlayerPosition);
        }

        public Vector3 GetTargetVelocity()
        {
            return TargetVelocity;
        }
        /// <summary>
        /// Tries to locate the best reachable position on (or very near) a ring around the target.
        /// </summary>
        /// <remarks>
        /// The search performs an angular sweep around <paramref name="targetPosition"/> and evaluates
        /// each sampled point in multiple stages:
        /// 1) Optional directional filtering (clockwise / counter-clockwise relative to the NPC),
        /// 2) NPC distance envelope filtering,
        /// 3) NavMesh projection and radius tolerance validation,
        /// 4) Full path reachability validation,
        /// 5) Scoring and best-candidate selection.
        ///
        /// The final score intentionally favors positions that keep the NPC near the midpoint of the
        /// requested NPC-distance range, while still staying close to the desired orbit radius.
        /// </remarks>
        /// <param name="targetRadius">Desired orbit radius around the target in world units.</param>
        /// <param name="targetPosition">World position of the target center point.</param>
        /// <param name="minDistanceFromNpc">Minimum allowed distance between NPC and candidate point.</param>
        /// <param name="maxDistanceFromNpc">Maximum allowed distance between NPC and candidate point.</param>
        /// <param name="bestPosition">Best valid position found on the NavMesh.</param>
        /// <param name="orbitClockwise">
        /// Optional directional constraint:
        /// - null: either side of the target is allowed,
        /// - true: only clockwise-side candidates are considered,
        /// - false: only counter-clockwise-side candidates are considered.
        /// </param>
        /// <param name="radiusErrorMargin">
        /// Maximum tolerated deviation (in world units) from <paramref name="targetRadius"/> after
        /// NavMesh projection.
        /// </param>
        /// <param name="sampleCount">
        /// Number of angular samples around the full 360° ring. Higher values improve quality but
        /// increase per-call cost.
        /// </param>
        /// <returns>
        /// True when at least one valid and reachable candidate is found; otherwise false.
        /// </returns>
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

            // Fast guard for invalid input or missing navigation dependencies.
            if (NavAgent == null || targetRadius <= 0f || maxDistanceFromNpc < 0f || minDistanceFromNpc > maxDistanceFromNpc)
            {
                return false;
            }

            // Resolve current NPC position with a fallback to the NavAgent transform.
            Vector3 npcPosition = NpcTransform != null ? NpcTransform.position : NavAgent.transform.position;
            // Midpoint used by scoring so we prefer candidates "centered" in the allowed range.
            float targetNpcDistance = (minDistanceFromNpc + maxDistanceFromNpc) * 0.5f;

            // Flattened direction from target to NPC, used as the directional reference when orbit side is constrained.
            Vector3 toNpcFromTarget = npcPosition - targetPosition;
            toNpcFromTarget.y = 0f;
            toNpcFromTarget = toNpcFromTarget.sqrMagnitude > 0.0001f ? toNpcFromTarget.normalized : Vector3.forward;

            float bestScore = float.MaxValue;
            bool foundValidPoint = false;
            // Uniform angular sweep across the full ring around the target.
            float angleStep = Mathf.PI * 2f / Mathf.Max(1, sampleCount);

            for (int i = 0; i < sampleCount; i++)
            {
                // Build the candidate point on the ideal (unprojected) radius ring.
                float angle = i * angleStep;
                Vector3 radialOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * targetRadius;
                Vector3 candidate = targetPosition + radialOffset;

                // Optional orbit-direction filter so callers can enforce side consistency.
                if (orbitClockwise.HasValue)
                {
                    Vector3 toCandidateFromTarget = candidate - targetPosition;
                    toCandidateFromTarget.y = 0f;
                    if (toCandidateFromTarget.sqrMagnitude < 0.0001f)
                    {
                        continue;
                    }

                    // Signed angle (Y-up) determines if candidate lies clockwise or counter-clockwise from the NPC reference vector.
                    float signedAngle = Vector3.SignedAngle(toNpcFromTarget, toCandidateFromTarget.normalized, Vector3.up);
                    bool isClockwiseDirection = signedAngle < 0f;
                    if (isClockwiseDirection != orbitClockwise.Value)
                    {
                        continue;
                    }
                }

                // Keep only candidates where NPC travel distance is inside the requested bounds.
                float distanceFromNpc = Vector3.Distance(npcPosition, candidate);
                if (distanceFromNpc < minDistanceFromNpc || distanceFromNpc > maxDistanceFromNpc)
                {
                    continue;
                }

                // Snap candidate onto the NavMesh near the sampled point.
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, radiusErrorMargin, NavMesh.AllAreas))
                {
                    continue;
                }

                // Reject points that drift too far from the intended target radius after NavMesh projection.
                float radiusError = Mathf.Abs(Vector3.Distance(navHit.position, targetPosition) - targetRadius);
                if (radiusError > radiusErrorMargin)
                {
                    continue;
                }

                // Ensure the NPC can actually path to the candidate and that the path is complete.
                NavMeshPath candidatePath = new NavMeshPath();
                if (!NavMesh.CalculatePath(npcPosition, navHit.position, NavMesh.AllAreas, candidatePath) ||
                    candidatePath.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                // Lower score is better:
                // - distanceFromNpc term prefers the center of [minDistanceFromNpc, maxDistanceFromNpc]
                // - radiusError term prefers strict adherence to the desired orbit radius
                float score = Mathf.Abs(distanceFromNpc - targetNpcDistance) + radiusError;
                if (score >= bestScore)
                {
                    continue;
                }

                // Record the best valid candidate discovered so far.
                bestScore = score;
                bestPosition = navHit.position;
                foundValidPoint = true;
            }

            // Caller can branch on this to either move to bestPosition or apply a fallback behavior.
            return foundValidPoint;
        }

        public void Tick(float _deltaTime)
        {
            TargetVelocity = (PlayerCharacterController.transform.position - TargetPrevPosition) / Time.deltaTime;
            TargetPrevPosition = PlayerCharacterController.transform.position;
        }
    }
}
