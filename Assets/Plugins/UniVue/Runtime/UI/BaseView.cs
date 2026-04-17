using System;
using System.Collections.Generic;
using Framwork.Utils;
using UnityEngine;
using UniVue.Common;
using UniVue.Internal;

namespace UniVue.UI
{
    public abstract class BaseView : BaseUI
    {
        public readonly int Layer;

        /// <summary>
        /// OnCreate时记录所有最初子界面、组件的状态，等待下一次重新打开时恢复为创建时的状态
        /// </summary>
        private Dictionary<BaseUI, bool> _recordCreateStatus;

        private List<BaseUI> _viewUIs;

        protected BaseView()
        {
            ViewName = GetType().Name;
        }

        /// <summary>
        /// 父界面，如果为Null，则说明是根视图
        /// </summary>
        public BaseView Parent { get; private set; }

        /// <summary>
        /// 子界面的生命周期由父界面管理
        /// </summary>
        public IEnumerable<BaseView> ChildViews
        {
            get
            {
                if (_viewUIs == null) yield break;
                foreach (BaseUI ui in _viewUIs)
                    if (ui is BaseView view)
                        yield return view;
            }
        }

        /// <summary>
        /// 当前View的所有组件
        /// </summary>
        public IEnumerable<BaseComponent> Components
        {
            get
            {
                if (_viewUIs == null) yield break;
                foreach (BaseUI ui in _viewUIs)
                    if (ui is BaseComponent component)
                        yield return component;
            }
        }

        public string ViewName { get; private set; }

        /// <summary>
        /// 如果是子view，则打开的参数是来自父界面
        /// </summary>
        protected object[] Args { get; private set; }

        /// <summary>
        /// true-打开状态  false-关闭状态
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// 打开静态子界面，即在UI预制体中就已经包含了这个界面
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="callback">界面打开完成后回调</param>
        /// <param name="args"></param>
        public void OpenChildView(string viewName, Action callback = null, params object[] args)
        {
            CheckDisposedAndInitialized();
            if (Disposed) return;
            foreach (BaseView childView in ChildViews)
                if (childView.ViewName == viewName)
                {
                    childView.OnOpenInternal(args);
                    callback?.Invoke();
                    return;
                }
        }

        /// <summary>
        /// 动态打开子界面，这个子界面是后面动态加载的（如果已经加载过这个类型的界面则不会再加载）
        /// </summary>
        /// <param name="mountNode">子界面的挂载点，如果为null，则挂载到UI节点</param>
        /// <param name="callback">界面打开完成后回调</param>
        /// <param name="args">界面传递参数</param>
        /// <typeparam name="T">界面类型（GameObject身上没有时会自动挂载此脚本）</typeparam>
        public void OpenChildView<T>(Transform mountNode = null, Action callback = null, params object[] args)
            where T : BaseView
        {
            CheckDisposedAndInitialized();
            if (Disposed) return;
            foreach (BaseView childView in ChildViews)
                if (childView.GetType() == typeof(T))
                {
                    childView.OnOpenInternal(args);
                    callback?.Invoke();
                    return;
                }

            if (mountNode && mountNode.TryGetComponent(out BaseView child))
            {
                LogUtil.Warn($"挂载点是一个子View，将调用子View{child.ViewName}[{child.GetType().FullName}]的OpenChildView<T>()方法实现");
                child.OpenChildView<T>(null, callback, args);
                return;
            }

            IUIPrefabLoader loader = UIMgr.Loader;
            loader.LoadUIPrefabAsync(typeof(T), viewPrefab =>
            {
                if (!viewPrefab)
                {
                    LogUtil.Exception(new Exception($"加载UI预制体失败，界面类型：{typeof(T).FullName}"));
                    return;
                }

                GameObject viewObj =
                    GameObjectUtils.RectTransformClone(viewPrefab, !mountNode ? UI.transform : mountNode);
                viewObj.name = viewPrefab.name;
                BaseView childView = viewObj.GetComponent<BaseView>();
                if (!childView)
                    viewObj.AddComponent<BaseView>();
                childView.Parent = this;
                childView.ViewName = viewObj.name;
                _viewUIs.Add(childView);
                childView.OnCreateInternal(viewObj);
                childView.OnOpenInternal(args);
                callback?.Invoke();
            });
        }

        /// <summary>
        /// 动态添加组件
        /// </summary>
        /// <param name="mountNode">挂载点，这个挂载点必须属性当前UI节点</param>
        /// <typeparam name="T">组件类型，如果GameObject身上没有挂载此组件则会自动添加此组件</typeparam>
        public void AddComponent<T>(Transform mountNode = null) where T : BaseComponent
        {
            CheckDisposedAndInitialized();
            if (Disposed) return;

            if (mountNode && mountNode.TryGetComponent(out BaseView child))
            {
                LogUtil.Warn($"挂载点是一个子View，将调用子View{child.ViewName}[{child.GetType().FullName}]的AddComponent<T>()方法实现");
                child.AddComponent<T>();
                return;
            }

            IUIPrefabLoader loader = UIMgr.Loader;
            loader.LoadUIPrefabAsync(typeof(T), uiPrefab =>
            {
                if (!uiPrefab)
                {
                    LogUtil.Exception(new Exception($"加载UI预制体失败，界面类型：{typeof(T).FullName}"));
                    return;
                }

                GameObject uiObj = GameObjectUtils.RectTransformClone(uiPrefab, !mountNode ? UI.transform : mountNode);
                BaseComponent component = uiObj.GetComponent<T>();
                if (!component)
                    component = uiObj.AddComponent<T>();

                _viewUIs.Add(component);
                component.View = this;
                component.OnCreateInternal(uiObj);
                component.Show();
            });
        }

        public void CloseChildView(string viewName)
        {
            CheckDisposedAndInitialized();
            if (Disposed) return;
            foreach (BaseView childView in ChildViews)
                if (childView.ViewName == viewName)
                {
                    childView.OnCloseInternal();
                    return;
                }
        }

        /// <summary>
        /// 关闭当前界面
        /// </summary>
        public void Close()
        {
            CheckDisposedAndInitialized();
            if (!Parent)
                UIMgr.Close(ViewName);
            else
                OnCloseInternal();
        }

        #region 内部初始化、生命周期回调

        internal void OnOpenInternal(object[] args)
        {
            if (Status || Disposed) return;
            Enable = true;
            Args = args;
            Status = true;
            //恢复创建时的状态
            foreach (KeyValuePair<BaseUI, bool> kv in _recordCreateStatus)
            {
                BaseUI ui = kv.Key;
                bool active = kv.Value;
                if (!active) continue;
                if (ui is BaseView subView)
                    subView.OnOpenInternal(args);
                else if (ui is BaseComponent component)
                    component.Show();
            }

            OnOpen();
        }

        internal void OnCloseInternal()
        {
            if (!Status || Disposed) return;
            Enable = false;
            Status = false;
            KillAllCoroutines();
            KillAllTimers();
            //关闭所有子界面和组件
            foreach (BaseView childView in ChildViews) childView.OnCloseInternal();
            foreach (BaseComponent component in Components) component.Hide();
            OnClose();
        }

        #endregion

        #region 生命周期

        protected sealed override void OnCreate()
        {
            _viewUIs = InternalObjectPool<List<BaseUI>>.Shared.Rent();
            _recordCreateStatus = InternalObjectPool<Dictionary<BaseUI, bool>>.Shared.Rent();
            _recordCreateStatus.Clear();
            _viewUIs.Clear();

            //对所有的Component和ChildView执行初始化
            DoCreateInitialization(UI.transform);
        }

        private void DoCreateInitialization(Transform parent)
        {
            foreach (Transform child in parent)
                if (child.TryGetComponent(out BaseUI ui))
                {
                    _recordCreateStatus[ui] = child.gameObject.activeSelf;
                    _viewUIs.Add(ui);

                    if (ui is BaseView subView)
                    {
                        subView.Parent = this;
                        subView.ViewName = child.name; //子界面的名称和GameObject的名称一致
                        subView.OnCreateInternal(child.gameObject);
                    }
                    else
                    {
                        if (ui is BaseComponent component)
                            component.View = this;
                        ui.OnCreateInternal(child.gameObject);
                        DoCreateInitialization(child); //嵌套的组件或其他继承自BaseUI也属于当前界面，组件之间不存在父子关系
                    }
                }
                else
                {
                    DoCreateInitialization(child);
                }
        }

        protected sealed override void OnDispose()
        {
            foreach (BaseUI ui in _viewUIs)
            {
                ui.OnDisposeInternal();
                if (ui is BaseComponent component) component.View = null;
            }

            OnRelease();

            _recordCreateStatus.Clear();
            _viewUIs.Clear();
            InternalObjectPool<List<BaseUI>>.Shared.Return(_viewUIs);
            InternalObjectPool<Dictionary<BaseUI, bool>>.Shared.Return(_recordCreateStatus);
        }

        #endregion

        #region 暴露给子类的生命周期

        protected virtual void OnOpen()
        {
            UI.SetActive(true);
        }

        protected virtual void OnClose()
        {
            UI.SetActive(false);
        }

        protected virtual void OnRelease()
        {
        }

        #endregion
    }
}