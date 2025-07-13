using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct RagdollStateData : IComponentData, IReplicatedComponent
{
    [NonSerialized] public int ragdollActive;
    [NonSerialized] public Vector3 impulse;
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<RagdollStateData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteBoolean("ragdollEnabled",ragdollActive == 1);
        writer.WriteVector3Q("impulse",impulse,1);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        ragdollActive = reader.ReadBoolean() ? 1 : 0;
        impulse = reader.ReadVector3Q();
    }
}

public class RagdollState : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Convert the MonoBehaviour data into the ECS component
        dstManager.AddComponentData(entity, new RagdollStateData
        {
        });
    }
}
