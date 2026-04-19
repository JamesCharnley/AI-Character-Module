using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HitZoneInfo
{
	public Transform SelfTransform;
	public Vector3 LocalOffset;
}
public interface IHasHitZones
{
	public List<HitZoneInfo> HitZones { get; set; }
	public HitZoneInfo GetClosestHitzoneTransform(Vector3 _RelativeTo);
}
