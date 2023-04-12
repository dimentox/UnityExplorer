using System;
using System.Collections;
using ECSExtension.Util;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UniverseLib;
using UniverseLib.UI.Widgets.ScrollView;

namespace ECSExtension.Panels
{
    public class EntityTree : ICellPoolDataSource<EntityCell>
    {
        private World world;
        private EntityQuery query;
        private JobHandle queryHandle;
        private NativeArray<Entity> entities;
        
        public ScrollPool<EntityCell> ScrollPool;
        
        private Coroutine refreshCoroutine;

        public int ItemCount => currentCount;
        private int currentCount;
        
        public Action<Entity> OnClickHandler;

        public EntityTree(ScrollPool<EntityCell> scrollPool, Action<Entity> onCellClicked)
        {
            ScrollPool = scrollPool;
            OnClickHandler = onCellClicked;
            ScrollPool.Initialize(this);
        }

        public void SetWorld(World world)
        {
            this.world = world;
            query =  world.EntityManager.UniversalQuery;
        }

        public void UseQuery(ComponentType[] componentTypes, bool includeDisabled)
        {
            query = world.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = componentTypes,
                Options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default
            });
        }

        public void RefreshData(bool jumpToTop)
        {
            if (refreshCoroutine != null || world == null)
                return;

            if (entities.IsCreated)
                entities.Dispose();
            
            entities = query.ToEntityArrayAsync(Allocator.Persistent, out queryHandle);
            refreshCoroutine = RuntimeHelper.StartCoroutine(RefreshCoroutine(jumpToTop));
        }

        private IEnumerator RefreshCoroutine(bool jumpToTop)
        {
            while (!queryHandle.IsCompleted)
                yield return null;

            queryHandle.Complete();
            currentCount = entities.Length;
            
            ScrollPool.Refresh(true, jumpToTop);
            refreshCoroutine = null;
        }
        
        public void SetCell(EntityCell cell, int index)
        {
            if (entities.IsCreated && index < entities.Length)
            {
                cell.ConfigureCell(entities[index], world.EntityManager);
            }
            else
                cell.Disable();
        }

        public void OnCellBorrowed(EntityCell cell)
        {
            cell.OnEntityClicked += OnEntityClicked;
            cell.onEnableClicked += OnEnableClicked;
        }

        private void OnEnableClicked(Entity entity, bool value)
        {
            if (world.EntityManager.Exists(entity))
            {
                world.EntityManager.SetEnabled(entity, value);
            }
        }

        private void OnEntityClicked(Entity obj)
        {
            Action<Entity> onClickHandler = OnClickHandler;
            if (onClickHandler == null)
                return;
            onClickHandler(obj);
        }
    }
}