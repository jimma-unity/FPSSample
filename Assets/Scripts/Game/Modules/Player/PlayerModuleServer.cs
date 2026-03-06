using Unity.Entities;
using UnityEngine;
using System;

public class PlayerModuleServer
{
    public PlayerModuleServer(GameWorld gameWorld, IContentResolver resourceSystem)
    {
        m_settings = Resources.Load<PlayerModuleSettings>("PlayerModuleSettings");
        m_resourceSystem = resourceSystem;

        m_world = gameWorld;
    }

    public void Shutdown()
    {
        Resources.UnloadAsset(m_settings);
    }

    public PlayerState CreatePlayer(GameWorld world, int playerId, string playerName, bool isReady)
    {
        var prefab = (GameObject)m_resourceSystem.GetSingleAssetResource(m_settings.playerStatePrefab);

        if (prefab == null)
        {
            var fallbackEntity = m_resourceSystem.CreateEntity(m_settings.playerStatePrefab);
            if (fallbackEntity != Entity.Null)
            {
                var fallbackPlayerState = m_world.GetEntityManager().GetComponentObject<PlayerState>(fallbackEntity);
                if (fallbackPlayerState != null)
                {
                    fallbackPlayerState.playerId = playerId;
                    fallbackPlayerState.playerName = playerName;

                    var fallbackReplicated = m_world.GetEntityManager().GetComponentData<ReplicatedEntityData>(fallbackEntity);
                    fallbackReplicated.predictingPlayerId = playerId;
                    m_world.GetEntityManager().SetComponentData(fallbackEntity, fallbackReplicated);

                    return fallbackPlayerState;
                }
            }

            throw new Exception("PlayerModuleServer: failed to resolve playerStatePrefab for guid " + m_settings.playerStatePrefab.GetGuidStr());
        }

        var gameObjectEntity = m_world.Spawn<GameObjectEntity>(prefab);
        var entityManager = gameObjectEntity.EntityManager;
        var entity = gameObjectEntity.Entity;
        
        var playerState = entityManager.GetComponentObject<PlayerState>(entity);
        playerState.playerId = playerId;
        playerState.playerName = playerName;

        // Mark the playerstate as 'owned' by ourselves so we can reduce amount of
        // data replicated out from server
        var re = entityManager.GetComponentData<ReplicatedEntityData>(entity);
        re.predictingPlayerId = playerId;
        entityManager.SetComponentData(entity,re);
            
        return playerState;
    }

    public void CleanupPlayer(PlayerState player)
    {
        m_world.RequestDespawn(player.gameObject);
    }

    readonly GameWorld m_world;
    readonly IContentResolver m_resourceSystem;
    readonly PlayerModuleSettings m_settings;
}
