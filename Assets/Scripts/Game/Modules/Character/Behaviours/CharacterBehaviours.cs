using System.Collections.Generic;
using Unity.Entities;

public class CharacterBehaviours   
{
    public static void CreateHandleSpawnSystems(GameWorld world,SystemCollection systems, BundledResourceManager resourceManager, bool server)
    {        
        systems.Add(world.GetECSWorld().AddSystem(new HandleCharacterSpawn(world, resourceManager, server))); // TODO (mogensh) needs to be done first as it creates presentation
        systems.Add(world.GetECSWorld().AddSystem(new HandleAnimStateCtrlSpawn(world)));
    }

    public static void CreateHandleDespawnSystems(GameWorld world,SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystem(new HandleCharacterDespawn(world)));  // TODO (mogens) HandleCharacterDespawn dewpans char presentation and needs to be called before other HandleDespawn. How do we ensure this ?   
        systems.Add(world.GetECSWorld().AddSystem(new HandleAnimStateCtrlDespawn(world)));
    }

    public static void CreateAbilityRequestSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystem(new Movement_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new RocketJump_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Dead_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new AutoRifle_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Chaingun_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new GrenadeLauncher_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new ProjectileLauncher_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Sprint_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Melee_RequestActive(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Emote_RequestActive(world)));
        
        // Update main abilities
        systems.Add(world.GetECSWorld().AddSystem(new DefaultBehaviourController_Update(world)));
    }
    
    public static void CreateMovementStartSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystem(new GroundTest(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Movement_Update(world)));
    }

    public static void CreateMovementResolveSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystem(new HandleMovementQueries(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Movement_HandleCollision(world)));
    }

    public static void CreateAbilityStartSystems(GameWorld world, SystemCollection systems)
    {
        
        systems.Add(world.GetECSWorld().AddSystem(new RocketJump_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Sprint_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new AutoRifle_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new ProjectileLauncher_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Chaingun_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new GrenadeLauncher_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Melee_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Emote_Update(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Dead_Update(world)));
    }

    public static void CreateAbilityResolveSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().AddSystem(new AutoRifle_HandleCollisionQuery(world)));
        systems.Add(world.GetECSWorld().AddSystem(new Melee_HandleCollision(world)));
    }
    
}
