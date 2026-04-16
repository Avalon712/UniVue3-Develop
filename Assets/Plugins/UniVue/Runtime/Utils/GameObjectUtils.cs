using UnityEngine;

namespace Framwork.Utils
{
    public static class GameObjectUtils
    {
        public static void SetActive(GameObject obj, bool active)
        {
            if (obj.activeSelf != active) obj.SetActive(active);
        }

        /// <summary>
        /// 从自身开始查找一个指定名称的GameObject(深度优先)
        /// </summary>
        public static GameObject DepthFind(string name, GameObject self)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (self.name.Equals(name)) return self;

            int childNum = self.transform.childCount;
            for (int i = 0; i < childNum; i++)
            {
                GameObject curr = self.transform.GetChild(i).gameObject;
                if (curr.name.Equals(name)) return curr;

                GameObject obj = DepthFind(name, curr);
                if (obj != null) return obj;
            }

            return null;
        }

        public static GameObject RectTransformClone(GameObject prefab, Transform parent)
        {
            GameObject clone = GameObject.Instantiate(prefab, parent);

            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            RectTransform prefabRect = prefab.GetComponent<RectTransform>();

            cloneRect.pivot = prefabRect.pivot;
            cloneRect.anchorMax = prefabRect.anchorMax;
            cloneRect.anchorMin = prefabRect.anchorMin;
            cloneRect.anchoredPosition = prefabRect.anchoredPosition;
            cloneRect.anchoredPosition3D = prefabRect.anchoredPosition3D;
            cloneRect.offsetMax = prefabRect.offsetMax;
            cloneRect.offsetMin = prefabRect.offsetMin;
            cloneRect.sizeDelta = prefabRect.sizeDelta;
            cloneRect.localScale = prefabRect.localScale;
            clone.name = prefab.name;

            return clone;
        }
    }
}