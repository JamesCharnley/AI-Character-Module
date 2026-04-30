using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct IncomingAttackData
{
    public EAttackType Type;
    public HitZoneInfo HitZoneData;
    public float TimeStamp;
}

public enum EAttackType
{
    None,
    Melee,
}
public interface ICombat
{
    void NotifyIncomingAttack(IncomingAttackData _data);

}
