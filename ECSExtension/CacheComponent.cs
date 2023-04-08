using System;
using CoreLib.Submodules.ModComponent;
using Unity.Entities;
using UnityExplorer;
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;

namespace ECSExtension
{
    public sealed class CacheComponent<T> : CacheObjectBase where T : unmanaged
    {
        private EntityManager entityManager;
        private Entity entity;

        public CacheComponent(EntityInspector inspector)
        {
            Owner = inspector;
            entityManager = inspector.currentWorld.EntityManager;
            entity = inspector.currentEntity;
            SetFallbackType(typeof(T));
        }

        public override bool ShouldAutoEvaluate => true;
        public override bool HasArguments => false;
        public override bool CanWrite => true;
        public override bool RefreshFromSource => true;


        public override void TrySetUserValue(object value)
        {
            if (value.GetType().IsAssignableTo(typeof(T)))
            {
                T component = (T)value;
                entityManager.SetModComponentData(entity, component);
            }
        }

        protected override bool TryAutoEvaluateIfUnitialized(CacheObjectCell objectcell)
        {
            CacheMemberCell cell = objectcell as CacheMemberCell;
            cell.EvaluateHolder.SetActive(false);

            if (State == ValueState.NotEvaluated)
                SetValueFromSource(TryEvaluate());

            return true;
        }

        public override object TryEvaluate()
        {
            try
            {
                return entityManager.GetModComponentData<T>(entity);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning(e);
            }

            return null;
        }
    }
}