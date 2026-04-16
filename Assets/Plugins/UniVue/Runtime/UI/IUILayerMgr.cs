using UnityEngine;

namespace UniVue.UI
{
    public interface IUILayerMgr
    {
        public GameObject Root { get; }

        /// <summary>
        /// 特殊层级，用于存放所有被关闭的根视图界面，等待销毁或被重新打开
        /// </summary>
        public GameObject HideLayer { get; }

        public string GetLayerName(int layer)
        {
            return $"Layer{layer}";
        }

        public GameObject GetLayerRoot(int layer)
        {
            return Root.transform.GetChild(layer).gameObject;
        }
    }
}