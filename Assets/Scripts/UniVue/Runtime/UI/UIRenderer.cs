using System;
using System.Collections.Generic;
using Framwork.Utils;
using UniVue.Common;
using UniVue.Event;
using UniVue.Internal;
using UniVue.Model;

namespace UniVue.UI
{
    public sealed class UIRenderer
    {
        /// <summary>
        /// key = EventKey or BaseModel     value = 所有绑定的红点Key
        /// </summary>
        private readonly Dictionary<Index, HashSet<ulong>> _bindingKeys = new(64);
        
        /// <summary>
        /// key = 红点key   value = propertyName
        /// </summary>
        private readonly Dictionary<ulong, string> _bindingPropertyKeys = new(64);
        
        /// <summary>
        /// key = 红点树的根key   value = RenderTree
        /// </summary>
        private readonly Dictionary<ulong, RenderTree> _trees = new(64);
        
        private readonly RedPointMgr _renderStatusContext = new();

        internal RedPointMgr Context => _renderStatusContext;

        internal UIRenderer()
        {
            EventMgr.OnEvent += NotifyEventTrigger;
        }
        
        private void NotifyEventTrigger(EventKey eventKey)
        {
            
        }
        
        private void NotifyPropertyChanged(BaseModel model, string propertyName, object propertyValue)
        {
            
        }
        
        internal RenderTree Create(string uiName)
        {
            RenderTree tree = InternalObjectPool<RenderTree>.Shared.Rent();
            tree.Key = _renderStatusContext.CreateRedPointTree(RedPointRule.Or, uiName);
            _trees.Add(tree.Key, tree);
            return tree;
        }

        // internal void Rebuild(RenderTree tree, BaseModel model, string propertyName)
        // {
        //     
        // }
        //
        // internal void Rebuild(RenderTree tree, BaseModel model, string propertyName)
        // {
        //     
        // }
        
        internal void Destroy(ref RenderTree tree)
        {
            if(tree == null || !_trees.Remove(tree.Key)) return;
            
            ulong rootKey = tree.Key;
            foreach (HashSet<ulong> keys in _bindingKeys.Values)
            {
                using InternalTempCollection<HashSet<ulong>, ulong> copyKeys = new(keys);
                foreach (ulong key in copyKeys)
                    if (RedPointMgr.GetRootKey(key) == rootKey)
                        keys.Remove(key);
            }
            
            _renderStatusContext.DeleteDependency(rootKey);
            
            InternalObjectPool<RenderTree>.Shared.Return(tree);
            tree = null;
        }

        private void ClearAll(in Index index)
        {
            if (!_bindingKeys.Remove(index, out HashSet<ulong> keys)) return;
            foreach (ulong key in keys)
            {
                _renderStatusContext.DeleteDependency(key);
                
                ulong renderKey = RedPointMgr.GetParentKey(key);
                if (!_renderStatusContext.HaveChildren(renderKey)) continue;

                ulong rootKey = RedPointMgr.GetRootKey(key);
                if (_trees.TryGetValue(rootKey, out RenderTree tree))
                    tree.Remove(renderKey);
            }
            keys.Clear();
            InternalObjectPool<HashSet<ulong>>.Shared.Return(keys);
            
            if (index.model != null)
            {
                index.model.OnPropertyChanged -= NotifyPropertyChanged;
            }
        }
        
        private void Clear(in Index index, RenderTree tree)
        {
            if (!_bindingKeys.TryGetValue(index, out HashSet<ulong> keys)) return;
            using InternalTempCollection<HashSet<ulong>, ulong> copyKeys = new(keys);
            foreach (ulong key in copyKeys)
            {
                if(RedPointMgr.GetRootKey(key) != tree.Key) continue;
                keys.Remove(key);
                _renderStatusContext.DeleteDependency(key);
                
                ulong renderKey = RedPointMgr.GetParentKey(key);
                if (!_renderStatusContext.HaveChildren(renderKey))
                    tree.Remove(renderKey);
            }

            if (keys.Count <= 0)
                ClearAll(index);
        }
        
        private void Clear(RenderTree tree, BaseModel model, HashSet<ulong> properties)
        {
            if (!_bindingKeys.TryGetValue(model, out HashSet<ulong> keys) || properties.Count <= 0) return;
            ulong modelKey = 0;   
            foreach (ulong key in properties)
            {
                _renderStatusContext.DeleteDependency(key);

                if (modelKey == 0)
                   modelKey= RedPointMgr.GetParentKey(key);
            }

            if (!_renderStatusContext.HaveChildren(modelKey))
            {
                _renderStatusContext.DeleteDependency(modelKey);
                ulong renderKey = RedPointMgr.GetParentKey(modelKey);
                if (!_renderStatusContext.HaveChildren(renderKey))
                    tree.Remove(renderKey);
            }

            if (keys.Remove(modelKey) && keys.Count <= 0)
            {
                ClearAll(model);
            }
        }
        
        /// <summary>
        /// 清空EventKey的所有渲染函数
        /// </summary>
        /// <param name="eventKey"></param>
        public void Clear(in EventKey eventKey)
        {
            if(eventKey.Type == EventKeyType.NotEventKey) return;
            ClearAll(eventKey);
        }

        /// <summary>
        /// 清空Model的所有渲染函数
        /// </summary>
        /// <param name="model"></param>
        public void Clear(BaseModel model)
        {
            ExceptionUtils.ThrowIfArgNull(model, nameof(model));
            ClearAll(model);
        }

        /// <summary>
        /// 清空RenderTree上对EventKey的渲染函数
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="eventKey"></param>
        internal void Clear(RenderTree tree, in EventKey eventKey)
        {
            Clear(eventKey, tree);
        }

        /// <summary>
        /// 清空RenderTree上对Model的渲染函数
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="model"></param>
        internal void Clear(RenderTree tree, BaseModel model)
        {
            Clear(model, tree);
        }
        
        /// <summary>
        /// 清空RenderTree上对Model身上指定属性的渲染函数
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="model"></param>
        /// <param name="properties"></param>
        [InternalParamsGCOptimization]
        internal void Clear(RenderTree tree, BaseModel model, params string[] properties)
        {
            ExceptionUtils.ThrowIfNull(model, nameof(model));
            if(!TryGetModelKey(tree.Key, model, out ulong modelKey)) return;
            
            using InternalTempCollection<HashSet<ulong>, ulong> keys = new(null);
            _renderStatusContext.GetChildrenKeysNoneAlloc(modelKey, keys);
            using InternalTempCollection<HashSet<ulong>, ulong> copyKeys = new(keys);
            
            foreach (ulong key in copyKeys)
            {
                if (!_bindingPropertyKeys.TryGetValue(key, out string propertyName) ||
                    Array.IndexOf(properties, propertyName) < 0)
                {
                    keys.Collection.Remove(key);
                }
            }
            
            Clear(tree, model, keys);
        }

        /// <summary>
        /// 清空RenderTree上对Model身上指定属性的渲染函数
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="model"></param>
        /// <param name="properties"></param>
        internal void Clear(RenderTree tree, BaseModel model, in Params<string> properties)
        {
            ExceptionUtils.ThrowIfNull(model, nameof(model));
            if(!TryGetModelKey(tree.Key, model, out ulong modelKey)) return;
            
            using InternalTempCollection<HashSet<ulong>, ulong> keys = new(null);
            _renderStatusContext.GetChildrenKeysNoneAlloc(modelKey, keys);
            using InternalTempCollection<HashSet<ulong>, ulong> copyKeys = new(keys);
            
            foreach (ulong key in copyKeys)
            {
                if (!_bindingPropertyKeys.TryGetValue(key, out string propertyName) || properties.IndexOf(propertyName) < 0)
                {
                    keys.Collection.Remove(key);
                }
            }
            
            Clear(tree, model, keys);
        }

        private bool TryGetModelKey(ulong rootKey, BaseModel model, out ulong modelKey)
        {
            modelKey = 0;
            if (!_bindingKeys.TryGetValue(model, out HashSet<ulong> keys)) return false;
            foreach (ulong key in keys)
            {
                if (RedPointMgr.GetRootKey(key) != rootKey) continue;
                modelKey = key;
                return true;
            }
            return false;
        }
        
        private readonly struct Index : IEquatable<Index>
        {
            public readonly EventKey key;
            public readonly BaseModel model;
            public readonly Action renderFunc;
            
            /// <summary>
            /// 是否是一个有效的索引
            /// </summary>
            public bool Valid=> key.Type != EventKeyType.NotEventKey || model != null || renderFunc != null;

            private Index(EventKey key, BaseModel model, Action renderFunc)
            {
                this.key = key;
                this.model = model;
                this.renderFunc = renderFunc;
            }
            
            public Index(in EventKey key) : this(key, null, null) { }
            
            public Index(BaseModel model) : this(new  EventKey(), model, null) { }

            public Index(Action renderFunc) : this(new  EventKey(), null, renderFunc) { }
            
            public static implicit operator Index(in EventKey key)
            {
                return new Index(key);
            }

            public static implicit operator Index(BaseModel model)
            {
                return new Index(model);
            }
            
            public static implicit operator Index(Action renderFunc)
            {
                return new Index(renderFunc);
            }
            
            public bool Equals(Index other)
            {
                return key.Equals(other.key) && Equals(model, other.model) && Equals(renderFunc, other.renderFunc);
            }

            public override bool Equals(object obj)
            {
                return obj is Index other && Equals(other);
            }

            public override int GetHashCode()
            {
                if (key.Type != EventKeyType.NotEventKey)
                    return key.GetHashCode();
                if(model != null)
                    return model.GetHashCode();
                if (renderFunc != null)
                    return renderFunc.GetHashCode();
                return 0;
            }
        }
    }


}