using System;
using System.Collections;
using System.Collections.Generic;
using AICharacterModule.NPC.Animation;
using AICharacterModule.NPC.StateMachine.Core;
using UnityEngine;

public class NpcCharacter : MonoBehaviour, ITakeDamage, IHasHitZones, ICombat
{
    [SerializeField] private float MaxHealth = 100;
    private float CurrentHealth = 100;
    
    [SerializeField] private HitZoneInfo[] HitZonesCache;

    [SerializeField] private AnimationEventHandler animationController;
    private void Start()
    {
        HitZones = new();
        foreach (HitZoneInfo zoneInfo in HitZonesCache)
        {
            HitZones.Add(zoneInfo);
        }
    }
    
    public void TakeDamage(float _amount)
    {
        throw new NotImplementedException();
    }

    
    public void TakeDamage(float _amount, Vector3 _direction, Vector3 _damagerPos)
    {
        throw new NotImplementedException();
    }

    public void TakeDamage(float _amount, Vector3 _direction, HitZoneInfo _hitZoneInfo)
    {
        CurrentHealth -= _amount;
        if (CurrentHealth <= 0)
        {
            Debug.Log("NPC DEAD");
            CurrentHealth = MaxHealth;
            return;
        }
        animationController.PlayDamageAnimation(_amount, _direction, _hitZoneInfo);
    }

    
    public List<HitZoneInfo> HitZones { get; set; }
    public HitZoneInfo GetClosestHitzoneTransform(Vector3 _RelativeTo)
    {
        HitZoneInfo closestHitZone = new();
        float shortestDist = 999;
        foreach (HitZoneInfo hitZone in HitZones)
        {
            float dist = Vector3.Distance(hitZone.SelfTransform.position, _RelativeTo);
            if (dist < shortestDist)
            {
                shortestDist = dist;
                closestHitZone = hitZone;
            }
        }

        return closestHitZone;
    }

    private IncomingAttackData CurrentIncomingAttack;
    public IncomingAttackData GetCurrentIncomingAttack => CurrentIncomingAttack;
    public void NotifyIncomingAttack(IncomingAttackData _data)
    {
        CurrentIncomingAttack = _data;
        StartCoroutine(IncomingAttackExpire(_data));
    }

    IEnumerator IncomingAttackExpire(IncomingAttackData _data)
    {
        yield return new WaitForSeconds(0.5f);
        if (Math.Abs(_data.TimeStamp - CurrentIncomingAttack.TimeStamp) < 0.05f)
        {
            CurrentIncomingAttack = new();
        }
    }
}

