using UnityEditor;
using UnityEngine;

namespace Unity.Entities.Editor
{
    [CustomEditor(typeof(GameObjectEntity))]
    public class GameObjectEntityEditor : UnityEditor.Editor
    {
        // JAPA ¯\_(ツ)_/¯
        //[SerializeField] private SystemInclusionList inclusionList;

        private void OnEnable()
        {
            // JAPA ¯\_(ツ)_/¯
            //inclusionList = new SystemInclusionList();
        }

        public override void OnInspectorGUI()
        {
            var gameObjectEntity = (GameObjectEntity)target;
            if (gameObjectEntity.World?.IsCreated != true)
                return;
            if (!gameObjectEntity.EntityManager.Exists(gameObjectEntity.Entity))
                return;

            // JAPA ¯\_(ツ)_/¯
            //inclusionList.OnGUI(World.DefaultGameObjectInjectionWorld, gameObjectEntity.Entity);
        }
    }
}
