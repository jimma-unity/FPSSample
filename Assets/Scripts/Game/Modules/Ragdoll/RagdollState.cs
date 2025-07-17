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

[RequireComponent(typeof(GameObjectEntity))]
public class RagdollState : MonoBehaviour
{
    private void OnEnable()
    {
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponentData(goe.Entity, new RagdollStateData());
    }

    private void OnDisable()
    {
        var goe = GetComponent<GameObjectEntity>();
        if ((goe.Entity != Entity.Null) && goe.EntityManager.HasComponent<RagdollStateData>(goe.Entity))
            goe.EntityManager.RemoveComponent<RagdollStateData>(goe.Entity);
    }
}
