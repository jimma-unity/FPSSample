using Unity.Entities;

// JAPA ¯\_(ツ)_/¯ - No idea why this was necessary before but now needs to be commented. To investigate.
//[assembly: RegisterGenericComponentType(typeof(InitializeComponentGroupSystem<Character, HandleCharacterSpawn.Initialized>))]
//[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<AnimStateController>))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<AnimStateController>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<RagdollOwner>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<NamePlateOwner>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<PlayerCameraSettings>.SystemState))]