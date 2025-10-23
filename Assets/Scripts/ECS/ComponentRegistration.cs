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
[assembly: RegisterGenericComponentType(typeof(UnityEngine.Animator))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.AudioListener))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.AudioReverbFilter))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.Camera))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.Canvas))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.FlareLayer))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.MeshFilter))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.Rendering.PostProcessing.PostProcessLayer))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.RectTransform))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.UI.CanvasScaler))]
[assembly: RegisterGenericComponentType(typeof(UnityEngine.UI.GraphicRaycaster))]
