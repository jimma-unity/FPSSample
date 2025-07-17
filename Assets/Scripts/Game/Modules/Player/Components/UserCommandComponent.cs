using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct UserCommandComponentData: IComponentData, IReplicatedComponent    
{   
    [NonSerialized] public UserCommand command;
    
    [NonSerialized] public int resetCommandTick;
    [NonSerialized] public float resetCommandLookYaw;          
    [NonSerialized] public float resetCommandLookPitch; // = 90;
    [NonSerialized] public int lastResetCommandTick;

    public void ResetCommand(int tick, float lookYaw, float lookPitch)
    {
        resetCommandTick = tick;
        resetCommandLookYaw = lookYaw;
        resetCommandLookPitch = lookPitch;
    }
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<UserCommandComponentData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteInt32("resetCamTick", resetCommandTick);
        writer.WriteFloatQ("lookYaw", resetCommandLookYaw, 1);
        writer.WriteFloatQ("lookPitch", resetCommandLookPitch, 1);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        resetCommandTick = reader.ReadInt32();
        resetCommandLookYaw = reader.ReadFloatQ();
        resetCommandLookPitch = reader.ReadFloatQ();
    }
}

[RequireComponent(typeof(GameObjectEntity))]
public class UserCommandComponent : MonoBehaviour
{
    private void OnEnable()
    {
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponentData(goe.Entity, new UserCommandComponentData());
    }

    private void OnDisable()
    {
        var goe = GetComponent<GameObjectEntity>();
        if ((goe.Entity != Entity.Null) && goe.EntityManager.HasComponent<UserCommandComponentData>(goe.Entity))
            goe.EntityManager.RemoveComponent<UserCommandComponentData>(goe.Entity);
    }
}