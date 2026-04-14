using UnityEngine;
using UnityEngine.UI;
using UniVue.UI;

namespace UniVue.UGUI
{
    public enum ScrollDirection
    {
        /// <summary>
        /// 水平
        /// </summary>
        Horizontal,

        /// <summary>
        /// 垂直
        /// </summary>
        Vertical
    }

    public abstract class LoopItem : BaseComponent
    {
        internal void Render(int index)
        {
            if (!UI.activeSelf)
                Show();

            OnRenderItem(index);
        }

        protected abstract void OnRenderItem(int index);
    }

    [RequireComponent(typeof(ScrollRect))]
    public abstract class Loopable : BaseComponent
    {
        [SerializeField] protected ScrollDirection _direction;

        private int _dataCount;

        /// <summary>
        /// 数据头指针，指向的是第一个Item的位置所渲染的数据的索引
        /// </summary>
        protected int _head;

        /// <summary>
        /// 采用脏标记模式解决快速拖动时留白渲染跟不上的问题
        /// </summary>
        private bool _isDirty;

        private Vector3[] _itemCorners;
        protected ScrollRect _scrollRect;

        /// <summary>
        /// 数据尾指针，指向的是最后一个Item的位置所渲染的数据的索引
        /// </summary>
        protected int _tail;

        private Vector3[] _viewportCorners;

        public ScrollRect ScrollRect
        {
            get
            {
                if (!_scrollRect) _scrollRect = GetComponent<ScrollRect>();
                return _scrollRect;
            }
        }

        public Scrollbar Scrollbar => _direction == ScrollDirection.Horizontal
            ? ScrollRect.horizontalScrollbar
            : ScrollRect.verticalScrollbar;

        /// <summary>
        /// 最大可见的Item的数量
        /// </summary>
        protected abstract int MaxViewCount { get; }

        protected int FirstIndex => 0;

        protected abstract int LastIndex { get; }

        public int ChildCount { get; private set; }

        /// <summary>
        /// 显示的数据的数量
        /// </summary>
        public int Count
        {
            get => _dataCount;
            set
            {
                _dataCount = value;
                Refresh(true);
            }
        }

        protected sealed override void OnInit()
        {
            _enableUpdate = true;
            _scrollRect = GetComponent<ScrollRect>();
            //防止BaseComponent的Show回调
            foreach (RectTransform item in _scrollRect.content) item.gameObject.SetActive(false);
            ChildCount = _scrollRect.content.childCount;

            //绑定滚动事件
            _itemCorners = new Vector3[4];
            _viewportCorners = new Vector3[4];
            _scrollRect.onValueChanged.AddListener(OnScroll);
            Scrollbar?.onValueChanged.AddListener(OnScroll);
        }

        protected override void OnUpdate(in float deltaTime)
        {
            if (!_isDirty) return;

            _scrollRect.viewport.GetWorldCorners(_viewportCorners);
            if (_direction == ScrollDirection.Vertical)
            {
                (_scrollRect.content.GetChild(FirstIndex) as RectTransform).GetWorldCorners(_itemCorners);
                if (_itemCorners[0].y > _viewportCorners[1].y)
                {
                    _isDirty = OnMoveItem(Direction.Up, _viewportCorners, _itemCorners);
                }
                else
                {
                    (_scrollRect.content.GetChild(LastIndex) as RectTransform).GetWorldCorners(_itemCorners);
                    if (_itemCorners[0].y < _viewportCorners[0].y)
                        _isDirty = OnMoveItem(Direction.Down, _viewportCorners, _itemCorners);
                }
            }
            else
            {
                (_scrollRect.content.GetChild(FirstIndex) as RectTransform).GetWorldCorners(_itemCorners);
                if (_itemCorners[3].x < _viewportCorners[1].x)
                {
                    _isDirty = OnMoveItem(Direction.Left, _viewportCorners, _itemCorners);
                }
                else
                {
                    (_scrollRect.content.GetChild(LastIndex) as RectTransform).GetWorldCorners(_itemCorners);
                    if (_itemCorners[3].x > _viewportCorners[3].x)
                        _isDirty = OnMoveItem(Direction.Right, _viewportCorners, _itemCorners);
                }
            }
        }

        protected sealed override void OnKill()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScroll);
            Scrollbar?.onValueChanged.RemoveListener(OnScroll);
        }

        private void OnScroll(float value)
        {
            _isDirty = true;
        }

        private void OnScroll(Vector2 _)
        {
            _isDirty = true;
        }

        /// <summary>
        /// 只刷新可以被看见的区域的数据
        /// </summary>
        protected void RefreshViewArea()
        {
            Transform content = _scrollRect.content;
            int len = content.childCount;
            int count = Count;
            _tail = _head;
            for (int i = 0; i < len; i++)
            {
                LoopItem item = content.GetChild(i).GetComponent<LoopItem>();
                if (_tail < count)
                    item.Render(_tail++);
                else
                    item.Hide();
            }

            --_tail;
        }

        /// <summary>
        /// 强制刷新
        /// </summary>
        protected void ForceRefresh()
        {
            _head = 0;
            _scrollRect.normalizedPosition = _direction == ScrollDirection.Vertical ? Vector2.up : Vector2.zero;
            ResetItemPos(Vector3.zero);
            RefreshViewArea();
        }

        /// <summary>
        /// 根据第一个Item的位置重新计算每个Item的位置
        /// </summary>
        /// <param name="firstItemPos">第一个Item的位置</param>
        protected abstract void ResetItemPos(Vector2 firstItemPos);

        /// <summary>
        /// 刷新视图
        /// </summary>
        /// <param name="force">是否为强制刷新</param>
        public abstract void Refresh(bool force = false);

        /// <summary>
        /// 重新计算ScrollRect的内容区域大小
        /// </summary>
        protected abstract void Resize();

        protected abstract bool OnMoveItem(Direction direction, Vector3[] viewportCorners, Vector3[] itemCorners);

        protected enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}