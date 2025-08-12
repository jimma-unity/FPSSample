using System.Collections.Generic;
using Unity.Entities;

public class CharacterBehaviours   
{
    public static void CreateHandleSpawnSystems(GameWorld world,SystemCollection systems, BundledResourceManager resourceManager, bool server)
    {        
        systems.Add(world.GetECSWorld().AddSystemManaged(new HandleCharacterSpawn(world, resourceManager, server))); // TODO (mogensh) needs to be done first as it creates presentation
        systems.Add(world.GetECSWorld().AddSystemManaged(new HandleAnimStateCtrlSpawn(world)));
    }

    public static void CreateHandleDespawnSystems(GameWorld world,SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystemManaged(new HandleCharacterDespawn(world)));  // TODO (mogens) HandleCharacterDespawn dewpans char presentation and needs to be called before other HandleDespawn. How do we ensure this ?   
        systems.Add(world.GetECSWorld().AddSystemManaged(new HandleAnimStateCtrlDespawn(world)));
    }

    public static void CreateAbilityRequestSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystemManaged(new Movement_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new RocketJump_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Dead_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new AutoRifle_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Chaingun_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new GrenadeLauncher_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new ProjectileLauncher_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Sprint_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Melee_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Emote_RequestActive(world)));
        
        // Update main abilities
        systems.Add(world.GetECSWorld().AddSystemManaged(new DefaultBehaviourController_Update(world)));
    }
    
    public static void CreateMovementStartSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystemManaged(new GroundTest(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Movement_Update(world)));
    }

    public static void CreateMovementResolveSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystemManaged(new HandleMovementQueries(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Movement_HandleCollision(world)));
    }

    public static void CreateAbilityStartSystems(GameWorld world, SystemCollection systems)
    {
        
        systems.Add(world.GetECSWorld().AddSystemManaged(new RocketJump_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Sprint_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new AutoRifle_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new ProjectileLauncher_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Chaingun_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new GrenadeLauncher_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Melee_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Emote_Update(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Dead_Update(world)));
    }

    public static void CreateAbilityResolveSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystemManaged(new AutoRifle_HandleCollisionQuery(world)));
        systems.Add(world.GetECSWorld().AddSystemManaged(new Melee_HandleCollision(world)));
    }
    
}
