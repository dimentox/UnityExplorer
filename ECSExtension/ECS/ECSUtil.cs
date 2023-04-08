using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace ECSExtension.Util
{
    public static class ECSUtil
    {
        public static int GetModTypeIndex<T>()
        {
            var index = SharedTypeIndex<T>.Ref.Data;

            if (index <= 0)
            {
                throw new ArgumentException($"Failed to get type index for {typeof(T).FullName}");
            }

            return index;
        }
        
        public static ComponentType ReadOnly<T>()
        {
            int typeIndex = GetModTypeIndex<T>();
            ComponentType componentType = ComponentType.FromTypeIndex(typeIndex);
            componentType.AccessModeType = ComponentType.AccessMode.ReadOnly;
            return componentType;
        }
        
        public static void SetEnabled(this EntityManager entityManager, Entity entity, bool enabled)
        {
            if (IsEntityEnabled(entityManager, entity) == enabled)
            {
                return;
            }
            ComponentType componentType = ComponentType.ReadWrite<Disabled>();
            if (entityManager.HasModComponent<LinkedEntityGroup>(entity))
            {
                NativeArray<Entity> entities = Reinterpret<LinkedEntityGroup, Entity>(entityManager.GetBuffer<LinkedEntityGroup>(entity)).ToNativeArray(Allocator.TempJob);
                if (enabled)
                {
                    entityManager.RemoveComponent(entities, componentType);
                }
                else
                {
                    entityManager.AddComponent(entities, componentType);
                }
                entities.Dispose();
                return;
            }
            if (!enabled)
            {
                entityManager.AddComponent(entity, componentType);
                return;
            }
            entityManager.RemoveComponent(entity, componentType);
        }
        
        public static string GetName(Entity entity, EntityManager entityManager)
        {
            entityManager.GetName(entity, out FixedString64Bytes fixedString);
            string name = fixedString.Value;
            return string.IsNullOrEmpty(name) ? entity.ToString() : name;
        }
        
        public static unsafe DynamicBuffer<U> Reinterpret<T, U>(DynamicBuffer<T> buffer) 
            where U : unmanaged
            where T : unmanaged
        {
            return new DynamicBuffer<U>(buffer.m_Buffer, buffer.m_InternalCapacity);
        }
        
        public static bool IsEntityEnabled(EntityManager entityManager, Entity entity)
        {
            return !entityManager.HasComponent(entity, ReadOnly<Disabled>());
        }
        
        public static unsafe T GetModComponentData<T>(this EntityManager entityManager, Entity entity)
        {
            int typeIndex = GetModTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccess();

            if (!dataAccess->HasComponent(entity, ComponentType.FromTypeIndex(typeIndex)))
            {
                throw new InvalidOperationException($"Tried to get component data for component {typeof(T).FullName}, which entity does not have!");
            }

            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteWriteDependency(typeIndex);
            }

            byte* ret = dataAccess->EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);

            return Unsafe.Read<T>(ret);
        }

        /// <summary>
        /// Set Component Data of type.
        /// This method will work on any type, including mod created ones
        /// </summary>
        /// <param name="entity">Target Entity</param>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="component">Component Data</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe void SetModComponentData<T>(this EntityManager entityManager, Entity entity, T component)
        {
            int typeIndex = GetModTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            var componentStore = dataAccess->EntityComponentStore;

            if (!dataAccess->HasComponent(entity, ComponentType.FromTypeIndex(typeIndex)))
            {
                throw new InvalidOperationException($"Tried to set component data for component {typeof(T).FullName}, which entity does not have!");
            }

            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteReadAndWriteDependency(typeIndex);
            }

            byte* writePtr = componentStore->GetComponentDataWithTypeRW(entity, typeIndex, componentStore->m_GlobalSystemVersion);
            Unsafe.Copy(writePtr, ref component);
        }
        
        public static unsafe bool HasModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetModTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccess();

            return dataAccess->HasComponent(entity, componentType);
        }
        
    }
}