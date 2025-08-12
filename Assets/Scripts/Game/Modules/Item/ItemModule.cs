using System.Collections.Generic;
using Unity.Entities;

public class ItemModule
{
    List<BaseComponentSystem> m_handleSpawnSystems = new List<BaseComponentSystem>();
    List<BaseComponentSystem> m_systems = new List<BaseComponentSystem>();
    GameWorld m_world;
    
    public ItemModule(GameWorld world)
    {
        m_world = world;
        
        // TODO (mogensh) make server version without all this client stuff
        m_systems.Add(world.GetECSWorld().AddSystemManaged(new RobotWeaponClientProjectileSpawnHandler(world)));
        m_systems.Add(world.GetECSWorld().AddSystemManaged(new TerraformerWeaponClientProjectileSpawnHandler(world)));
        m_systems.Add(world.GetECSWorld().AddSystemManaged(new UpdateTerraformerWeaponA(world)));
        m_systems.Add(world.GetECSWorld().AddSystemManaged(new UpdateItemActionTimelineTrigger(world)));
        m_systems.Add(world.GetECSWorld().AddSystemManaged(new System_RobotWeaponA(world)));
    }

    public void HandleSpawn()
    {
        foreach (var system in m_handleSpawnSystems)
            system.Update();
    }

    public void Shutdown()
    {
        foreach (var system in m_handleSpawnSystems)
            m_world.GetECSWorld().DestroySystemManaged(system);
        foreach (var system in m_systems)
            m_world.GetECSWorld().DestroySystemManaged(system);
    }

    public void LateUpdate()
    {        
        foreach (var system in m_systems)
            system.Update();
    }
}
