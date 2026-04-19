using UnityEngine;
using static UnityEngine.UI.ScrollRect;

namespace UniVue.UI.Widegts
{
    [AddComponentMenu("UniVue/UI Widgets/LoopList")]
    public sealed class LoopList : Loopable
    {
        private int _targetIndex = -1; //要滚动到指定数据的目标位置
        private Vector2 _velocity; //滚动速度

        /// <summary>
        /// 相连两个Item之间的位置差
        /// </summary>
        private Vector2 deltaPos => _direction == ScrollDirection.Vertical
            ? new Vector2(0, _distance)
            : new Vector2(_distance, 0);

        protected override int MaxViewCount => _viewCount;

        protected override int LastIndex => ScrollRect.content.childCount - 2;

        /// <summary>
        /// 滚动到指定数据的位置
        /// </summary>
        public void ScrollTo(int index)
        {
            _targetIndex = index;
            if (_targetIndex != -1 && _head != _targetIndex)
            {
                int flag = _direction == ScrollDirection.Horizontal ? -1 : 1;
                Vector3 startPos = _scrollRect.content.anchoredPosition;
                if (flag == 1)
                    startPos.y = (_head * deltaPos).y;
                else
                    startPos.x = (_head * deltaPos).x * flag;

                Vector3 sumDeltaPos = deltaPos * ((_targetIndex - _head) * flag);
                Vector3 endPos = startPos + sumDeltaPos;

                //计算缓动时间
                float duration = _direction == ScrollDirection.Vertical
                    ? Mathf.Abs(sumDeltaPos.y / _distance) * _perItemScrollTime
                    : Mathf.Abs(sumDeltaPos.x / _distance) * _perItemScrollTime;

                _velocity = (endPos - startPos) / duration;
            }
        }

        protected override void OnUpdate(in float deltaTime)
        {
            base.OnUpdate(in deltaTime);
            //滚动动画的实现
            if (_targetIndex != -1)
            {
                if (_targetIndex >= _head && _targetIndex <= _tail)
                {
                    _velocity = Vector2.zero;
                    _targetIndex = -1;
                }

                _scrollRect.velocity = _velocity;
            }
        }

        public override void Refresh(bool force = false)
        {
            //重新计算Content的大小
            Resize();

            if (force)
            {
                ForceRefresh();
            }
            else
            {
                if (_alwaysShowNewestData && Count >= _viewCount)
                    DoRefresh_ShowLast();
                else
                    RefreshViewArea();
            }
        }

        protected override void Resize()
        {
            //会触发OnValueChanged事件
            _scrollRect.content.sizeDelta = deltaPos * Count;
            if (Count <= _viewCount)
                _scrollRect.movementType = MovementType.Clamped;
            else
                _scrollRect.movementType = _unlimitScroll ? MovementType.Unrestricted : MovementType.Elastic;
        }

        /// <summary>
        /// 刷新时总是显示最后那一个数据
        /// </summary>
        private void DoRefresh_ShowLast()
        {
            int startPtr = Count - _viewCount;
            _head = startPtr < 0 ? 0 : startPtr;
            _scrollRect.normalizedPosition = _direction == ScrollDirection.Vertical ? Vector2.zero : Vector2.right;
            int singal = _direction == ScrollDirection.Vertical ? 1 : -1;
            ResetItemPos(Vector2.zero - singal * _head * deltaPos);
            RefreshViewArea();
        }

        protected override void ResetItemPos(Vector2 firstPos)
        {
            int singal = _direction == ScrollDirection.Vertical ? 1 : -1;
            Transform content = _scrollRect.content;
            Vector2 deltaPos = this.deltaPos;
            int childCount = content.childCount;
            for (int i = 0; i < childCount; i++)
                (content.GetChild(i) as RectTransform).anchoredPosition = firstPos - singal * i * deltaPos;
        }

#region 滚动算法实现

        protected override bool OnMoveItem(Direction direction, Vector3[] viewportCorners, Vector3[] itemCorners)
        {
            Transform content = _scrollRect.content;
            int childCount = ChildCount;
            int dataCount = Count;
            RectTransform itemTrans = null;
            int index = -1;

            switch (direction)
            {
                case Direction.Up:
                    if ((_tail != Count - 1 || _unlimitScroll) && itemCorners[0].y > viewportCorners[1].y)
                    {
                        itemTrans = _scrollRect.content.GetChild(0) as RectTransform;
                        itemTrans.anchoredPosition =
                            (content.GetChild(childCount - 1) as RectTransform).anchoredPosition - deltaPos;
                        itemTrans.SetAsLastSibling(); //设置为最后一个位置

                        _tail = (_tail + 1) % dataCount;
                        _head = (_head + 1) % dataCount;
                        index = _tail;
                    }

                    break;
                case Direction.Down:
                    if ((_head != 0 || _unlimitScroll) && itemCorners[0].y < viewportCorners[0].y)
                    {
                        itemTrans = content.GetChild(childCount - 1) as RectTransform;
                        itemTrans.anchoredPosition = (content.GetChild(0) as RectTransform).anchoredPosition + deltaPos;
                        itemTrans.SetAsFirstSibling(); //最后一个设置为第一个位置

                        _tail = (_tail + dataCount - 1) % dataCount;
                        _head = (_head + dataCount - 1) % dataCount;
                        index = _head;
                    }

                    break;
                case Direction.Left:
                    if ((_tail != dataCount - 1 || _unlimitScroll) && itemCorners[3].x < viewportCorners[1].x)
                    {
                        itemTrans = content.GetChild(0) as RectTransform;
                        itemTrans.anchoredPosition =
                            (content.GetChild(childCount - 1) as RectTransform).anchoredPosition + deltaPos;
                        itemTrans.SetAsLastSibling(); //设置为最后一个位置

                        _tail = (_tail + 1) % dataCount;
                        _head = (_head + 1) % dataCount;
                        index = _tail;
                    }

                    break;
                case Direction.Right:
                    if ((_head != 0 || _unlimitScroll) && itemCorners[3].x > viewportCorners[3].x)
                    {
                        itemTrans = content.GetChild(childCount - 1) as RectTransform;
                        itemTrans.anchoredPosition = (content.GetChild(0) as RectTransform).anchoredPosition - deltaPos;
                        itemTrans.SetAsFirstSibling(); //最后一个设置为第一个位置

                        _tail = (_tail + dataCount - 1) % dataCount;
                        _head = (_head + dataCount - 1) % dataCount;
                        index = _head;
                    }

                    break;
            }

            if (index != -1)
            {
                LoopItem item = itemTrans.GetComponent<LoopItem>();
                OnItemRender?.Invoke(index, item);
            }

            return index != -1;
        }

#endregion

#region Unity - 组件配置参数

        [SerializeField] [Tooltip("可以看见的Item的最大数量")]
        private int _viewCount;

        [SerializeField] [Tooltip("相连两个item在滚动方向上的距离 垂直方向滚动:up.y-down.y  水平方向滚动:right.x-left.x")]
        private float _distance;

        [SerializeField] private bool _unlimitScroll;

        [SerializeField] private bool _alwaysShowNewestData;

        [SerializeField] [Tooltip("每滚动一个Item的距离使用的时间(秒)")]
        private float _perItemScrollTime = 0.1f;

#endregion
    }
}