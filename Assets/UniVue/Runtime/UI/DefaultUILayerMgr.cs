using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniVue.UI
{
    /// <summary>
    /// 将场景中的Canvas作为根节点，所有层级的渲染都使用此Canvas
    /// </summary>
    public sealed class DefaultUILayerMgr : IUILayerMgr
    {
        private readonly List<Transform> _layers = new(8);

#if UNITY_EDITOR
        private DrivenRectTransformTracker _tracker;
#endif

        private DefaultUILayerMgr()
        {
#if UNITY_EDITOR
            _tracker = new DrivenRectTransformTracker();
#endif

            Canvas canvas = Object.FindObjectOfType<Canvas>(true);
            if (!canvas)
                throw new NullReferenceException("Canvas not found！默认的UILayerMgr的实现中，场景里应该存在一个主Canvas作为LayerMgr的根节点");
            Root = canvas.gameObject;
            Root.SetActive(true);

            HideLayer = new GameObject("Hide");
            HideLayer.AddComponent<RectTransform>();
            HideLayer.SetActive(false);
            HideLayer.hideFlags = HideFlags.HideInHierarchy;
            Transform hideLayerTransform = HideLayer.transform;
            hideLayerTransform.SetParent(canvas.transform);
            hideLayerTransform.localScale = Vector3.one;
            hideLayerTransform.position = Vector3.zero;
            hideLayerTransform.rotation = Quaternion.identity;
            hideLayerTransform.localPosition = Vector3.zero;
        }

        public static DefaultUILayerMgr Default { get; } = new();

        public string GetLayerName(int layer)
        {
            return $"Layer{layer}";
        }

        public GameObject GetLayerRoot(int layer)
        {
            string layerName = GetLayerName(layer);
            foreach (Transform transform in _layers)
            {
                if (transform.name == layerName)
                    return transform.gameObject;
            }

            return CreateNewLayer(layerName);
        }

        public GameObject Root { get; }
        public GameObject HideLayer { get; }

        private GameObject CreateNewLayer(string layerName)
        {
            _layers.Clear();
            Transform root = Root.transform;
            int layerCount = root.childCount;
            for (int i = 0; i < layerCount; i++)
            {
                Transform transform = root.GetChild(i);
                if (transform.gameObject == HideLayer) continue;
                _layers.Add(transform);
            }

            Canvas canvas = Root.GetComponent<Canvas>();
            GameObject newLayerObj = new(layerName);
            RectTransform newLayer = newLayerObj.AddComponent<RectTransform>();

            newLayer.SetParent(root);
            newLayer.anchorMin = new Vector2(0.5f, 0.5f);
            newLayer.anchorMax = new Vector2(0.5f, 0.5f);
            newLayer.anchoredPosition = Vector2.zero;
            newLayer.pivot = new Vector2(0.5f, 0.5f);
            newLayer.localScale = Vector3.one;
            newLayer.position = Vector3.zero;
            newLayer.rotation = Quaternion.identity;
            newLayer.localPosition = Vector3.zero;
            newLayer.sizeDelta = (canvas.transform as RectTransform).sizeDelta;
#if UNITY_EDITOR
            _tracker.Add(canvas, newLayer, DrivenTransformProperties.All);
#endif
            _layers.Add(newLayer);

            _layers.Sort((l1, l2) => string.Compare(l1.name, l2.name, StringComparison.Ordinal));

            for (int i = 0; i < _layers.Count; i++) _layers[i].SetSiblingIndex(i);

            HideLayer.transform.SetAsLastSibling();
            return newLayerObj;
        }
    }
}