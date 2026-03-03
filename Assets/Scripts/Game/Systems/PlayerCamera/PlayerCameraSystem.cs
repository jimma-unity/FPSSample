using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[DisableAutoCreation]
public partial class HandlePlayerCameraControlSpawn : InitializeComponentSystem<PlayerCameraSettings>
{
    public HandlePlayerCameraControlSpawn(GameWorld world) : base(world)
    {
        m_cameraPrefab = Resources.Load<PlayerCamera>("Prefabs/PlayerCamera");
    }

    protected override void Initialize(Entity entity, PlayerCameraSettings component)
    {
        var camera = m_world.Spawn<PlayerCamera>(m_cameraPrefab.gameObject);
        camera.cameraSettings = component;
        camera.gameObject.SetActive(false);
    }

    PlayerCamera m_cameraPrefab;
}

[DisableAutoCreation]
public partial class UpdatePlayerCameras : BaseComponentSystem
{
    public EntityQuery Group;
    const float k_MaxCameraDistance = 200000.0f;
    const float k_DefaultMinCameraWorldY = -30.0f;

    public UpdatePlayerCameras(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        Group = GetEntityQuery(typeof(PlayerCamera), typeof(Camera));
    }

    protected override void OnUpdate()
    {
        var cameraArray = Group.ToComponentArray<Camera>();
        var playerCameraArray = Group.ToComponentArray<PlayerCamera>();
        for (var i = 0; i < cameraArray.Length; i++)
        {
            try
            {
                var camera = cameraArray[i];
                var playerCamera = playerCameraArray[i];
                if (camera == null || playerCamera == null)
                    continue;

                var gameObject = camera.gameObject;
                if (gameObject == null)
                    continue;

                var settings = playerCamera.cameraSettings;
                var enabled = settings.isEnabled;
                var isActive = gameObject.activeSelf;
                if (!enabled)
                {
                    if (isActive)
                    {
                        Game.game.PopCamera(camera);
                        gameObject.SetActive(false);
                    }
                    continue;
                }

                if (!isActive)
                {
                    gameObject.SetActive(true);
                    Game.game.PushCamera(camera);
                }

                camera.fieldOfView = settings.fieldOfView;
                if (debugCameraDetach.IntValue == 0)
                {
                    var desiredPosition = settings.position;
                    var desiredRotation = settings.rotation;
                    var rawPosition = desiredPosition;

                    var invalidPosition = !IsFinite(desiredPosition) || desiredPosition.sqrMagnitude > (k_MaxCameraDistance * k_MaxCameraDistance);
                    if (invalidPosition)
                    {
                        desiredPosition = camera.transform.position;
                        settings.position = desiredPosition;
                        if (UnityEngine.Time.frameCount % 120 == 0)
                            GameDebug.LogWarning("PlayerCamera pose guard: rejected invalid camera position " + rawPosition + " and kept previous transform position.");
                    }

                    if (!IsFinite(desiredRotation))
                    {
                        desiredRotation = camera.transform.rotation;
                        settings.rotation = desiredRotation;
                        if (UnityEngine.Time.frameCount % 120 == 0)
                            GameDebug.LogWarning("PlayerCamera pose guard: rejected invalid camera rotation and kept previous transform rotation.");
                    }

                    var minCameraWorldY = k_DefaultMinCameraWorldY;
                    if (desiredPosition.y < minCameraWorldY)
                    {
                        desiredPosition.y = minCameraWorldY;
                        settings.position = desiredPosition;
                        if (UnityEngine.Time.frameCount % 120 == 0)
                            GameDebug.LogWarning("PlayerCamera pose guard: clamped camera Y below world floor. minY=" + minCameraWorldY + ", requested=" + rawPosition.y);
                    }

                    camera.transform.position = desiredPosition;
                    camera.transform.rotation = desiredRotation;
                }

                if(debugCameraDetach.ChangeCheck())
                {
                    Game.Input.SetBlock(Game.Input.Blocker.Debug, debugCameraDetach.IntValue == 2);
                }
                if (debugCameraDetach.IntValue == 2 && !Console.IsOpen())
                {
                    var eu = camera.transform.localEulerAngles;
                    if (eu.x > 180.0f) eu.x -= 360.0f;
                    eu.x = Mathf.Clamp(eu.x, -70.0f, 70.0f);
                    eu += new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);
                    float invertY = Game.configInvertY.IntValue > 0 ? 1.0f : -1.0f;
                    eu += SystemAPI.Time.DeltaTime * (new Vector3(- invertY * Input.GetAxisRaw("RightStickY")*InputSystem.s_JoystickLookSensitivity.y, Input.GetAxisRaw("RightStickX") * InputSystem.s_JoystickLookSensitivity.x, 0));
                    camera.transform.localEulerAngles = eu;
                    m_DetachedMoveSpeed += Input.GetAxisRaw("Mouse ScrollWheel");
                    float verticalMove = (Game.Input.GetKeyNoBlock(Key.R) ? 1.0f : 0.0f) + (Game.Input.GetKeyNoBlock(Key.F) ? -1.0f : 0.0f);
                    verticalMove += Input.GetAxisRaw("Trigger");
                    camera.transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), verticalMove, Input.GetAxisRaw("Vertical")) * SystemAPI.Time.DeltaTime * m_DetachedMoveSpeed);
                }
            }
            catch (System.NullReferenceException)
            {
                continue;
            }
            catch (MissingReferenceException)
            {
                continue;
            }
        }
    }

    // Debugging graphs to show player movement in 3 axis
    static float[] movehist_x = new float[100];
    static float[] movehist_y = new float[100];
    static float[] movehist_z = new float[100];
    static float lastUsedFrame;

    [ConfigVar(Name = "debug.cameramove", Description = "Show graphs of first person camera rotation", DefaultValue = "0")]
    public static ConfigVar debugCameraMove;
    [ConfigVar(Name = "debug.cameradetach", Description = "Detach player camera from player", DefaultValue = "0")]
    public static ConfigVar debugCameraDetach;

    float m_DetachedMoveSpeed = 4.0f;

    static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    static bool IsFinite(Quaternion value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}