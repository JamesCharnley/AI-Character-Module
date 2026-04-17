using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace FirstPersonCharacter
{
    /// <summary>
    /// Procedurally punches by moving Animation Rigging IK hand targets.
    /// Attach this to your first-person arm rig root and assign the IK constraints/targets in the inspector.
    /// </summary>
    public class FirstPersonPunchRig : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Rig armRig;
        [SerializeField] private Transform spine;
        [SerializeField] private TwoBoneIKConstraint leftArmIK;
        [SerializeField] private TwoBoneIKConstraint rightArmIK;

        [Header("IK Targets")]
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightHandTarget;

        [Header("Input")]
        [SerializeField] private int mouseButton = 0;

        [Header("Punch Timing")]
        [Min(0.01f)] [SerializeField] private float windUpDuration = 0.06f;
        [Min(0.01f)] [SerializeField] private float strikeDuration = 0.1f;
        [Min(0.01f)] [SerializeField] private float recoverDuration = 0.12f;
        [Min(0f)] [SerializeField] private float punchCooldown = 0.03f;

        [Header("Punch Shape (Local Space)")]
        [SerializeField] private float forwardDistance = 0.33f;
        [SerializeField] private float inwardDistance = 0.05f;
        [SerializeField] private float upwardDistance = 0.02f;
        [SerializeField] private float windUpBackDistance = 0.08f;
        [SerializeField] private float centerlineX = 0f;
        [Range(0f, 1f)] [SerializeField] private float centerBias = 0.7f;
        [SerializeField] private float strikeArcHeight = 0.045f;

        [Header("Spine Motion")]
        [SerializeField] private float spinePitch = 8f;
        [SerializeField] private float spineYaw = 4f;

        [Header("Curves")]
        [SerializeField] private AnimationCurve windUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve strikeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve recoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 leftRestLocalPos;
        private Vector3 rightRestLocalPos;
        private Quaternion spineRestLocalRot;
        private bool punchRunning;
        private bool punchRightNext = true;
        private float lastPunchTime = -10f;

        private void Awake()
        {
            CacheRestPose();

            if (armRig != null)
            {
                armRig.weight = 1f;
            }

            if (leftArmIK != null)
            {
                leftArmIK.weight = 1f;
            }

            if (rightArmIK != null)
            {
                rightArmIK.weight = 1f;
            }
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(mouseButton))
            {
                return;
            }

            if (punchRunning || Time.time < lastPunchTime + punchCooldown)
            {
                return;
            }

            StartCoroutine(PunchRoutine(punchRightNext));
            punchRightNext = !punchRightNext;
            lastPunchTime = Time.time;
        }

        [ContextMenu("Cache Rest Pose")]
        public void CacheRestPose()
        {
            if (leftHandTarget != null)
            {
                leftRestLocalPos = leftHandTarget.localPosition;
            }

            if (rightHandTarget != null)
            {
                rightRestLocalPos = rightHandTarget.localPosition;
            }

            if (spine != null)
            {
                spineRestLocalRot = spine.localRotation;
            }
        }

        private IEnumerator PunchRoutine(bool useRightArm)
        {
            punchRunning = true;

            Transform activeTarget = useRightArm ? rightHandTarget : leftHandTarget;
            if (activeTarget == null)
            {
                punchRunning = false;
                yield break;
            }

            Vector3 rest = useRightArm ? rightRestLocalPos : leftRestLocalPos;
            float sideSign = useRightArm ? -1f : 1f;

            Vector3 windUpPos = rest + Vector3.back * windUpBackDistance + Vector3.right * sideSign * inwardDistance * 0.5f;
            Vector3 strikePos = rest + Vector3.forward * forwardDistance + Vector3.right * sideSign * inwardDistance + Vector3.up * upwardDistance;
            strikePos.x = Mathf.Lerp(strikePos.x, centerlineX, Mathf.Clamp01(centerBias));

            Quaternion spineStart = spine != null ? spine.localRotation : Quaternion.identity;
            Quaternion spinePunchOffset = Quaternion.Euler(-spinePitch, -sideSign * spineYaw, 0f);

            yield return MoveTarget(activeTarget, rest, windUpPos, windUpDuration, windUpCurve, spineStart, Quaternion.identity);
            yield return MoveTarget(activeTarget, windUpPos, strikePos, strikeDuration, strikeCurve, spineStart, spinePunchOffset, strikeArcHeight);
            yield return MoveTarget(activeTarget, strikePos, rest, recoverDuration, recoverCurve, spineStart, Quaternion.identity);

            activeTarget.localPosition = rest;
            if (spine != null)
            {
                spine.localRotation = spineRestLocalRot;
            }

            punchRunning = false;
        }

        private IEnumerator MoveTarget(
            Transform target,
            Vector3 start,
            Vector3 end,
            float duration,
            AnimationCurve curve,
            Quaternion spineStart,
            Quaternion spineOffset,
            float arcHeight = 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = curve != null ? curve.Evaluate(t) : t;

                Vector3 nextPosition = Vector3.LerpUnclamped(start, end, curvedT);
                if (arcHeight > 0f)
                {
                    nextPosition.y += Mathf.Sin(curvedT * Mathf.PI) * arcHeight;
                }

                target.localPosition = nextPosition;

                if (spine != null)
                {
                    spine.localRotation = Quaternion.SlerpUnclamped(spineStart, spineStart * spineOffset, curvedT);
                }

                yield return null;
            }

            target.localPosition = end;
            if (spine != null)
            {
                spine.localRotation = spineStart * spineOffset;
            }
        }

        private void OnDisable()
        {
            if (leftHandTarget != null)
            {
                leftHandTarget.localPosition = leftRestLocalPos;
            }

            if (rightHandTarget != null)
            {
                rightHandTarget.localPosition = rightRestLocalPos;
            }

            if (spine != null)
            {
                spine.localRotation = spineRestLocalRot;
            }

            punchRunning = false;
        }
    }
}
