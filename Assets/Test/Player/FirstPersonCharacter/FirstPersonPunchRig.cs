using System.Collections;
using System.Collections.Generic;
using AICharacterModule.NPC.StateMachine.Core;
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
        [SerializeField] private Transform leftHandBone;
        [SerializeField] private Transform rightHandBone;

        [Header("Input")]
        [SerializeField] private int mouseButton = 0;

        [Header("Punch Timing")]
        [Min(0.01f)] [SerializeField] private float minWindUpDuration = 0.06f;
        [Min(0.01f)] [SerializeField] private float maxWindUpDuration = 0.2f;
        [Min(0.01f)] [SerializeField] private float minStrikeDuration = 0.05f;
        [Min(0.01f)] [SerializeField] private float maxStrikeDuration = 0.1f;
        [Min(0.01f)] [SerializeField] private float recoverDuration = 0.12f;
        [Min(0f)] [SerializeField] private float punchCooldown = 0.03f;
        [Range(0f, 1f)] [SerializeField] private float punchDamageWindowNormalized = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float impulseForwardTimingNormalized = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float punchWooshTimingNormalized = 0.25f;

        [Header("Punch Shape (Local Space)")]
        [SerializeField] private float forwardDistance = 0.33f;
        [SerializeField] private float inwardDistance = 0.05f;
        [SerializeField] private float upwardDistance = 0.02f;
        [SerializeField] private float windUpBackDistance = 0.08f;
        [SerializeField] private float centerlineX = 0f;
        [Range(0f, 1f)] [SerializeField] private float centerBias = 0.7f;
        [SerializeField] private float strikeArcHeight = 0.045f;
        [Range(0f, 1f)] [SerializeField] private float minimumForwardTravelRatio = 0.7f;
        [SerializeField] private bool trackHitZoneDuringStrike = true;

        [Header("Spine Motion")]
        [SerializeField] private float spinePitch = 8f;
        [SerializeField] private float spineYaw = 4f;

        [Header("Punch Damage")]
        [SerializeField] private float punchDamageAmount = 20f;
        [Min(0.01f)] [SerializeField] private float punchOverlapSphereRadius = 0.2f;
        [SerializeField] private LayerMask enemyLayerMask;
        [SerializeField] private bool showPunchOverlapDebugSpheres = true;
        [SerializeField] private Color punchOverlapDebugColor = new(1f, 0.2f, 0.2f, 0.75f);

        [Header("Punch Audio")]
        [SerializeField] private AudioSource leftHandAudioSource;
        [SerializeField] private AudioSource rightHandAudioSource;
        [SerializeField] private AudioClip[] punchWooshSounds;

        [Header("Curves")]
        [SerializeField] private AnimationCurve windUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve strikeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve recoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 leftRestLocalPos;
        private Vector3 rightRestLocalPos;
        private Quaternion spineRestLocalRot;
        private bool punchRunning;
        private bool punchCharging;
        private float punchChargeStartTime;
        private bool punchRightNext = true;
        private float lastPunchTime = -10f;
        private bool punchDamageResolved;
        
        [SerializeField] private LayerMask CamRaycastMask;
        [SerializeField] private float PunchRaycastDistance = 1;
        [SerializeField] private Camera cam;

        [SerializeField] private PlayerController playerController;

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

        [SerializeField] private HitZoneInfo CurrentHitZone;
        private void Update()
        {
            if (punchRunning)
            {
                return;
            }

            if (Input.GetMouseButtonDown(mouseButton))
            {
                TryStartPunchCharge();
            }

            if (!punchCharging)
            {
                return;
            }

            float maxWindUp = GetMaxWindUpDuration();
            if (maxWindUp > 0f && Time.time - punchChargeStartTime >= maxWindUp)
            {
                LaunchPunch(1f);
                return;
            }

            if (Input.GetMouseButtonUp(mouseButton))
            {
                LaunchPunch(GetCurrentChargeRatio());
            }
        }

        private void TryStartPunchCharge()
        {
            if (Time.time < lastPunchTime + punchCooldown)
            {
                return;
            }

            punchCharging = true;
            punchChargeStartTime = Time.time;
        }

        private float GetCurrentChargeRatio()
        {
            float maxWindUp = GetMaxWindUpDuration();
            if (maxWindUp <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01((Time.time - punchChargeStartTime) / maxWindUp);
        }

        private float GetMaxWindUpDuration()
        {
            return Mathf.Max(minWindUpDuration, maxWindUpDuration);
        }

        private void LaunchPunch(float chargeRatio)
        {
            if (!punchCharging || punchRunning)
            {
                return;
            }

            CurrentHitZone = GetCurrentHitZone();
            punchCharging = false;
            float clampedChargeRatio = Mathf.Clamp01(chargeRatio);
            float windUpDuration = Mathf.Lerp(minWindUpDuration, GetMaxWindUpDuration(), clampedChargeRatio);
            float strikeDuration = Mathf.Lerp(Mathf.Max(maxStrikeDuration, minStrikeDuration), minStrikeDuration, clampedChargeRatio);
            StartCoroutine(PunchRoutine(punchRightNext, windUpDuration, strikeDuration));
            punchRightNext = !punchRightNext;
            lastPunchTime = Time.time;
        }

        private HitZoneInfo GetCurrentHitZone()
        {
            HitZoneInfo selectedHitZone = new HitZoneInfo();
            Vector3 overlapPos = transform.position + Vector3.up + transform.forward * 0.5f;
            Collider[] cols = Physics.OverlapSphere(overlapPos, 1,
                enemyLayerMask,
                QueryTriggerInteraction.Collide);
            foreach (Collider col in cols)
            {
                if (col.transform.root.TryGetComponent(out IHasHitZones hitzones))
                {
                    Vector3 adjustedCamPosition = cam.transform.position + cam.transform.forward;
                    selectedHitZone = hitzones.GetClosestHitzoneTransform(adjustedCamPosition);
                }
            }

            return selectedHitZone;
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

        [SerializeField] private float punchMobileForce = 0.3f;

        void ImpulseForward()
        {
            playerController.AddImpulse(transform.forward * punchMobileForce);
        }

        private IEnumerator PunchRoutine(bool useRightArm, float windUpDuration, float strikeDuration)
        {
            punchRunning = true;

            Transform activeTarget = useRightArm ? rightHandTarget : leftHandTarget;
            if (activeTarget == null)
            {
                punchRunning = false;
                punchDamageResolved = false;
                Debug.LogError("Punch Active Target Null");
                yield break;
            }

            Vector3 rest = useRightArm ? rightRestLocalPos : leftRestLocalPos;
            float sideSign = useRightArm ? -1f : 1f;

            Vector3 windUpPos = rest + Vector3.back * windUpBackDistance + Vector3.right * sideSign * inwardDistance * 0.5f;
            Vector3 defaultStrikePos = rest + Vector3.forward * forwardDistance + Vector3.right * sideSign * inwardDistance + Vector3.up * upwardDistance;
            defaultStrikePos.x = Mathf.Lerp(defaultStrikePos.x, centerlineX, Mathf.Clamp01(centerBias));
            Vector3 strikePos = GetStrikePositionFromCurrentHitZone(rest, defaultStrikePos);
            Transform activeHandBone = useRightArm ? rightHandBone : leftHandBone;
            if (activeHandBone == null)
            {
                activeHandBone = activeTarget;
            }
            HashSet<ITakeDamage> damagedTargets = new HashSet<ITakeDamage>();
            punchDamageResolved = false;

            Quaternion spineStart = spine != null ? spine.localRotation : Quaternion.identity;
            Quaternion spinePunchOffset = Quaternion.Euler(-spinePitch, -sideSign * spineYaw, 0f);
            float totalPunchDuration = windUpDuration + strikeDuration + recoverDuration;
            float impulseDelay = totalPunchDuration * Mathf.Clamp01(impulseForwardTimingNormalized);
            float punchWooshDelay = totalPunchDuration * Mathf.Clamp01(punchWooshTimingNormalized);
            StartCoroutine(ImpulseForwardRoutine(impulseDelay));
            StartCoroutine(PlayPunchWooshRoutine(punchWooshDelay, useRightArm));

            yield return MoveTarget(activeTarget, rest, windUpPos, windUpDuration, windUpCurve, spineStart, Quaternion.identity);
            yield return MoveTarget(activeTarget, windUpPos, strikePos, strikeDuration, strikeCurve, spineStart, spinePunchOffset, strikeArcHeight, activeHandBone, damagedTargets, trackHitZoneDuringStrike, rest, defaultStrikePos);
            Vector3 recoverStart = activeTarget.localPosition;
            yield return MoveTarget(activeTarget, recoverStart, rest, recoverDuration, recoverCurve, spineStart, Quaternion.identity);
            Debug.DrawLine(transform.TransformPoint(strikePos), transform.TransformPoint(strikePos) + Vector3.up, Color.green, 3);
            activeTarget.localPosition = rest;
            if (spine != null)
            {
                spine.localRotation = spineRestLocalRot;
            }

            punchRunning = false;
            punchDamageResolved = false;
        }

        private IEnumerator ImpulseForwardRoutine(float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            if (punchRunning)
            {
                ImpulseForward();
            }
        }

        private IEnumerator PlayPunchWooshRoutine(float delaySeconds, bool isRightHand)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            if (punchRunning)
            {
                PlayPunchWoosh(isRightHand);
            }
        }

        private void PlayPunchWoosh(bool isRightHand)
        {
            if (punchWooshSounds == null || punchWooshSounds.Length == 0)
            {
                return;
            }

            AudioSource handAudioSource = isRightHand ? rightHandAudioSource : leftHandAudioSource;
            if (handAudioSource == null)
            {
                return;
            }

            AudioClip randomWooshClip = punchWooshSounds[Random.Range(0, punchWooshSounds.Length)];
            if (randomWooshClip == null)
            {
                return;
            }

            handAudioSource.PlayOneShot(randomWooshClip);
        }

        private IEnumerator MoveTarget(
            Transform target,
            Vector3 start,
            Vector3 end,
            float duration,
            AnimationCurve curve,
            Quaternion spineStart,
            Quaternion spineOffset,
            float arcHeight = 0f,
            Transform activeHandBone = null,
            HashSet<ITakeDamage> damagedTargets = null,
            bool trackHitZone = false,
            Vector3 restPosition = default,
            Vector3 defaultStrikePosition = default)
        {
            
            
            float elapsed = 0f;
            Vector3 finalEnd = end;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = curve != null ? curve.Evaluate(t) : t;
                Vector3 dynamicEnd = trackHitZone ? GetStrikePositionFromCurrentHitZone(restPosition, defaultStrikePosition) : end;
                finalEnd = dynamicEnd;

                Vector3 nextPosition = Vector3.LerpUnclamped(start, dynamicEnd, curvedT);
                if (arcHeight > 0f)
                {
                    nextPosition.y += Mathf.Sin(curvedT * Mathf.PI) * arcHeight;
                }

                target.localPosition = nextPosition;

                if (spine != null)
                {
                    spine.localRotation = Quaternion.SlerpUnclamped(spineStart, spineStart * spineOffset, curvedT);
                }

                if (activeHandBone != null && !punchDamageResolved && ShouldApplyPunchDamageWindow(t))
                {
                    DoPunchOverlapDamage(activeHandBone, damagedTargets);
                }

                yield return null;
            }

            target.localPosition = finalEnd;
            if (spine != null)
            {
                spine.localRotation = spineStart * spineOffset;
            }

            if (activeHandBone != null && !punchDamageResolved && ShouldApplyPunchDamageWindow(1f))
            {
                DoPunchOverlapDamage(activeHandBone, damagedTargets);
            }
        }

        private Vector3 GetStrikePositionFromCurrentHitZone(Vector3 restPosition, Vector3 defaultStrikePosition)
        {
            if (CurrentHitZone.SelfTransform == null)
            {
                return defaultStrikePosition;
            }

            Vector3 zonePositionWorld = CurrentHitZone.SelfTransform.TransformPoint(CurrentHitZone.LocalOffset);
            Vector3 zonePositionLocal = transform.InverseTransformPoint(zonePositionWorld);

            Vector3 desiredDelta = zonePositionLocal - restPosition;
            Vector3 maxDelta = defaultStrikePosition - restPosition;

            float clampedX = maxDelta.x >= 0f
                ? Mathf.Clamp(desiredDelta.x, 0f, maxDelta.x)
                : Mathf.Clamp(desiredDelta.x, maxDelta.x, 0f);
            float clampedY = Mathf.Clamp(desiredDelta.y, 0f, upwardDistance);
            float clampedZ = Mathf.Clamp(desiredDelta.z, 0f, forwardDistance);
            float minForwardDistance = forwardDistance * Mathf.Clamp01(minimumForwardTravelRatio);
            clampedZ = Mathf.Max(clampedZ, minForwardDistance);

            return restPosition + new Vector3(clampedX, clampedY, clampedZ);
        }

        private bool ShouldApplyPunchDamageWindow(float normalizedProgress)
        {
            float checkStart = 1f - Mathf.Clamp01(punchDamageWindowNormalized);
            return normalizedProgress >= checkStart;
        }

        private void DoPunchOverlapDamage(Transform handBone, HashSet<ITakeDamage> damagedTargets)
        {
            
            Collider[] hitColliders = Physics.OverlapSphere(handBone.position + transform.forward * 0.2f, punchOverlapSphereRadius, enemyLayerMask, QueryTriggerInteraction.Collide);
            Debug.LogWarning($"DoPunchOverlapDamage {hitColliders.Length}");
            foreach (Collider hitCollider in hitColliders)
            {
                //ITakeDamage damageReceiver = hitCollider.GetComponentInParent<ITakeDamage>();
                ITakeDamage damageReceiver = hitCollider.transform.root.GetComponent<ITakeDamage>();
                if (damageReceiver == null)
                {
                    Debug.LogWarning("DamageReciever null");
                    continue;
                }

                if (damagedTargets != null && !damagedTargets.Add(damageReceiver))
                {
                    Debug.LogWarning("other continue");
                    continue;
                }

                Vector3 handPosition = handBone.position;
                Vector2 handOffset = new Vector2(handPosition.x, handPosition.y);
                damageReceiver.TakeDamage(punchDamageAmount, transform.forward, CurrentHitZone);
                punchDamageResolved = true;
                return;
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
            punchCharging = false;
            punchDamageResolved = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showPunchOverlapDebugSpheres)
            {
                return;
            }

            Gizmos.color = punchOverlapDebugColor;
            DrawPunchOverlapDebugSphere(leftHandBone, leftHandTarget);
            DrawPunchOverlapDebugSphere(rightHandBone, rightHandTarget);
        }

        private void DrawPunchOverlapDebugSphere(Transform handBone, Transform handTarget)
        {
            Transform debugSource = handBone != null ? handBone : handTarget;
            
            if (debugSource == null)
            {
                return;
            }
            

            Gizmos.DrawWireSphere(debugSource.position + transform.forward * 0.2f, punchOverlapSphereRadius);
        }
    }
}
