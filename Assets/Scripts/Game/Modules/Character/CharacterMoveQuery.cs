using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class CharacterMoveQuery : MonoBehaviour
{
    [Serializable]
    public struct Settings
    {
        public float slopeLimit;
        public float stepOffset;
        public float skinWidth;
        public float minMoveDistance;
        public float3 center;
        public float radius;
        public float height;
    }

    [NonSerialized] public int collisionLayer;
    [NonSerialized] public float3 moveQueryStart;
    [NonSerialized] public float3 moveQueryEnd;
    [NonSerialized] public float3 moveQueryResult;
    [NonSerialized] public bool isGrounded;
    [NonSerialized] public bool hasLastValidPosition;
    [NonSerialized] public float3 lastValidPosition;

    [NonSerialized] public CharacterController charController;
    [NonSerialized] public Settings settings;
    
    public void Initialize(Settings settings, Entity hitCollOwner)
    {
        //GameDebug.Log("CharacterMoveQuery.Initialize");
        this.settings = settings;
        var go = new GameObject("MoveColl_" + name,typeof(CharacterController), typeof(HitCollision));
        charController = go.GetComponent<CharacterController>();
        charController.transform.position = transform.position;
        hasLastValidPosition = true;
        lastValidPosition = transform.position;
        charController.slopeLimit = settings.slopeLimit;
        charController.stepOffset = settings.stepOffset;
        charController.skinWidth = settings.skinWidth;
        charController.minMoveDistance = settings.minMoveDistance;
        charController.center = settings.center; 
        charController.radius = settings.radius; 
        charController.height = settings.height;

        var hitCollision = go.GetComponent<HitCollision>();
        hitCollision.owner = hitCollOwner;
    }

    public void Shutdown()
    {
        //GameDebug.Log("CharacterMoveQuery.Shutdown");
        GameObject.Destroy(charController.gameObject);
    }
}


[DisableAutoCreation]
partial class HandleMovementQueries : BaseComponentSystem
{
    EntityQuery Group;
    const float k_MaxWorldDistance = 20000.0f;
	
    public HandleMovementQueries(GameWorld world) : base(world) {}
	
    protected override void OnCreate()
    {
        base.OnCreate();
        Group = GetEntityQuery(typeof(CharacterMoveQuery));
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample("HandleMovementQueries");
        
        var queryArray = Group.ToComponentArray<CharacterMoveQuery>();

        for (var i = 0; i < queryArray.Length; i++)
        {
            var query = queryArray[i];

            var charController = query.charController;
            var controllerPosition = (float3)charController.transform.position;

            if (IsFinite(controllerPosition) && math.lengthsq(controllerPosition) <= k_MaxWorldDistance * k_MaxWorldDistance)
            {
                query.hasLastValidPosition = true;
                query.lastValidPosition = controllerPosition;
            }

            if (charController.gameObject.layer != query.collisionLayer)
                charController.gameObject.layer = query.collisionLayer;
            
            var controllerOutOfBounds = !IsFinite(controllerPosition) || math.lengthsq(controllerPosition) > k_MaxWorldDistance * k_MaxWorldDistance;
            var queryOutOfBounds = !IsFinite(query.moveQueryStart) || !IsFinite(query.moveQueryEnd) || math.lengthsq(query.moveQueryStart) > k_MaxWorldDistance * k_MaxWorldDistance || math.lengthsq(query.moveQueryEnd) > k_MaxWorldDistance * k_MaxWorldDistance;
            if (queryOutOfBounds || controllerOutOfBounds)
            {
                var recoveryPosition = query.hasLastValidPosition ? query.lastValidPosition : float3.zero;
                charController.transform.position = recoveryPosition;
                query.moveQueryStart = recoveryPosition;
                query.moveQueryEnd = query.moveQueryStart;
                query.moveQueryResult = query.moveQueryStart;
                query.isGrounded = charController.isGrounded;
                if (UnityEngine.Time.frameCount % 120 == 0)
                    GameDebug.LogWarning("CharacterMoveQuery sanitize: recovered invalid movement state. queryOutOfBounds=" + queryOutOfBounds + ", controllerOutOfBounds=" + controllerOutOfBounds + ", startLenSq=" + math.lengthsq(query.moveQueryStart) + ", endLenSq=" + math.lengthsq(query.moveQueryEnd) + ", controllerLenSq=" + math.lengthsq(controllerPosition) + ", start=" + query.moveQueryStart + ", end=" + query.moveQueryEnd + ", controller=" + controllerPosition + ", recovery=" + recoveryPosition);
                continue;
            }

            float3 currentControllerPos = charController.transform.position;
            if (math.distance(currentControllerPos, query.moveQueryStart) > 0.01f)
            {
                currentControllerPos = query.moveQueryStart;
                charController.transform.position = currentControllerPos;
            }

            var deltaPos = query.moveQueryEnd - currentControllerPos; 
            charController.Move(deltaPos);
            query.moveQueryResult = charController.transform.position;
            query.isGrounded = charController.isGrounded;
        }
        
        Profiler.EndSample();
    }

    static bool IsFinite(float3 value)
    {
        return math.isfinite(value.x) && math.isfinite(value.y) && math.isfinite(value.z);
    }
}