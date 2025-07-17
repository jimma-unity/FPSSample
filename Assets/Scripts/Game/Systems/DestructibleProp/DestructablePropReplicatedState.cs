using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct DestructablePropReplicatedData : IComponentData, IReplicatedComponent
{
    public int destroyedTick;      

    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<DestructablePropReplicatedData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteInt32("destroyed",destroyedTick);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        destroyedTick = reader.ReadInt32();
    }
}

[RequireComponent(typeof(GameObjectEntity))]
public class DestructablePropReplicatedState : MonoBehaviour
{
    private void OnEnable()
    {
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponentData(goe.Entity, new DestructablePropReplicatedData());
    }

    private void OnDisable()
    {
        var goe = GetComponent<GameObjectEntity>();
        if ((goe.Entity != Entity.Null) && goe.EntityManager.HasComponent<DestructablePropReplicatedData>(goe.Entity))
            goe.EntityManager.RemoveComponent<DestructablePropReplicatedData>(goe.Entity);
    }
}