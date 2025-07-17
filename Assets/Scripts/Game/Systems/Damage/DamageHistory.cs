using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct DamageHistoryData : IComponentData, IReplicatedComponent             
{
    [Serializable]
    public struct InflictedDamage
    {
        public int tick;
        public int lethal;

        public void Serialize(ref NetworkWriter writer)
        {
            writer.WriteInt32("tick", tick);
            writer.WriteBoolean("lethal", lethal == 1);
        }

        public void Deserialize(ref NetworkReader reader, int tick)
        {
            this.tick = reader.ReadInt32();
            lethal = reader.ReadBoolean() ? 1 : 0;
        }
    }

    [NonSerialized] public InflictedDamage inflictedDamage;

    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<DamageHistoryData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        inflictedDamage.Serialize(ref writer);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        inflictedDamage.Deserialize(ref reader, context.tick);
    }
}

[RequireComponent(typeof(GameObjectEntity))]
public class DamageHistory : MonoBehaviour
{
    private void OnEnable()
    {
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponentData(goe.Entity, new DamageHistoryData());
    }

    private void OnDisable()
    {
        var goe = GetComponent<GameObjectEntity>();
        if ((goe.Entity != Entity.Null) && goe.EntityManager.HasComponent<DamageHistoryData>(goe.Entity))
            goe.EntityManager.RemoveComponent<DamageHistoryData>(goe.Entity);
    }
}

