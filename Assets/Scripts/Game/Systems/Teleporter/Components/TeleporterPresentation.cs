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
public class TeleporterPresentation : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Convert the MonoBehaviour data into the ECS component
        dstManager.AddComponentData(entity, new TeleporterPresentationData
        {
        });
    }
}
