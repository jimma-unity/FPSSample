using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Flags]
public enum HitCollisionFlags
{
    TeamA = 1 << 0,
    TeamB = 1 << 1,
}


[Serializable]
public struct HitCollisionOwnerData : IComponentData
{
    [EnumBitField(typeof(HitCollisionFlags))] 
    public uint colliderFlags;

    public int collisionEnabled;
}

[RequireComponent(typeof(GameObjectEntity))]
[DisallowMultipleComponent]
public class HitCollisionOwner : MonoBehaviour
{
    private void OnEnable()
    {
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponentData(goe.Entity, new HitCollisionOwnerData());
        
        // Make sure damage event buffer is created
        // TODO (mogensh) create DamageEvent buffer using monobehavior wrapper (when it is available)
        if (goe != null && goe.EntityManager != null)
        {
            goe.EntityManager.AddBuffer<DamageEvent>(goe.Entity);
        }
    }

    private void OnDisable()
    {
        var goe = GetComponent<GameObjectEntity>();
        if ((goe.Entity != Entity.Null) && goe.EntityManager.HasComponent<HitCollisionOwnerData>(goe.Entity))
            goe.EntityManager.RemoveComponent<HitCollisionOwnerData>(goe.Entity);
    }
}
