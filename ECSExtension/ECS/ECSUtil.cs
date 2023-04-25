using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace ECSExtension.Util
{
    public static unsafe class ECSUtil
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
        
        public const int ClearFlagsMask = 0x007FFFFF;
        public static ref readonly TypeManager.TypeInfo GetTypeInfo(int typeIndex)
        {
            return ref TypeManager.GetTypeInfoPointer()[typeIndex & ClearFlagsMask];
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
            ComponentType componentType = ReadOnly<Disabled>();
            if (entityManager.HasModComponent<LinkedEntityGroup>(entity))
            {
                NativeArray<Entity> entities = entityManager
                    .GetModBuffer<LinkedEntityGroup>(entity)
                    .Reinterpret<Entity>()
                    .ToIl2CppNativeArray(Allocator.TempJob);
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

        public static bool IsEntityEnabled(EntityManager entityManager, Entity entity)
        {
            return !entityManager.HasComponent(entity, ReadOnly<Disabled>());
        }
        
        /// <summary>
        /// Gets the dynamic buffer of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="isReadOnly">Specify whether the access to the component through this object is read only
        /// or read and write. </param>
        /// <typeparam name="T">The type of the buffer's elements.</typeparam>
        /// <returns>The DynamicBuffer object for accessing the buffer contents.</returns>
        /// <exception cref="ArgumentException">Thrown if T is an unsupported type.</exception>
        public static ModDynamicBuffer<T> GetModBuffer<T>(this EntityManager entityManager, Entity entity, bool isReadOnly = false) where T : unmanaged
        {
            var typeIndex = GetModTypeIndex<T>();
            var access = entityManager.GetCheckedEntityDataAccess();
            
            if (!access->IsInExclusiveTransaction)
            {
                if (isReadOnly)
                    access->DependencyManager->CompleteWriteDependency(typeIndex);
                else
                    access->DependencyManager->CompleteReadAndWriteDependency(typeIndex);
            }

            BufferHeader* header;
            if (isReadOnly)
            {
                header = (BufferHeader*) access->EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);
            }
            else
            {
                header = (BufferHeader*) access->EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex, access->EntityComponentStore->GlobalSystemVersion);
            }

            int internalCapacity = GetTypeInfo(typeIndex).BufferCapacity;
            return new ModDynamicBuffer<T>(header, internalCapacity);
        }

        public static T GetModComponentData<T>(this EntityManager entityManager, Entity entity)
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
        public static void SetModComponentData<T>(this EntityManager entityManager, Entity entity, T component)
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
        
        public static bool HasModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetModTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccess();

            return dataAccess->HasComponent(entity, componentType);
        }
        
    }
}