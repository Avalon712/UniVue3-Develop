namespace UniVue.UI
{
    public abstract class BaseComponent : BaseUI
    {
        /// <summary>
        /// 当前组件所属的界面
        /// </summary>
        public BaseView View { get; internal set; }

        /// <summary>
        /// 状态
        /// </summary>
        public bool Status { get; private set; }

        public void Show()
        {
            if (Status) return;
            CheckDisposedAndInitialized();
            Enable = true;
            UI.SetActive(true);
            Status = true;
            OnShow();
        }

        public void Hide()
        {
            if (!Status) return;
            CheckDisposedAndInitialized();
            Enable = false;
            UI.SetActive(false);
            Status = false;
            OnHide();
        }


        #region 子类重写


        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        #endregion
    }
}