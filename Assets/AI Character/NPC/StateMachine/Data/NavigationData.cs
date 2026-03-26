using System.Collections.Generic;
using UnityEngine;

namespace AICharacterModule.NPC.StateMachine.Data
{
    /// <summary>
    /// Data local to the navigation sub-state machine.
    /// </summary>
    public class NavigationData
    {
        private readonly Queue<DistanceSample> distanceSamples = new Queue<DistanceSample>();
        private float elapsedTrackingTime;

        private struct DistanceSample
        {
            public float Time;
            public float RemainingDistance;
        }

        public Vector3 PatrolPoint;
        public float ReachedThreshold = 3.0f;
        public float RemainingDistanceAverageWindowSeconds = 3.0f;

        /// <summary>
        /// Track remaining path distance over time so an arrival estimate can be calculated.
        /// </summary>
        public void UpdateRemainingDistanceHistory(float remainingDistance, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            elapsedTrackingTime += deltaTime;

            distanceSamples.Enqueue(new DistanceSample
            {
                Time = elapsedTrackingTime,
                RemainingDistance = Mathf.Max(remainingDistance, 0f)
            });

            TrimDistanceHistory();
        }

        /// <summary>
        /// Estimate seconds to destination using average progress from recent distance samples.
        /// Returns Mathf.Infinity when there is not enough progress data.
        /// </summary>
        public float GetEstimatedSecondsToDestination()
        {
            if (distanceSamples.Count < 2)
            {
                return Mathf.Infinity;
            }

            DistanceSample[] samples = distanceSamples.ToArray();
            DistanceSample oldest = samples[0];
            DistanceSample latest = samples[samples.Length - 1];

            float deltaTime = latest.Time - oldest.Time;
            if (deltaTime <= 0f)
            {
                return Mathf.Infinity;
            }

            float distanceReduction = oldest.RemainingDistance - latest.RemainingDistance;
            float averageDistanceReductionPerSecond = distanceReduction / deltaTime;
            if (averageDistanceReductionPerSecond <= 0f)
            {
                return Mathf.Infinity;
            }

            return latest.RemainingDistance / averageDistanceReductionPerSecond;
        }

        /// <summary>
        /// Call this whenever a new destination is chosen.
        /// </summary>
        public void ResetArrivalEstimateTracking()
        {
            distanceSamples.Clear();
            elapsedTrackingTime = 0f;
        }

        private void TrimDistanceHistory()
        {
            float historyWindow = Mathf.Max(RemainingDistanceAverageWindowSeconds, 0.1f);
            while (distanceSamples.Count > 0 && elapsedTrackingTime - distanceSamples.Peek().Time > historyWindow)
            {
                distanceSamples.Dequeue();
            }
        }
    }
}
