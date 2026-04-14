using System;
using System.Collections.Generic;
using UnityEngine;
using UniVue.UI;
using Object = UnityEngine.Object;

namespace UniVue.UGUI
{
    /// <summary>
    /// 将场景中的Canvas作为根节点，所有层级的渲染都使用此Canvas
    /// </summary>
    public sealed class DefaultUILayerMgr : IUILayerMgr
    {
        private readonly List<Transform> _layers = new(8);

        private DefaultUILayerMgr()
        {
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

        public GameObject GetLayerRoot(int layer)
        {
            string layerName = ((IUILayerMgr)this).GetLayerName(layer);
            foreach (Transform transform in _layers)
                if (transform.name == layerName)
                    return transform.gameObject;

            return CreateNewLayer(layerName);
        }

        public GameObject Root { get; }
        public GameObject HideLayer { get; }

        internal void InitLayers(int count)
        {
            for (int i = 0; i < count; i++) CreateNewLayer(((IUILayerMgr)this).GetLayerName(i));
        }

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

            GameObject newLayerObj = new(layerName);
            newLayerObj.AddComponent<RectTransform>();
            Transform newLayer = newLayerObj.transform;
            newLayer.SetParent(root);
            newLayer.localScale = Vector3.one;
            newLayer.position = Vector3.zero;
            newLayer.rotation = Quaternion.identity;
            newLayer.localPosition = Vector3.zero;
            _layers.Add(newLayer);

            _layers.Sort((l1, l2) => string.Compare(l1.name, l2.name, StringComparison.Ordinal));

            for (int i = 0; i < _layers.Count; i++) _layers[i].SetSiblingIndex(i);

            HideLayer.transform.SetAsLastSibling();
            return newLayerObj;
        }
    }
}