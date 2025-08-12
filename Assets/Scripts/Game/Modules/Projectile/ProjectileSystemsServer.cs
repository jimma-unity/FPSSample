using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

[DisableAutoCreation]
public partial class HandleServerProjectileRequests : BaseComponentSystem
{
	EntityQuery Group;

	public HandleServerProjectileRequests(GameWorld world, BundledResourceManager resourceSystem) : base(world)
	{
		m_resourceSystem = resourceSystem;
    
		m_settings = Resources.Load<ProjectileModuleSettings>("ProjectileModuleSettings");
	}

	protected override void OnCreate()
	{
		base.OnCreate();
		Group = GetEntityQuery(typeof(ProjectileRequest));
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Resources.UnloadAsset(m_settings);
	}

	protected override void OnUpdate()
	{
		var entityArray = Group.ToEntityArray(Allocator.TempJob);
		var requestArray = Group.ToComponentDataArray<ProjectileRequest>(Allocator.TempJob);
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		
		// Copy requests as spawning will invalidate Group 
		var requests = new ProjectileRequest[requestArray.Length];
		for (var i = 0; i < requestArray.Length; i++)
		{
			requests[i] = requestArray[i];
			ecb.DestroyEntity(entityArray[i]);
		}

		// Handle requests
		var projectileRegistry = m_resourceSystem.GetResourceRegistry<ProjectileRegistry>();
		foreach (var request in requests)
		{
			var registryIndex = projectileRegistry.FindIndex(request.projectileAssetGuid);
			if (registryIndex == -1)
			{
				GameDebug.LogError("Cant find asset guid in registry");
				continue;
			}

			var projectileEntity = m_settings.projectileFactory.Create(EntityManager,m_resourceSystem, m_world);

			var projectileData = EntityManager.GetComponentData<ProjectileData>(projectileEntity);
			projectileData.SetupFromRequest(request, registryIndex);
			projectileData.Initialize(projectileRegistry);
			
			ecb.SetComponent(projectileEntity, projectileData);
			ecb.AddComponent(projectileEntity, new UpdateProjectileFlag());
		}

		ecb.Playback(EntityManager);
		ecb.Dispose();
		entityArray.Dispose();
		requestArray.Dispose();
	}

	BundledResourceManager m_resourceSystem;
	ProjectileModuleSettings m_settings;
}
