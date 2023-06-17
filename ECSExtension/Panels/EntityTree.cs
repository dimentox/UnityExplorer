using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ECSExtension.Util;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UniverseLib;
using UniverseLib.Runtime;
using UniverseLib.UI.Widgets.ScrollView;

namespace ECSExtension.Panels
{
    public class EntityTree : ICellPoolDataSource<EntityCell>
    {
        private World world;
        private EntityQuery query;
        private JobHandle queryHandle;
        private ModNativeArray<Entity> nativeArray;
        private IList<Entity> entities;
        private string currentFilter;
        
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

        public void UseQuery(ComponentType[] include, ComponentType[] exclude, bool includeDisabled)
        {
            query = world.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = include,
                None = exclude,
                Options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default
            });
        }

        public void SetFilter(string filter)
        {
            currentFilter = filter;
            ApplyFilter(true);
        }

        private void ApplyFilter(bool refresh)
        {
            if (string.IsNullOrEmpty(currentFilter))
                entities = nativeArray;
            else
                entities = nativeArray.Where(entity => ECSHelper.GetName(world.EntityManager, entity).Contains(currentFilter, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (refresh)
                ScrollPool.Refresh(true, true);
        }


        public void RefreshData(bool jumpToTop)
        {
            if (refreshCoroutine != null || world == null)
                return;

            if (nativeArray.IsCreated)
                nativeArray.Dispose();
            
            nativeArray = new ModNativeArray<Entity>(query.ToEntityArrayAsync(Allocator.Persistent, out queryHandle));
            entities = nativeArray;
            
            refreshCoroutine = RuntimeHelper.StartCoroutine(RefreshCoroutine(jumpToTop));
        }

        private IEnumerator RefreshCoroutine(bool jumpToTop)
        {
            while (!queryHandle.IsCompleted)
                yield return null;

            queryHandle.Complete();
            currentCount = nativeArray.Count;
            ApplyFilter(false);
            
            ScrollPool.Refresh(true, jumpToTop);
            refreshCoroutine = null;
        }
        
        public void SetCell(EntityCell cell, int index)
        {
            if (index < entities.Count)
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