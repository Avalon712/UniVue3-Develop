using System;
using UnityEngine;
using UniVue.UI;

namespace Game
{
    public class ReactiveTest : MonoBehaviour
    {
        // Start is called before the first frame update
        private void Start()
        {
            UIMgr.Initialize(new UIPrefabLoader(), DefaultLayerMgr.Default);
            
            UIMgr.Open<GMView>();
        }

        private sealed class UIPrefabLoader : IUIPrefabLoader
        {
            public void LoadUIPrefabAsync(Type uiType, Action<GameObject> callback)
            {
                LoadUIPrefabAsync(uiType.Name, callback);
            }

            public void LoadUIPrefabAsync(string uiName, Action<GameObject> callback)
            {
                ResourceRequest resourceRequest = Resources.LoadAsync<GameObject>(uiName);
                resourceRequest.completed += op =>
                {
                    if (op.isDone) callback.Invoke((GameObject)resourceRequest.asset);
                };
            }
        }
    }
}