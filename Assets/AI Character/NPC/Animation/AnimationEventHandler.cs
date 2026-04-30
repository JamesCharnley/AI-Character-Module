using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AICharacterModule.NPC.StateMachine.Core;
using Random = UnityEngine.Random;

namespace AICharacterModule.NPC.Animation
{
	public class AnimationEventHandler : MonoBehaviour
	{

		[SerializeField] private Animator anim;
		[SerializeField] private NPCBehaviourController npcController;
		
		public event Action chaseAnimationCycleEndingEvent;
		
		[SerializeField] private float damageOverlapRadius = 0.75f;
		[SerializeField] private float damageAmount = 20f;
		[SerializeField] private LayerMask damageOverlapLayers = ~0;
		[SerializeField] private Vector3 defaultDamageOverlapOffset = new(0f, 1f, 1f);
		[SerializeField] private bool showDamageOverlapDebugSphere = true;
		[SerializeField] private Color damageOverlapDebugColor = new(1f, 0.2f, 0.2f, 0.75f);
		
		[SerializeField] private AudioClip[] HeavyPunchWooshSounds;
        [SerializeField] private AudioClip[] LightPunchWooshSounds;
        [SerializeField] private AudioClip[] HeavyKickWooshSounds;
        [SerializeField] private AudioClip[] LightKickWooshSounds;

        public void PlayHeavyPunchWoosh(string _isLeftHand)
        {
            int maxEx = HeavyPunchWooshSounds.Length;
            AudioClip[] clips = HeavyPunchWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftHandDamageBone.GetComponent<AudioSource>()
                : RightHandDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();

        }
        public void PlayLightPunchWoosh(string _isLeftHand)
        {
            int maxEx = LightPunchWooshSounds.Length;
            AudioClip[] clips = LightPunchWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftHandDamageBone.GetComponent<AudioSource>()
                : RightHandDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();

        }

        public void PlayHeavyKickWoosh(string _isLeftHand)
        {
            int maxEx = HeavyKickWooshSounds.Length;
            AudioClip[] clips = HeavyKickWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftFootDamageBone.GetComponent<AudioSource>()
                : RightFootDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();
        }
        public void PlayLightKickWoosh(string _isLeftHand)
        {
            int maxEx = HeavyKickWooshSounds.Length;
            AudioClip[] clips = HeavyKickWooshSounds;
            int rand = Random.Range(0, maxEx);
            AudioSource source = _isLeftHand == "true"
                ? LeftFootDamageBone.GetComponent<AudioSource>()
                : RightFootDamageBone.GetComponent<AudioSource>();
            if (source == null)
            {
                return;
            }

            source.clip = clips[rand];
            source.Play();
        }
		
		private void Update()
		{
			DoDamageCheck();
		}
		
		public void AttackAnimationCompleted()
		{
			if (npcController.GetMasterStateMachine == null)
			{
				Debug.Log("AttackAnimationCompleted failed");
				return;
			}
			Debug.Log("AttackAnimationCompleted");
			npcController.GetMasterStateMachine.GlobalData.IsAttacking = false;
		}

		public void DodgeAnimationCompleted()
		{
			npcController.GetMasterStateMachine.GlobalData.IsDodging = false;
		}

		public void ChaseAnimationCycleEnding()
		{
			chaseAnimationCycleEndingEvent?.Invoke();
		}
		
		[SerializeField] private Transform RightHandDamageBone;
		[SerializeField] private Transform LeftHandDamageBone;
		[SerializeField] private Transform LeftFootDamageBone;
		[SerializeField] private Transform RightFootDamageBone;
        
		private bool LeftHandDamageActive = false;
		private bool RightHandDamageActive = false;
		private bool LeftFootDamageActive = false;
		private bool RightFootDamageActive = false;

		void DoDamageCheck()
		{
			if (LeftHandDamageActive)
			{
				bool didDamage = DoDamageOverlap(LeftHandDamageBone);
				if (didDamage) LeftHandDamageActive = false;
			}
			if (RightHandDamageActive)
			{
				bool didDamage = DoDamageOverlap(RightHandDamageBone);
				if (didDamage) RightHandDamageActive = false;
			}
			if (LeftFootDamageActive)
			{
				bool didDamage = DoDamageOverlap(LeftFootDamageBone);
				if (didDamage) LeftFootDamageActive = false;
			}
			if (RightHandDamageActive)
			{
				bool didDamage = DoDamageOverlap(RightFootDamageBone);
				if (didDamage) RightHandDamageActive = false;
			}
		}
		
		public bool DoDamageOverlap(Transform _originTransform)
		{
            
			Collider[] hitColliders = Physics.OverlapSphere(_originTransform.position, damageOverlapRadius, damageOverlapLayers);
			foreach (Collider hitCollider in hitColliders)
			{
				if (hitCollider.transform == transform)
				{
					continue;
				}

				ITakeDamage takeDamage = hitCollider.GetComponentInParent<ITakeDamage>();
				if (takeDamage == null)
				{
					continue;
				}
				takeDamage.TakeDamage(npcController.GetMasterStateMachine.GlobalData.Anim.GetFloat("CurrentDamage"), transform.forward * npcController.GetMasterStateMachine.GlobalData.Anim.GetFloat("CurrentDamageForce"), Vector3.zero);
				return true;
			}

			return false;
		}
		
		public void DoDamageOverlapActivate(string _damagePointId)
		{
			switch (_damagePointId)
			{
				case "LeftHand":
					LeftHandDamageActive = true;
					break;
				case "RightHand":
					RightHandDamageActive = true;
					break;
				case "LeftFoot":
					LeftFootDamageActive = true;
					break;
				case "RightFoot":
					RightFootDamageActive = true;
					break;
				default:
					RightHandDamageActive = true;
					break;
			}
		}
		public void DoDamageOverlapDeactivate(string _damagePointId)
		{
			switch (_damagePointId)
			{
				case "LeftHand":
					LeftHandDamageActive = false;
					break;
				case "RightHand":
					RightHandDamageActive = false;
					break;
				case "LeftFoot":
					LeftFootDamageActive = false;
					break;
				case "RightFoot":
					RightFootDamageActive = false;
					break;
				default:
					RightHandDamageActive = false;
					break;
			}
		}
		
		private void OnDrawGizmosSelected()
		{
			if (!showDamageOverlapDebugSphere)
			{
				return;
			}
			if (LeftHandDamageActive)
			{
				Gizmos.color = damageOverlapDebugColor;
				Gizmos.DrawWireSphere(LeftHandDamageBone.position, damageOverlapRadius);
			}
			if (RightHandDamageActive)
			{
				Gizmos.color = damageOverlapDebugColor;
				Gizmos.DrawWireSphere(RightHandDamageBone.position, damageOverlapRadius);
			}
			if (LeftFootDamageActive)
			{
				Gizmos.color = damageOverlapDebugColor;
				Gizmos.DrawWireSphere(LeftFootDamageBone.position, damageOverlapRadius);
			}
			if (RightHandDamageActive)
			{
				Gizmos.color = damageOverlapDebugColor;
				Gizmos.DrawWireSphere(RightFootDamageBone.position, damageOverlapRadius);
			}

            
		}
		
		public void PlayDodgeAnimation(Vector3 _offset)
		{
			if (_offset.y == 2 && _offset.x == 0)
			{
				// duck
				anim.SetTrigger("DodgePunchDown");
				return;
			}
			if ((_offset.y == -1 && _offset.x == 0) || (_offset.y == 1 && _offset.x == 0))
			{
				// back
				anim.SetTrigger("DodgePunchBack");
				return;
			}
			if (_offset.z == -1)
			{
				// back
				anim.SetTrigger("DodgePunchBack");
				return;
			}
			if (_offset.x == -1)
			{
				// left
				anim.SetTrigger("DodgePunchLeft");
				return;
			}
			if (_offset.x == 1)
			{
				// right
				anim.SetTrigger("DodgePunchRight");
				return;
			}
		}

		public void PlayDamageAnimation(float _amount, Vector3 _direction, HitZoneInfo _hitZoneInfo)
		{
			Vector3 offset = _hitZoneInfo.LocalOffset;
            Debug.LogError($"TakeDamage: {offset}");

            // Expected axis mapping:
            // X: +1 left, -1 right
            // Y: +1 above, -1 below
            // Z: +1 front, -1 back

            if (offset.z == -1f)
            {
                if (offset.y == 1f)
                {
                    anim.SetTrigger("TorsoeDamageHigh_Back");
                    PlayHighBodyHit(_hitZoneInfo);
                    return;
                }

                if (offset.y == -1f)
                {
                    anim.SetTrigger("TorsoeDamageLow_Back");
                    PlayLowBodyHit(_hitZoneInfo);
                    return;
                }
            }

            if (offset == new Vector3(0, 1, 1))
            {
                // high front
                anim.SetTrigger("TorsoeDamageHigh_Front");
                PlayHighBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(1, 1, 1))
            {
                // High left
                anim.SetTrigger("TorsoeDamageHigh_Left");
                PlayHighBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(-1, 1, 1))
            {
                // High right
                anim.SetTrigger("TorsoeDamageHigh_Right");
                PlayHighBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(1, -1, 1))
            {
                // Low left
                anim.SetTrigger("TorsoeDamageLow_Left");
                PlayLowBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(-1, -1, 1))
            {
                // Low right
                anim.SetTrigger("TorsoeDamageLow_Right");
                PlayLowBodyHit(_hitZoneInfo);
                return;
            }
            if (offset == new Vector3(0, -1, 1))
            {
                // Low front
                anim.SetTrigger("TorsoeDamageLow_Front");
                PlayLowBodyHit(_hitZoneInfo);
                return;
            }
		}
		[SerializeField] private AudioClip[] HighBodyHitSounds;
		[SerializeField] private AudioClip[] LowBodyHitSounds;
		[SerializeField] private AudioClip[] FaceHitSounds;
		void PlayLowBodyHit(HitZoneInfo _hitZone)
		{
			if (!_hitZone.SelfTransform.TryGetComponent(out AudioSource source))
			{
				return;
			}
			int rand = Random.Range(0, LowBodyHitSounds.Length);
			source.clip = LowBodyHitSounds[rand];
			source.Play();

		}
		void PlayHighBodyHit(HitZoneInfo _hitZone)
		{
			if (!_hitZone.SelfTransform.TryGetComponent(out AudioSource source))
			{
				return;
			}
			int rand = Random.Range(0, HighBodyHitSounds.Length);
			source.clip = HighBodyHitSounds[rand];
			source.Play();
		}
	}
}

