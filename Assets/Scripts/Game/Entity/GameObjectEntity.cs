using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Conversion;
using UnityEngine;

using UnityComponent = UnityEngine.Component;

namespace Unity.Entities
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("")]
    public class GameObjectEntity : MonoBehaviour
    {
        public World World
        {
            get
            {
                if (enabled && gameObject.activeInHierarchy)
                    ReInitializeEntityManagerAndEntityIfNecessary();
                return m_World;
            }
        }

        private World m_World;

        public EntityManager EntityManager
        {
            get
            {
                World w = World;
                if (w != null && w.IsCreated)return w.EntityManager;
                else return default;
            }
        }

        public Entity Entity
        {
            get
            {
                if (enabled && gameObject.activeInHierarchy)
                    ReInitializeEntityManagerAndEntityIfNecessary();
                return m_Entity;
            }
        }
        Entity m_Entity;

        void ReInitializeEntityManagerAndEntityIfNecessary()
        {
            // in case e.g., on a prefab that was open for edit when domain was unloaded
            // existing EntityManager lost all its data, so simply create a new one
            if (m_World != null && !m_World.IsCreated && !m_Entity.Equals(default))
                Initialize();
        }

        static List<Component> s_ComponentsCache = new List<Component>();

        public static Entity AddToEntityManager(EntityManager entityManager, GameObject gameObject)
        {
            var entity =
                CreateGameObjectEntity(entityManager, gameObject, s_ComponentsCache);
            s_ComponentsCache.Clear();
            return entity;
        }

        //@TODO: is this used? deprecate?
        public static void AddToEntity(EntityManager entityManager, GameObject gameObject, Entity entity)
        {
            var components = gameObject.GetComponents<Component>();

            for (var i = 0; i != components.Length; i++)
            {
                var component = components[i];
                if (component == null || component is GameObjectEntity || component.IsComponentDisabled())
                    continue;

                entityManager.AddComponentObject(entity, component);
            }
        }

        void Initialize()
        {
            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                m_World = World.DefaultGameObjectInjectionWorld;
                m_Entity = AddToEntityManager(m_World.EntityManager, gameObject);
            }
        }

        protected virtual void OnEnable()
        {
            Initialize();
        }

        protected virtual void OnDisable()
        {
            if (m_World != null && m_World.IsCreated)
            {
                var em = m_World.EntityManager;
                if (em.Exists(Entity))
                    em.DestroyEntity(Entity);
            }

            m_World = null;
            m_Entity = Entity.Null;
        }

        /////////////// JAPA
        internal static unsafe Entity CreateGameObjectEntity(EntityManager entityManager, GameObject gameObject,
            List<UnityComponent> componentsCache)
        {
            var componentTypes = stackalloc ComponentType[128];
            if (!gameObject.GetComponents(componentTypes, 128, componentsCache))
                return Entity.Null;

            EntityArchetype archetype = entityManager.CreateArchetype(componentTypes, componentsCache.Count);

            var entity = entityManager.CreateEntity(archetype);

            for (var i = 0; i != componentsCache.Count; i++)
            {
                var com = componentsCache[i];
                var componentDataProxy = com as ComponentDataProxyBase;
                if (componentDataProxy != null)
                {
                    componentDataProxy.UpdateComponentData(entityManager, entity);
                }
                else if (com != null)
                {
                    entityManager.SetComponentObject(entity, componentTypes[i], com);
                }
            }

            return entity;
        }
    }

    static class UnityEngineExtensions
    {
        public static unsafe bool GetComponents(this GameObject @this, ComponentType* componentTypes,
            int maxComponentTypes, List<Component> componentsCache)
        {
            int outputIndex = 0;
            @this.GetComponents(componentsCache);

            if (maxComponentTypes < componentsCache.Count)
            {
                Debug.LogWarning($"Too many components on {@this.name}", @this);
                return false;
            }

            for (var i = 0; i != componentsCache.Count; i++)
            {
                var component = componentsCache[i];

                if (component == null)
                    Debug.LogWarning($"The referenced script is missing on {@this.name}", @this);
                else if (!component.IsComponentDisabled() && !(component is GameObjectEntity))
                {
                    var componentType =
                        (component as ComponentDataProxyBase)?.GetComponentType() ?? component.GetType();
                    var isUniqueType = true;
                    for (var j = 0; j < outputIndex; ++j)
                    {
                        if (componentTypes[j].Equals(componentType))
                        {
                            isUniqueType = false;
                            break;
                        }
                    }

                    if (!isUniqueType)
                        continue;

                    componentsCache[outputIndex] = component;
                    componentTypes[outputIndex] = componentType;

                    outputIndex++;
                }
            }

            componentsCache.RemoveRange(outputIndex, componentsCache.Count - outputIndex);
            return true;
        }

        public static bool IsComponentDisabled(this Component @this)
        {
            switch (@this)
            {
                case Renderer r: return !r.enabled;
#if LEGACY_PHYSICS
                case Collider  c: return !c.enabled;
#endif
                case LODGroup l: return !l.enabled;
                case Behaviour b: return !b.enabled;
            }

            return false;
        }
    }
}