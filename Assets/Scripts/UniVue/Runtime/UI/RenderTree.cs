using System;
using System.Collections.Generic;
using Framwork.Utils;
using UniVue.Internal;

namespace UniVue.UI
{
    /// <summary>
    /// 渲染树
    /// BaseUI
    ///    |--- RenderFunc
    ///            |------- Model
    ///            |          |------ Property 
    ///            |--- Event
    /// </summary>
    public sealed class RenderTree
    {
        internal ulong Key { get; set; }

        private readonly Dictionary<ulong, Action> _renders = new();
        
        internal bool Render(in ulong renderKey)
        {
            if (!_renders.TryGetValue(renderKey, out Action render)) return false;
            render.Invoke();
            return true;
        }

        internal void AddRender(in ulong renderKey, Action render)
        {
            ExceptionUtils.ThrowIfTrue(_renders.ContainsKey(renderKey), "已经存在一个相同key的渲染");
            _renders.Add(renderKey, render);
        }

        internal void GetAllRenderChildrenKeysNonAlloc(HashSet<ulong> keys)
        {
            ExceptionUtils.ThrowIfArgNull(keys, nameof(keys));
            RedPointMgr context = UIMgr.UIRenderer.Context;
            foreach (ulong renderKey in _renders.Keys)
            {
                context.GetChildrenKeysNoneAlloc(renderKey, keys);
            }
        }
        
        internal bool Remove(in ulong renderKey)
        {
            return _renders.Remove(renderKey);
        }
        
        internal void RemoveAll()
        {
            _renders.Clear();
        }
    }
}