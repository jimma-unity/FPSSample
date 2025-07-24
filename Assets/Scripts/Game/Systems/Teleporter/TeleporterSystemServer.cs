using Unity.Collections;
using Unity.Entities;

[DisableAutoCreation]
public partial class TeleporterSystemServer : SystemBase
{

    public TeleporterSystemServer(GameWorld gameWorld)
    {
        m_GameWorld = gameWorld;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TeleporterServerGroup = GetEntityQuery(typeof(TeleporterServer), typeof(TeleporterPresentationData));
    }

    protected override void OnUpdate()
    {
        var teleporters = m_TeleporterServerGroup.ToComponentArray<TeleporterServer>();
        var presentationArray = m_TeleporterServerGroup.ToComponentDataArray<TeleporterPresentationData>(Allocator.TempJob);
        var entities = m_TeleporterServerGroup.ToEntityArray(Allocator.TempJob);
        for (int i = 0, c = teleporters.Length; i < c; i++)
        {
            var t = teleporters[i];

            if (t.characterInside != null)
            {
                

                if (t.characterInside.owner != Entity.Null && EntityManager.HasComponent<Character>(t.characterInside.owner))
                {
                    var character = EntityManager.GetComponentObject<Character>(t.characterInside.owner);    
                    
                    var dstPos = t.targetTeleporter.GetSpawnPositionWorld();
                    var dstRot = t.targetTeleporter.GetSpawnRotationWorld();

                    character.TeleportTo(dstPos, dstRot);

                    var presentation = presentationArray[i];
                    presentation.effectTick = m_GameWorld.worldTime.tick;
                    EntityManager.SetComponentData(entities[i],presentation);
                }
                t.characterInside = null;

            }
        }
        
        entities.Dispose();
        presentationArray.Dispose();
    }

    GameWorld m_GameWorld;
    private EntityQuery m_TeleporterServerGroup;
}
