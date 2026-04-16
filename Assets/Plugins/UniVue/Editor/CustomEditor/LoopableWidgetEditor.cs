using Framwork.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UniVue.UI.Widegts;

namespace UniVue.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Loopable), true)]
    internal sealed class LoopableWidgetEditor : UnityEditor.Editor
    {
        private LoopItem _prefab;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying) return;

            EditorGUILayout.BeginHorizontal();
            _prefab = EditorGUILayout.ObjectField("LoopItem Prefab", _prefab, typeof(LoopItem), true) as LoopItem;
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create Items"))
            {
                if (!_prefab)
                {
                    Debug.LogWarning("请先指定LoopItem Prefab");
                    return;
                }

                Undo.IncrementCurrentGroup();
                if (target is LoopList list)
                    CreateItems(list);
                else if (target is LoopGrid grid)
                    CreateItems(grid);
                Undo.SetCurrentGroupName("Create Loop Items");
            }

            Settings(target as Loopable);
        }

        private void Settings(Loopable loop)
        {
            ScrollRect scrollRect = loop.ScrollRect;
            if (scrollRect == null) return;

            ScrollDirection direction = ReflectionUtils.GetFieldValue<ScrollDirection>(loop, "_direction");
            RectTransform content = scrollRect.content;
            if (direction == ScrollDirection.Horizontal)
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 0.5f);
            }
            else
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0.5f, 1f);
            }

            if (loop is LoopList)
            {
                bool unlimitScroll = ReflectionUtils.GetFieldValue<bool>(loop, "_unlimitScroll");
                scrollRect.movementType =
                    unlimitScroll ? ScrollRect.MovementType.Unrestricted : ScrollRect.MovementType.Elastic;
            }

            foreach (RectTransform childItem in content) SetItemAnchorPos(childItem);
        }

        private void CreateItems(LoopList list)
        {
            ScrollRect scrollRect = list.ScrollRect;
            RectTransform content = scrollRect.content;

            ScrollDirection direction = ReflectionUtils.GetFieldValue<ScrollDirection>(list, "_direction");
            int viewCount = ReflectionUtils.GetFieldValue<int>(list, "_viewCount");
            float distance = ReflectionUtils.GetFieldValue<float>(list, "_distance");
            Vector2 deltaPos = direction == ScrollDirection.Vertical
                ? new Vector3(0, distance, 0)
                : new Vector3(distance, 0, 0);
            int singal = direction == ScrollDirection.Vertical ? 1 : -1;

            Vector2 firstPos = Vector2.zero;
            bool linkedPrefab = TryGetLoopItemPrefabAssetRoot(out GameObject itemPrefabRoot);
            if (linkedPrefab && !itemPrefabRoot.GetComponentInChildren<LoopItem>(true))
            {
                Debug.LogWarning($"Prefab「{itemPrefabRoot.name}」中未找到 LoopItem 组件。");
                return;
            }

            Undo.RecordObject(content, "Create Loop Items");
            content.sizeDelta = deltaPos * (viewCount + 1);
            for (int i = 0; i <= viewCount; i++)
            {
                GameObject item = CreateItemOnContent(content, linkedPrefab, itemPrefabRoot, out LoopItem loopItem);
                item.name = $"{loopItem.GetType().Name} {i}";
                SetItemAnchorPos(item.transform as RectTransform);
                (content.GetChild(i) as RectTransform).anchoredPosition = firstPos - singal * i * deltaPos;
                Undo.RegisterCreatedObjectUndo(item, "Create Loop Items");
            }

            MarkLoopItemsDirty(content);
        }

        private void CreateItems(LoopGrid grid)
        {
            ScrollRect scrollRect = grid.ScrollRect;
            RectTransform content = scrollRect.content;

            ScrollDirection direction = ReflectionUtils.GetFieldValue<ScrollDirection>(grid, "_direction");
            int rows = ReflectionUtils.GetFieldValue<int>(grid, "_rows");
            int cols = ReflectionUtils.GetFieldValue<int>(grid, "_cols");
            float xDeltaPos = ReflectionUtils.GetFieldValue<float>(grid, "_xDeltaPos");
            float yDeltaPos = ReflectionUtils.GetFieldValue<float>(grid, "_yDeltaPos");

            Vector3 deltaPos = direction == ScrollDirection.Vertical
                ? new Vector2(0, Mathf.Abs(yDeltaPos))
                : new Vector2(Mathf.Abs(xDeltaPos), 0);
            float temp = direction == ScrollDirection.Vertical ? rows : cols;
            bool linkedPrefab = TryGetLoopItemPrefabAssetRoot(out GameObject itemPrefabRoot);
            if (linkedPrefab && !itemPrefabRoot.GetComponentInChildren<LoopItem>(true))
            {
                Debug.LogWarning($"Prefab「{itemPrefabRoot.name}」中未找到 LoopItem 组件。");
                return;
            }

            Undo.RecordObject(content, "Create Loop Items");
            content.sizeDelta = (temp + 1) * deltaPos;

            int iMax = direction == ScrollDirection.Vertical ? rows : cols;
            int jMax = direction == ScrollDirection.Vertical ? cols : rows;

            for (int i = 0; i <= iMax; i++) //垂直滚动多一行
            for (int j = 0; j < jMax; j++)
            {
                GameObject item = CreateItemOnContent(content, linkedPrefab, itemPrefabRoot, out LoopItem loopItem);
                SetItemAnchorPos(item.transform as RectTransform);
                item.name = $"{loopItem.GetType().Name} {i}_{j}";
                Undo.RegisterCreatedObjectUndo(item, "Create Loop Items");
            }

            ReflectionUtils.InvokeMethod(grid, "ResetItemPos", Vector2.zero);
            MarkLoopItemsDirty(content);
        }

        /// <summary>
        /// 预制体模板：<see cref="PrefabUtility.InstantiatePrefab" />；非预制体（场景原型）：<see cref="GameObjectUtils.RectTransformClone" />。
        /// </summary>
        private GameObject CreateItemOnContent(Transform content, bool linkedPrefab, GameObject prefabAssetRoot,
                                               out LoopItem loopItem)
        {
            if (linkedPrefab)
            {
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefabAssetRoot, content);
                loopItem = item.GetComponentInChildren<LoopItem>(true);
                return item;
            }

            GameObject clone = GameObjectUtils.RectTransformClone(_prefab.gameObject, content);
            loopItem = clone.GetComponentInChildren<LoopItem>(true);
            return clone;
        }

        /// <summary>
        /// 解析 LoopItem 所在预制体资源的根节点，供 <see cref="PrefabUtility.InstantiatePrefab" /> 使用（保持嵌套引用）。
        /// </summary>
        private bool TryGetLoopItemPrefabAssetRoot(out GameObject prefabAssetRoot)
        {
            prefabAssetRoot = null;
            if (!_prefab)
                return false;
            GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(_prefab.gameObject);
            if (!source)
                source = _prefab.gameObject;
            prefabAssetRoot = source.transform.root.gameObject;
            return PrefabUtility.IsPartOfPrefabAsset(prefabAssetRoot);
        }

        private static void MarkLoopItemsDirty(RectTransform content)
        {
            EditorUtility.SetDirty(content.gameObject);
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                EditorUtility.SetDirty(prefabStage.prefabContentsRoot);
            else if (content.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(content.gameObject.scene);
        }

        private void SetItemAnchorPos(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
        }
    }
}