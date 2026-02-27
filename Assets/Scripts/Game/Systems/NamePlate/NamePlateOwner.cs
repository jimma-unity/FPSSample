using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class NamePlateOwner : MonoBehaviour
{
	public NamePlate namePlatePrefab;		
	public Transform namePlateTransform;

	[NonSerialized] public bool visible = true;
	[NonSerialized] public string text;
	[NonSerialized] public int team;
	[NonSerialized] public float health;
	[NonSerialized] public NamePlate namePlate;
}

[DisableAutoCreation]
public partial class HandleNamePlateSpawn : InitializeComponentSystem<NamePlateOwner>
{
	public HandleNamePlateSpawn(GameWorld world) : base(world) {}

	protected override void Initialize(Entity entity, NamePlateOwner component)
	{
		component.namePlate = GameObject.Instantiate(component.namePlatePrefab);
	}
}

[DisableAutoCreation]
public partial class HandleNamePlateDespawn : DeinitializeComponentSystem<NamePlateOwner>
{
	public HandleNamePlateDespawn(GameWorld world) : base(world) {}

	protected override void Deinitialize(Entity entity, NamePlateOwner component)
	{
		GameObject.Destroy(component.namePlate.gameObject);
		component.namePlate = null;
	}
}


[DisableAutoCreation]
public partial class UpdateNamePlates : BaseComponentSystem
{
	EntityQuery Group;
	EntityQuery LocalPlayerGroup;
	
	public UpdateNamePlates(GameWorld world) : base(world) {}

	protected override void OnCreate()
	{
		base.OnCreate();

		Group = GetEntityQuery(typeof(NamePlateOwner), typeof(CharacterPresentationSetup));
		LocalPlayerGroup = GetEntityQuery(typeof(LocalPlayer));
	}

	protected override void OnUpdate()
	{
		var localPlayerArray = LocalPlayerGroup.ToComponentArray<LocalPlayer>();
		
		if (localPlayerArray.Length == 0)
			return;

		var localPlayer = localPlayerArray[0];
		if (localPlayer.playerState == null)
			return;

		var nameplateArray = Group.ToComponentArray<NamePlateOwner>();
		var charPresentationArray = Group.ToComponentArray<CharacterPresentationSetup>();
		
		for (int i = 0; i < nameplateArray.Length; i++)
		{
			var plateOwner = nameplateArray[i];
			if (plateOwner.namePlate == null) 
			{
				GameDebug.LogError("namePlateOwner.namePlate == null");
				continue;
			}

			var root = plateOwner.namePlate.namePlateRoot.gameObject;

            if (IngameHUD.showHud.IntValue == 0)
            {
                SetActiveIfNeeded(root, false);
                continue;
            }

			if (!plateOwner.visible)
			{
				SetActiveIfNeeded(root, false);
				continue;
			}

			// Dont show our own
			var character = charPresentationArray[i].character;
			if (character == localPlayer.playerState.controlledEntity)
			{
				SetActiveIfNeeded(root, false);
				continue;
			}
			
            // Dont show nameplate behinds
            var camera = Game.game.TopCamera();// Camera.allCameras[0];
            if (camera == null || !camera.enabled)
            {
                SetActiveIfNeeded(root, false);
                continue;
            }

			if (!IsFinite(camera.transform.position) || !IsFinite(camera.transform.forward))
			{
				SetActiveIfNeeded(root, false);
				continue;
			}

			var platePosWorld = plateOwner.namePlateTransform.position;
			if (!IsFinite(platePosWorld))
			{
				SetActiveIfNeeded(root, false);
				continue;
			}
			if (!TryProjectToScreen(camera, platePosWorld, out var screenPos))
			{
				SetActiveIfNeeded(root,false);
				continue;
			}

			if (screenPos.x < 1.0f || screenPos.x > Screen.width - 1.0f || screenPos.y < 1.0f || screenPos.y > Screen.height - 1.0f)
			{
				SetActiveIfNeeded(root,false);
				continue;
			}
			
			// Test occlusion
			var rayStart = camera.transform.position + camera.transform.forward * Mathf.Max(0.01f, camera.nearClipPlane);
			var v = platePosWorld - rayStart;
			var distance = v.magnitude;
			const int defaultLayerMask = 1 << 0;
			var occluded = Physics.Raycast(rayStart, v.normalized, distance, defaultLayerMask);
			
			var friendly = plateOwner.team == localPlayer.playerState.teamIndex;
            var color = friendly ? Game.game.gameColors[(int)Game.GameColor.Friend] : Game.game.gameColors[(int)Game.GameColor.Enemy];

			var showPlate = friendly || !occluded;

			// Update plate
			if (!showPlate)
			{
				SetActiveIfNeeded(root,false);
				continue;
			}
				
			screenPos.z = 0;
			plateOwner.namePlate.namePlateRoot.transform.position = screenPos;

			// Update icon
			var showIcon = friendly;
			SetActiveIfNeeded(plateOwner.namePlate.icon.gameObject,showIcon);
			if (showIcon)
			{
				plateOwner.namePlate.icon.color = color;
			}

			// Update name text
			var inNameTextDist = distance <= plateOwner.namePlate.maxNameDistance;
			var showNameText = !occluded && inNameTextDist;
			SetActiveIfNeeded(plateOwner.namePlate.nameText.gameObject,showNameText);
			if (showNameText)
			{
				plateOwner.namePlate.nameText.text = plateOwner.text;	
				plateOwner.namePlate.nameText.color = color;
			}
			
			SetActiveIfNeeded(root,true);
		}
    }

	static bool IsFinite(Vector3 value)
	{
		return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
		       && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
	}

	static bool TryProjectToScreen(Camera camera, Vector3 worldPosition, out Vector3 screenPosition)
	{
		screenPosition = default;
		var viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
		var clipPosition = viewProjection * new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 1.0f);

		if (Mathf.Abs(clipPosition.w) < 1e-5f)
			return false;

		var invW = 1.0f / clipPosition.w;
		var ndcX = clipPosition.x * invW;
		var ndcY = clipPosition.y * invW;
		var ndcZ = clipPosition.z * invW;

		if (!float.IsFinite(ndcX) || !float.IsFinite(ndcY) || !float.IsFinite(ndcZ))
			return false;

		screenPosition = new Vector3(
			(ndcX * 0.5f + 0.5f) * camera.pixelWidth,
			(ndcY * 0.5f + 0.5f) * camera.pixelHeight,
			clipPosition.w);

		return clipPosition.w > 0.0f;
	}

	// Set settings active on UI Text creates garbage we check for whether active state has changed 
	void SetActiveIfNeeded(GameObject go, bool active)
	{
		if (go.activeSelf != active)
		{
			go.SetActive(active);
		}
	}
}