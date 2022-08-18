using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Il2CppSystem;
using Il2CppSystem.Reflection;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;
using Console = System.Console;
using Object = Il2CppSystem.Object;

namespace ECSExtension
{
    public class EntityInspector : InspectorBase
    {
        public Entity currentEntity;
        public World currentWorld => Target as World;

        public GameObject Content;
        private ScrollPool<ECSComponentCell> componentScroll;
        private ScrollPool<EntityCell> entityScroll;
        private EntityList entityList;
        private ECSComponentList ecsComponentList;

        public List<Entity> currentPage = new List<Entity>();

        public const int pageLimit = 30;
        public int pageIndex = 0;
        public int maxPages;

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "EntityInspector", true, false, true, true, 5,
                new Vector4(4, 4, 4, 4), new Color(0.065f, 0.065f, 0.065f));

            var scrollObj = UIFactory.CreateScrollView(UIRoot, "EntityInspector", out Content, out var scrollbar,
                new Color(0.065f, 0.065f, 0.065f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 250, preferredHeight: 300, flexibleHeight: 0, flexibleWidth: 9999);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(Content, spacing: 3, padTop: 2, padBottom: 2, padLeft: 2, padRight: 2);

            ConstructLists();

            return UIRoot;
        }

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);

            Target = target as World;
            ExplorerCore.LogWarning($"Target is null: {Target == null}");
            
            string currentBaseTabText = $"[ECS] {SignatureHighlighter.Parse(Target.GetActualType(), false)}";
            Tab.TabText.text = currentBaseTabText;

            RuntimeHelper.StartCoroutine(InitCoroutine());
        }


        private IEnumerator InitCoroutine()
        {
            yield return null;
            
            UpdateComponents();

            LayoutRebuilder.ForceRebuildLayoutImmediate(InspectorPanel.Instance.ContentRect);
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
        }


        public override void Update()
        {
            if (!this.IsActive)
                return;
        }

        private NativeArray<ComponentType> GetComponents()
        {
            if (currentEntity != Entity.Null)
            {
                return currentWorld.EntityManager.GetComponentTypes(currentEntity);
            }

            return new NativeArray<ComponentType>();
        }
        
        private List<Entity> GetEntities()
        {
            return currentPage;
        }

        public void InspectEntity(Entity entity)
        {
            currentEntity = entity;
            UpdateComponents();
        }

        public IComponentData GetComponentData(ComponentType type)
        {
            if (currentEntity != Entity.Null)
            {
                try
                {
                    Type il2cppType = type.GetManagedType();
                    Type entityManagerType = Il2CppType.Of<EntityManager>();
                    MethodInfo methodInfo = entityManagerType.GetMethod(nameof(EntityManager.GetComponentData));
                    MethodInfo genericMethod = methodInfo.MakeGenericMethod(il2cppType);

                    IComponentData data = genericMethod.Invoke( currentWorld.EntityManager.BoxIl2CppObject(), new Il2CppReferenceArray<Object>(new[]
                    {
                        currentEntity.BoxIl2CppObject()
                    })).Cast<IComponentData>();
                    return data;
                }
                catch (System.Exception e)
                {
                    ExplorerCore.LogWarning($"Error getting component data {type.GetManagedType().ToString()}, message: {e.Message}, stacktrace:\n {e.StackTrace}");
                }
            }

            return null;
        }
        
        
        private void ConstructLists()
        {
            var defInspect = UIFactory.CreateButton(UIRoot, "InpsectButton", "Inspect");
            UIFactory.SetLayoutElement(defInspect.Component.gameObject, minHeight: 25, minWidth: 80);
            defInspect.OnClick += () =>
            {
                InspectorManager.Inspect(Target, null, false);
            };
            
            
            var listHolder = UIFactory.CreateUIObject("ListHolders", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(listHolder, false, true, true, true, 8, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(listHolder, minHeight: 150, flexibleWidth: 9999, flexibleHeight: 9999);

            // Left group (Children)

            var leftGroup = UIFactory.CreateUIObject("EntitiesGroup", listHolder);
            UIFactory.SetLayoutElement(leftGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(leftGroup, false, false, true, true, 2);

            var childrenLabel = UIFactory.CreateLabel(leftGroup, "EntitiesListTitle", "Entities", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(childrenLabel.gameObject, flexibleWidth: 9999);
            
            // Add Child
            var addChildRow = UIFactory.CreateUIObject("AddEntityPageRow", leftGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addChildRow, false, false, true, true, 2);

            //addChildInput = UIFactory.CreateInputField(addChildRow, "AddChildInput", "Enter a name...");
           // UIFactory.SetLayoutElement(addChildInput.Component.gameObject, minHeight: 25, preferredWidth: 9999);

            var prevButton = UIFactory.CreateButton(addChildRow, "PrevPageButton", "Prev");
            UIFactory.SetLayoutElement(prevButton.Component.gameObject, minHeight: 25, minWidth: 80);
            prevButton.OnClick += () =>
            {
                pageIndex -= 1;
                if (pageIndex < 0) pageIndex = 0;
                UpdateComponents();
            };
            
            var spacer = UIFactory.CreateUIObject("Spacer", addChildRow);
            UIFactory.SetLayoutElement(spacer, preferredWidth: 9999);

            var nextButton = UIFactory.CreateButton(addChildRow, "NextPageButton", "Next");
            UIFactory.SetLayoutElement(nextButton.Component.gameObject, minHeight: 25, minWidth: 80);
            nextButton.OnClick += () =>
            {
                pageIndex += 1;
                if (pageIndex > maxPages) pageIndex = maxPages - 1;
                UpdateComponents();
            };
            
            
            entityScroll = UIFactory.CreateScrollPool<EntityCell>(leftGroup, "EntitiesList", out GameObject compObj1,
                out GameObject compContent1, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(compObj1, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(compContent1, flexibleHeight: 9999);

            entityList = new EntityList(entityScroll, GetEntities);
            entityList.Parent = this;
            entityScroll.Initialize(entityList);
            

            // Right group (Components)

            var rightGroup = UIFactory.CreateUIObject("ComponentGroup", listHolder);
            UIFactory.SetLayoutElement(rightGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(rightGroup, false, false, true, true, 2);

            var compLabel = UIFactory.CreateLabel(rightGroup, "CompListTitle", "Components", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(compLabel.gameObject, flexibleWidth: 9999);

            // Add Child
            var addComponentRow = UIFactory.CreateUIObject("AddComponentPageRow", rightGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addComponentRow, false, false, true, true, 2);
            
            var spacer1 = UIFactory.CreateUIObject("Spacer", addComponentRow);
            UIFactory.SetLayoutElement(spacer1, preferredWidth: 15000);
            
            componentScroll = UIFactory.CreateScrollPool<ECSComponentCell>(rightGroup, "ComponentList", out GameObject compObj,
                out GameObject compContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(compObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(compContent, flexibleHeight: 9999);

            ecsComponentList = new ECSComponentList(componentScroll, GetComponents);
            ecsComponentList.Parent = this;
            componentScroll.Initialize(ecsComponentList); 
        }

        public override void CloseInspector()
        {
            InspectorManager.ReleaseInspector(this);
        }

        public void UpdateComponents()
        {
            NativeArray<Entity> entites =  currentWorld.EntityManager.GetAllEntities();

            maxPages = Mathf.CeilToInt(entites.Length / (float)pageLimit);
            if (pageIndex > maxPages)
            {
                pageIndex = maxPages - 1;
            }

            int startIndex = Mathf.Max(0, pageIndex * pageLimit);
            int endIndex = Mathf.Min(entites.Length, (pageIndex + 1) * pageLimit);

            currentPage.Clear();
            
            for (int i = startIndex; i < endIndex; i++)
            {
                currentPage.Add(entites[i]);
            }

            ecsComponentList.RefreshData();
            ecsComponentList.ScrollPool.Refresh(true);

            entityList.RefreshData();
            entityList.ScrollPool.Refresh(true);
        }
    }
}