namespace UniVue.UI
{
    public abstract class BaseComponent : BaseUI
    {
        /// <summary>
        /// 当前组件所属的界面
        /// </summary>
        public BaseView View { get; internal set; }

        /// <summary>
        /// 组件名称，即gameObject.name
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// 状态
        /// </summary>
        public bool Status { get; private set; }

        protected sealed override void OnCreate()
        {
            OnInit();
        }

        protected sealed override void OnDispose()
        {
            View = null;
            OnKill();
        }

        public void Show()
        {
            if (Status) return;
            CheckDisposedAndInitialized();
            RenderStatus = true;
            UI.SetActive(true);
            Status = true;
            OnShow();
        }

        public void Hide()
        {
            if (!Status) return;
            CheckDisposedAndInitialized();
            RenderStatus = false;
            UI.SetActive(false);
            Status = false;
            OnHide();
        }


#region 子类重写

        protected virtual void OnInit()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnKill()
        {
        }

#endregion
    }
}