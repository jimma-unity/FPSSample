using System;
using Unity.Entities;
using UnityEngine;


[System.Serializable]
public struct TeleporterPresentationData : IComponentData, IReplicatedComponent
{
    [NonSerialized] public int effectTick;
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<TeleporterPresentationData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteInt32("effectTick", effectTick);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        effectTick = reader.ReadInt32();
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(GameObjectEntity))]
public class TeleporterPresentation : MonoBehaviour
{
    private void OnEnable()
    {
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponentData(goe.Entity, new TeleporterPresentationData());
    }

    private void OnDisable()
    {
        var goe = GetComponent<GameObjectEntity>();
        if ((goe.Entity != Entity.Null) && goe.EntityManager.HasComponent<TeleporterPresentationData>(goe.Entity))
            goe.EntityManager.RemoveComponent<TeleporterPresentationData>(goe.Entity);
    }
}
