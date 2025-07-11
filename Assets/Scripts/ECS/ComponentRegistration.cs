using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(InitializeComponentGroupSystem<Character, HandleCharacterSpawn.Initialized>))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<AnimStateController>))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<AnimStateController>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<RagdollOwner>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<NamePlateOwner>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<PlayerCameraSettings>.SystemState))]