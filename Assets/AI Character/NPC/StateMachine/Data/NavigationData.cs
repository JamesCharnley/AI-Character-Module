using System.Collections.Generic;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.Data
{
    /// <summary>
    /// Data local to the navigation sub-state machine.
    /// </summary>
    public class NavigationData
    {
        private readonly Queue<GapSample> gapSamples = new Queue<GapSample>();
        private float elapsedTrackingTime;

        private struct GapSample
        {
            public float Time;
            public float RemainingGapToAttack;
        }

        public Vector3 PatrolPoint;
        public float ReachedThreshold = 3.0f;
        public float RemainingDistanceAverageWindowSeconds = 3.0f;
        public float AttackAnimationTravelDistance = 1.0f;

        /// <summary>
        /// Track remaining gap to attack over time so an arrival estimate can be calculated.
        /// This uses the real NPC-target distance and subtracts distance that will be covered
        /// by the attack animation itself.
        /// </summary>
        public void UpdateAttackGapHistory(float npcToTargetDistance, float attackRange, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            elapsedTrackingTime += deltaTime;

            float remainingGapToAttack = Mathf.Max(npcToTargetDistance - attackRange - AttackAnimationTravelDistance, 0f);

            gapSamples.Enqueue(new GapSample
            {
                Time = elapsedTrackingTime,
                RemainingGapToAttack = remainingGapToAttack
            });

            TrimGapHistory();
        }

        /// <summary>
        /// Estimate seconds to attack using average gap-closing speed from recent samples.
        /// Returns Mathf.Infinity when there is not enough data or if the NPC is not closing in.
        /// </summary>
        public float GetEstimatedSecondsToAttack()
        {
            if (gapSamples.Count < 2)
            {
                return Mathf.Infinity;
            }

            GapSample[] samples = gapSamples.ToArray();
            GapSample oldest = samples[0];
            GapSample latest = samples[samples.Length - 1];

            float deltaTime = latest.Time - oldest.Time;
            if (deltaTime <= 0f)
            {
                return Mathf.Infinity;
            }

            float gapReduction = oldest.RemainingGapToAttack - latest.RemainingGapToAttack;
            float averageGapReductionPerSecond = gapReduction / deltaTime;
            if (averageGapReductionPerSecond <= 0f)
            {
                return Mathf.Infinity;
            }

            return latest.RemainingGapToAttack / averageGapReductionPerSecond;
        }

        /// <summary>
        /// Call this whenever a new destination is chosen.
        /// </summary>
        public void ResetArrivalEstimateTracking()
        {
            gapSamples.Clear();
            elapsedTrackingTime = 0f;
        }

        private void TrimGapHistory()
        {
            float historyWindow = Mathf.Max(RemainingDistanceAverageWindowSeconds, 0.1f);
            while (gapSamples.Count > 0 && elapsedTrackingTime - gapSamples.Peek().Time > historyWindow)
            {
                gapSamples.Dequeue();
            }
        }
    }
}
