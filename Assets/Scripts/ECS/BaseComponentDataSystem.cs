using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

public abstract partial class BaseComponentSystem : SystemBase
{
    protected BaseComponentSystem(GameWorld world)
    {
        m_world = world;
    }

    readonly protected GameWorld m_world;
}

 public abstract partial class BaseComponentSystem<T1> : BaseComponentSystem
 	where T1 : MonoBehaviour
 {
 	EntityQuery Group;
 	protected ComponentType[] ExtraComponentRequirements;
	string name;

 	public BaseComponentSystem(GameWorld world) : base(world) {}

    protected override void OnCreate()
 	{
 		base.OnCreate();
		name = GetType().Name;
 		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
 		list.AddRange(new ComponentType[] { typeof(T1) } );
		list.Add(ComponentType.Exclude<DespawningEntity>());
 		Group = GetEntityQuery(list.ToArray());
 	}
 
 	protected override void OnUpdate()
 	{
		Profiler.BeginSample(name);

 		var entityArray = Group.ToEntityArray(Allocator.TempJob);
 		var dataArray = Group.ToComponentArray<T1>();
 
 		for (var i = 0; i < entityArray.Length; i++)
 		{
 			Update(entityArray[i], dataArray[i]);
 		}
	    
	    entityArray.Dispose();
		 
		Profiler.EndSample();
 	}
 	
 	protected abstract void Update(Entity entity,T1 data);
 }


public abstract partial class BaseComponentSystem<T1,T2> : BaseComponentSystem
	where T1 : MonoBehaviour
	where T2 : MonoBehaviour
{
	EntityQuery Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name; 
	
	public BaseComponentSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] {typeof(T1), typeof(T2)});
		list.Add(ComponentType.Exclude<DespawningEntity>());
		Group = GetEntityQuery(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.ToEntityArray(Allocator.TempJob);
		var dataArray1 = Group.ToComponentArray<T1>();
		var dataArray2 = Group.ToComponentArray<T2>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i]);
		}
		
		entityArray.Dispose();
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2);
}


public abstract partial class BaseComponentSystem<T1,T2,T3> : BaseComponentSystem
	where T1 : MonoBehaviour
	where T2 : MonoBehaviour
	where T3 : MonoBehaviour
{
	EntityQuery Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3) } );
		list.Add(ComponentType.Exclude<DespawningEntity>());
		Group = GetEntityQuery(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.ToEntityArray(Allocator.TempJob);
		var dataArray1 = Group.ToComponentArray<T1>();
		var dataArray2 = Group.ToComponentArray<T2>();
		var dataArray3 = Group.ToComponentArray<T3>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i]);
		}
		
		entityArray.Dispose();
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3);
}

public abstract partial class BaseComponentDataSystem<T1> : BaseComponentSystem
	where T1 : unmanaged,IComponentData
{
	EntityQuery Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1) } );
		list.Add(ComponentType.Exclude<DespawningEntity>());
		Group = GetEntityQuery(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);
		
		var chunks = Group.ToArchetypeChunkArray(Unity.Collections.Allocator.TempJob);
		var entityType = GetEntityTypeHandle();
		var type1 = GetComponentTypeHandle<T1>(true);

		for (int chunkIndex = 0; chunkIndex < chunks.Length; ++chunkIndex)
		{
			var chunk = chunks[chunkIndex];
			var entities = chunk.GetNativeArray(entityType);
			var data1 = chunk.GetNativeArray(type1);

			for (int i = 0; i < chunk.Count; i++)
			{
				Update(entities[i], data1[i]);
			}
		}
		
		chunks.Dispose();
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data);
}

public abstract partial class BaseComponentDataSystem<T1,T2> : BaseComponentSystem
	where T1 : unmanaged,IComponentData
	where T2 : unmanaged,IComponentData
{
	EntityQuery Group;
	protected ComponentType[] ExtraComponentRequirements;
	private string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreate()
	{
		name = GetType().Name;
		base.OnCreate();
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2) } );
		list.Add(ComponentType.Exclude<DespawningEntity>());
		Group = GetEntityQuery(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var chunks = Group.ToArchetypeChunkArray(Unity.Collections.Allocator.TempJob);
		var entityType = GetEntityTypeHandle();
		var type1 = GetComponentTypeHandle<T1>(true);
		var type2 = GetComponentTypeHandle<T2>(true);

		for (int chunkIndex = 0; chunkIndex < chunks.Length; ++chunkIndex)
		{
			var chunk = chunks[chunkIndex];
			var entities = chunk.GetNativeArray(entityType);
			var data1 = chunk.GetNativeArray(type1);
			var data2 = chunk.GetNativeArray(type2);

			for (int i = 0; i < chunk.Count; i++)
			{
				Update(entities[i], data1[i], data2[i]);
			}
		}

		chunks.Dispose();

		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2);
}

public abstract partial class BaseComponentDataSystem<T1,T2,T3> : BaseComponentSystem
	where T1 : unmanaged,IComponentData
	where T2 : unmanaged,IComponentData
	where T3 : unmanaged,IComponentData
{
	EntityQuery Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3) } );
		list.Add(ComponentType.Exclude<DespawningEntity>());
		Group = GetEntityQuery(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var chunks = Group.ToArchetypeChunkArray(Unity.Collections.Allocator.TempJob);
		var entityType = GetEntityTypeHandle();
		var type1 = GetComponentTypeHandle<T1>(true);
		var type2 = GetComponentTypeHandle<T2>(true);
		var type3 = GetComponentTypeHandle<T3>(true);

		for (int chunkIndex = 0; chunkIndex < chunks.Length; ++chunkIndex)
		{
			var chunk = chunks[chunkIndex];
			var entities = chunk.GetNativeArray(entityType);
			var data1 = chunk.GetNativeArray(type1);
			var data2 = chunk.GetNativeArray(type2);
			var data3 = chunk.GetNativeArray(type3);

			for (int i = 0; i < chunk.Count; i++)
			{
				Update(entities[i], data1[i], data2[i], data3[i]);
			}
		}

		chunks.Dispose();

		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3);
}


public abstract partial class BaseComponentDataSystem<T1,T2,T3,T4> : BaseComponentSystem
	where T1 : unmanaged,IComponentData
	where T2 : unmanaged,IComponentData
	where T3 : unmanaged,IComponentData
	where T4 : unmanaged,IComponentData
{
	EntityQuery Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) } );
		list.Add(ComponentType.Exclude<DespawningEntity>());
		Group = GetEntityQuery(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var chunks = Group.ToArchetypeChunkArray(Unity.Collections.Allocator.TempJob);
		var entityType = GetEntityTypeHandle();
		var type1 = GetComponentTypeHandle<T1>(true);
		var type2 = GetComponentTypeHandle<T2>(true);
		var type3 = GetComponentTypeHandle<T3>(true);
		var type4 = GetComponentTypeHandle<T4>(true);

		for (int chunkIndex = 0; chunkIndex < chunks.Length; ++chunkIndex)
		{
			var chunk = chunks[chunkIndex];
			var entities = chunk.GetNativeArray(entityType);
			var data1 = chunk.GetNativeArray(type1);
			var data2 = chunk.GetNativeArray(type2);
			var data3 = chunk.GetNativeArray(type3);
			var data4 = chunk.GetNativeArray(type4);

			for (int i = 0; i < chunk.Count; i++)
			{
				Update(entities[i], data1[i], data2[i], data3[i], data4[i]);
			}
		}

		chunks.Dispose();

		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3,T4 data4);
}

public abstract partial class BaseComponentDataSystem<T1,T2,T3,T4,T5> : BaseComponentSystem
	where T1 : unmanaged,IComponentData
	where T2 : unmanaged,IComponentData
	where T3 : unmanaged,IComponentData
	where T4 : unmanaged,IComponentData
	where T5 : unmanaged,IComponentData
{
	EntityQuery Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) } );
		list.Add(ComponentType.Exclude<DespawningEntity>());
		Group = GetEntityQuery(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var chunks = Group.ToArchetypeChunkArray(Unity.Collections.Allocator.TempJob);
		var entityType = GetEntityTypeHandle();
		var type1 = GetComponentTypeHandle<T1>(true);
		var type2 = GetComponentTypeHandle<T2>(true);
		var type3 = GetComponentTypeHandle<T3>(true);
		var type4 = GetComponentTypeHandle<T4>(true);
		var type5 = GetComponentTypeHandle<T5>(true);

		for (int chunkIndex = 0; chunkIndex < chunks.Length; ++chunkIndex)
		{
			var chunk = chunks[chunkIndex];
			var entities = chunk.GetNativeArray(entityType);
			var data1 = chunk.GetNativeArray(type1);
			var data2 = chunk.GetNativeArray(type2);
			var data3 = chunk.GetNativeArray(type3);
			var data4 = chunk.GetNativeArray(type4);
			var data5 = chunk.GetNativeArray(type5);

			for (int i = 0; i < chunk.Count; i++)
			{
				Update(entities[i], data1[i], data2[i], data3[i], data4[i], data5[i]);
			}
		}

		chunks.Dispose();

		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3,T4 data4, T5 data5);
}

public abstract partial class InitializeComponentSystem<T> : BaseComponentSystem
	where T : MonoBehaviour
{
	public struct SystemState : IComponentData {}
	EntityQuery IncomingGroup;
	string name;
	
	public InitializeComponentSystem(GameWorld world) : base(world) {}

	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		IncomingGroup = GetEntityQuery(typeof(T),ComponentType.Exclude<SystemState>());
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var incomingEntityArray = IncomingGroup.ToEntityArray(Allocator.TempJob);
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		if (incomingEntityArray.Length > 0)
		{
			var incomingComponentArray = IncomingGroup.ToComponentArray<T>();
			for (var i = 0; i < incomingComponentArray.Length; i++)
			{
				var entity = incomingEntityArray[i];
				ecb.AddComponent(entity,new SystemState());

				Initialize(entity, incomingComponentArray[i]);
			}
		}
		
		ecb.Playback(EntityManager);
		ecb.Dispose();
		incomingEntityArray.Dispose();
		Profiler.EndSample();
	}

	protected abstract void Initialize(Entity entity, T component);
}

public abstract partial class InitializeComponentDataSystem<T,K> : BaseComponentSystem
	where T : unmanaged, IComponentData
	where K : unmanaged, IComponentData
{
	
	EntityQuery IncomingGroup;
	string name;
	
	public InitializeComponentDataSystem(GameWorld world) : base(world) {}

	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		IncomingGroup = GetEntityQuery(typeof(T),ComponentType.Exclude<K>());
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var incomingEntityArray = IncomingGroup.ToEntityArray(Allocator.TempJob);
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		if (incomingEntityArray.Length > 0)
		{
			var incomingComponentDataArray = IncomingGroup.ToComponentDataArray<T>(Allocator.TempJob);
			for (var i = 0; i < incomingComponentDataArray.Length; i++)
			{
				var entity = incomingEntityArray[i];
				ecb.AddComponent(entity,new K());

				Initialize(entity, incomingComponentDataArray[i]);
			}
			incomingComponentDataArray.Dispose();
		}
		ecb.Playback(EntityManager);
		ecb.Dispose();
		incomingEntityArray.Dispose();
		Profiler.EndSample();
	}

	protected abstract void Initialize(Entity entity, T component);
}



public abstract partial class DeinitializeComponentSystem<T> : BaseComponentSystem
	where T : MonoBehaviour
{
	EntityQuery OutgoingGroup;
	string name;

	public DeinitializeComponentSystem(GameWorld world) : base(world) {}

	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		OutgoingGroup = GetEntityQuery(typeof(T), typeof(DespawningEntity));
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var outgoingComponentArray = OutgoingGroup.ToComponentArray<T>();
		var outgoingEntityArray = OutgoingGroup.ToEntityArray(Allocator.TempJob);
		for (var i = 0; i < outgoingComponentArray.Length; i++)
		{
			Deinitialize(outgoingEntityArray[i], outgoingComponentArray[i]);
		}
		outgoingEntityArray.Dispose();
		Profiler.EndSample();
	}

	protected abstract void Deinitialize(Entity entity, T component);
}


public abstract partial class DeinitializeComponentDataSystem<T> : BaseComponentSystem
	where T : unmanaged, IComponentData
{
	EntityQuery OutgoingGroup;
	string name;

	public DeinitializeComponentDataSystem(GameWorld world) : base(world) {}

	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		OutgoingGroup = GetEntityQuery(typeof(T), typeof(DespawningEntity));
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var outgoingComponentArray = OutgoingGroup.ToComponentDataArray<T>(Allocator.TempJob);
		var outgoingEntityArray = OutgoingGroup.ToEntityArray(Allocator.TempJob);
		for (var i = 0; i < outgoingComponentArray.Length; i++)
		{
			Deinitialize(outgoingEntityArray[i], outgoingComponentArray[i]);
		}

		outgoingComponentArray.Dispose();
		outgoingEntityArray.Dispose();
		Profiler.EndSample();
	}

	protected abstract void Deinitialize(Entity entity, T component);
}

public abstract partial class InitializeComponentGroupSystem<T,S> : BaseComponentSystem
	where T : MonoBehaviour
	where S : unmanaged, IComponentData
{
	EntityQuery IncomingGroup;
	string name;

	public InitializeComponentGroupSystem(GameWorld world) : base(world) {}

	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		IncomingGroup = GetEntityQuery(typeof(T),ComponentType.Exclude<S>());
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var incomingEntityArray = IncomingGroup.ToEntityArray(Allocator.TempJob);
		var ecb = new EntityCommandBuffer(Allocator.TempJob);
		if (incomingEntityArray.Length > 0)
		{
			for (var i = 0; i < incomingEntityArray.Length; i++)
			{
				var entity = incomingEntityArray[i];
				ecb.AddComponent(entity, new S());
			}
			Initialize(ref IncomingGroup);
		}
		incomingEntityArray.Dispose();
		ecb.Playback(EntityManager);
		ecb.Dispose();
		Profiler.EndSample();
	}

	protected abstract void Initialize(ref EntityQuery group);
}



public abstract partial class DeinitializeComponentGroupSystem<T> : BaseComponentSystem
	where T : MonoBehaviour
{
	EntityQuery OutgoingGroup;
	string name;

	public DeinitializeComponentGroupSystem(GameWorld world) : base(world) {}

	protected override void OnCreate()
	{
		base.OnCreate();
		name = GetType().Name;
		OutgoingGroup = GetEntityQuery(typeof(T), typeof(DespawningEntity));
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		if (OutgoingGroup.IsEmpty == false)
			Deinitialize(ref OutgoingGroup);
		
		Profiler.EndSample();
	}

	protected abstract void Deinitialize(ref EntityQuery group);
}
