
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

public class CharacterModulePreview : CharacterModuleShared
{

    public CharacterModulePreview(GameWorld world, BundledResourceManager resourceSystem): base(world)
    {
        // Handle spawn requests
        m_HandleCharacterSpawnRequests = m_world.GetECSWorld().AddSystemManaged(new HandleCharacterSpawnRequests(m_world, resourceSystem, false));
        m_HandleCharacterDepawnRequests = m_world.GetECSWorld().AddSystemManaged(new HandleCharacterDespawnRequests(m_world));

        // Handle control change        
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().AddSystemManaged(new PlayerCharacterControlSystem(m_world)));
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().AddSystemManaged(new UpdateCharacter1PSpawn(m_world, resourceSystem)));

        // Handle spawning
        CharacterBehaviours.CreateHandleSpawnSystems(m_world,m_HandleSpawnSystems, resourceSystem, false);

        // Handle despawn
        CharacterBehaviours.CreateHandleDespawnSystems(m_world, m_HandleDespawnSystems);
        
        // Behaviors 
        CharacterBehaviours.CreateAbilityRequestSystems(m_world, m_AbilityRequestUpdateSystems);
        m_MovementStartSystems.Add(m_world.GetECSWorld().AddSystemManaged(new UpdateTeleportation(m_world)));
        CharacterBehaviours.CreateMovementStartSystems(m_world,m_MovementStartSystems);
        CharacterBehaviours.CreateMovementResolveSystems(m_world,m_MovementResolveSystems);
        CharacterBehaviours.CreateAbilityStartSystems(m_world,m_AbilityStartSystems);
        CharacterBehaviours.CreateAbilityResolveSystems(m_world,m_AbilityResolveSystems);

        m_UpdateCharPresentationState = m_world.GetECSWorld().AddSystemManaged(new UpdateCharPresentationState(m_world));
        m_ApplyPresentationState = m_world.GetECSWorld().AddSystemManaged(new ApplyPresentationState(m_world));
        
        m_CharacterLateUpdate = m_world.GetECSWorld().AddSystemManaged(new CharacterLateUpdate(m_world));
        
        m_HandleDamage = m_world.GetECSWorld().AddSystemManaged(new HandleDamage(m_world));
        
        m_updateCharacterUI = m_world.GetECSWorld().AddSystemManaged(new UpdateCharacterUI(m_world));
        m_characterCameraSystem = m_world.GetECSWorld().AddSystemManaged(new UpdateCharacterCamera(m_world));
        
        m_UpdatePresentationRootTransform = m_world.GetECSWorld().AddSystemManaged(new UpdatePresentationRootTransform(m_world));
        m_UpdatePresentationAttachmentTransform = m_world.GetECSWorld().AddSystemManaged(new UpdatePresentationAttachmentTransform(m_world));
            
        m_HandleCharacterEvents = m_world.GetECSWorld().CreateSystemManaged<HandleCharacterEvents>();
            
        // Preload all character resources (until we have better streaming solution)
        var charRegistry = resourceSystem.GetResourceRegistry<CharacterTypeRegistry>();
        for (var i = 0; i < charRegistry.entries.Count; i++)
        {
            resourceSystem.GetSingleAssetResource(charRegistry.entries[i].prefab1P);
            resourceSystem.GetSingleAssetResource(charRegistry.entries[i].prefabClient);
        }

        Console.AddCommand("thirdperson", CmdToggleThirdperson, "Toggle third person mode", this.GetHashCode());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        
        
        m_world.GetECSWorld().DestroySystemManaged(m_HandleCharacterSpawnRequests);
        m_world.GetECSWorld().DestroySystemManaged(m_HandleCharacterDepawnRequests);

        m_world.GetECSWorld().DestroySystemManaged(m_UpdateCharPresentationState);
        m_world.GetECSWorld().DestroySystemManaged(m_CharacterLateUpdate);

        m_world.GetECSWorld().DestroySystemManaged(m_HandleDamage);
        m_world.GetECSWorld().DestroySystemManaged(m_updateCharacterUI);
        m_world.GetECSWorld().DestroySystemManaged(m_characterCameraSystem);

        m_world.GetECSWorld().DestroySystemManaged(m_UpdatePresentationRootTransform);
        m_world.GetECSWorld().DestroySystemManaged(m_UpdatePresentationAttachmentTransform);

        m_world.GetECSWorld().DestroySystemManaged(m_ApplyPresentationState);
        
        m_world.GetECSWorld().DestroySystemManaged(m_HandleCharacterEvents);
        
        Console.RemoveCommandsWithTag(GetHashCode());
    }

    public void HandleSpawnRequests()
    {
        m_HandleCharacterDepawnRequests.Update();
        m_HandleCharacterSpawnRequests.Update();
    }
    
    public void HandleDamage() 
    {
        m_HandleDamage.Update();
    }
    
    
    public void UpdatePresentation()
    {
        m_UpdateCharPresentationState.Update();
        m_ApplyPresentationState.Update();      
    }

    public void LateUpdate()
    {
        m_CharacterLateUpdate.Update();
        m_UpdatePresentationRootTransform.Update();
        m_characterCameraSystem.Update();
        m_UpdatePresentationAttachmentTransform.Update();
    }

   
    public void UpdateUI()
    {
        m_updateCharacterUI.Update();
        m_HandleCharacterEvents.Update();
    }

    void CmdToggleThirdperson(string[] args)
    {
        m_characterCameraSystem.ToggleFOrceThirdPerson();
    }
    

    
    readonly HandleCharacterSpawnRequests m_HandleCharacterSpawnRequests;
    readonly HandleCharacterDespawnRequests m_HandleCharacterDepawnRequests;

   
    readonly UpdateCharPresentationState m_UpdateCharPresentationState;

    readonly ApplyPresentationState m_ApplyPresentationState;
    
    readonly HandleDamage m_HandleDamage;
    readonly UpdateCharacterUI m_updateCharacterUI;
    readonly UpdateCharacterCamera m_characterCameraSystem;

    readonly UpdatePresentationRootTransform m_UpdatePresentationRootTransform;
    readonly UpdatePresentationAttachmentTransform m_UpdatePresentationAttachmentTransform;

    readonly CharacterLateUpdate m_CharacterLateUpdate;
        

    readonly HandleCharacterEvents m_HandleCharacterEvents;
}
