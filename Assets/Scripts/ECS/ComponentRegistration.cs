using Unity.Entities;

// JAPA ¯\_(ツ)_/¯ - No idea why this was necessary before but now needs to be commented. To investigate.
//[assembly: RegisterGenericComponentType(typeof(InitializeComponentGroupSystem<Character, HandleCharacterSpawn.Initialized>))]
//[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<AnimStateController>))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<AnimStateController>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<RagdollOwner>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<NamePlateOwner>.SystemState))]
[assembly: RegisterGenericComponentType(typeof(InitializeComponentSystem<PlayerCameraSettings>.SystemState))]

// JAPA ¯\_(ツ)_/¯ - These would ideally ultimately be removed (or at least partially) as it indicates
// we are accessing types from ECS code that we ideally should not.

// Packages
[assembly: RegisterGenericComponentType(typeof(HDDynamicResolution))]

// Engine
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.Animator))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.AudioListener))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.AudioReverbFilter))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.Camera))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.Canvas))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.MeshFilter))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.Rendering.PostProcessing.PostProcessLayer))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.RectTransform))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.UI.CanvasScaler))]
[assembly: RegisterUnityEngineComponentType(typeof(UnityEngine.UI.GraphicRaycaster))]
