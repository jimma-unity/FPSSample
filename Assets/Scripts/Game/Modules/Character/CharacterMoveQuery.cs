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

    [NonSerialized] public CharacterController charController;
    [NonSerialized] public Settings settings;
    
    public void Initialize(Settings settings, Entity hitCollOwner)
    {
        //GameDebug.Log("CharacterMoveQuery.Initialize");
        this.settings = settings;
        var go = new GameObject("MoveColl_" + name,typeof(CharacterController), typeof(HitCollision));
        charController = go.GetComponent<CharacterController>();
        charController.transform.position = transform.position;
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
	
    public HandleMovementQueries(GameWorld world) : base(world) {}
	
    protected override void OnCreate()
    {
        base.OnCreate();
        Group = GetEntityQuery(typeof(CharacterMoveQuery));
    }

    protected override void OnUpdate()
    {
        var queryArray = Group.ToComponentArray<CharacterMoveQuery>();

        for (var i = 0; i < queryArray.Length; i++)
        {
            var query = queryArray[i];

            var charController = query.charController;

            if (charController.gameObject.layer != query.collisionLayer)
                charController.gameObject.layer = query.collisionLayer;
            
            float3 currentControllerPos = charController.transform.position;
            if (math.distance(currentControllerPos, query.moveQueryStart) > 0.01f)
            {
                currentControllerPos = query.moveQueryStart;
                
                // JAPA ¯\_(ツ)_/¯ - Hack - previously Physics.autoSyncTransforms was active and setting the CharacterController transform directly was not an issue.
                // However, Physics.autoSyncTransforms is deprecated and to be removed. Without it active, setting the Transform *only* updates that value but not the internal Physics Character representation.
                // As such, when we subsequently call Move() below this block, what is actually moved is still at the old position.
                // Currently, the game spawns client-side players at 0,0,0 and so the delta between actual position and move destination can be huge, with collision in the way, etc.
                // The CharacterController does not have a teleport or other function, and we do not want to do Physics.SyncTransforms for each CharacterController.
                // So we are left with disable->change transform->enable which while clunky does at least update the internal representation.
                
                charController.enabled = false;
                charController.transform.position = currentControllerPos;
                charController.enabled = true;
            }

            var deltaPos = query.moveQueryEnd - currentControllerPos; 
            charController.Move(deltaPos);
            query.moveQueryResult = charController.transform.position;
            query.isGrounded = charController.isGrounded;
        }
    }
}