using System;
using System.Collections;
using System.Collections.Generic;
using UniVue.Coroutine;
using UniVue.Event;
using UniVue.Internal;
using UniVue.Model;
using UniVue.Utils;

namespace UniVue.UI
{
    public sealed class RGraphs
    {
        private static readonly CoroutineYieldHandleContext _renderCtx =
            new(new List<YieldHandler>(1) { new InternalYieldWaitSecondsTime() });


        private readonly HashSet<RNode> _renderQueue = new(16);
        private CoroutineID _coroutineId;

        public RGraphs()
        {
            Entry = RGraph.Create();
            EventMgr.OnEvent += OnTriggerEvent;
        }

        internal RGraph Entry { get; private set; }

        /// <summary>
        /// 渲染间隔（秒），默认0.1秒渲染一次（如果有渲染事件触发）
        /// </summary>
        public float RenderInternal { get; set; } = 0.1f;

        private IEnumerator Render()
        {
            while (true)
            {
                yield return RenderInternal;
                using InternalTempCollection<HashSet<RNode>, RNode> queue = new(_renderQueue);
                _renderQueue.Clear();

                foreach (RNode rNode in queue)
                {
                    Action renderFn = rNode.Key.As<Action>();
                    if (rNode.Reachable && renderFn != null) renderFn.Invoke();
                }
            }
        }

        private void OnTriggerEvent(EventKey eventKey)
        {
            if (Entry.g == null || !Entry.g.next.TryGetValue(eventKey, out RNode mGraphs)) return;
            using InternalTempCollection<Dictionary<RKey, RNode>, KeyValuePair<RKey, RNode>> graphs = new(mGraphs.next);
            foreach (RNode graph in graphs.Collection.Values)
                graph.Visit(node =>
                {
                    if (node.Key.type == RKeyType.Event && node.Key.Equals(eventKey))
                        foreach (RKey key in node.next.Keys)
                            if (key.type == RKeyType.Rendering && key.As<Action>() != null)
                                _renderQueue.Add(node.next[key]);

                    return node.Key.type == RKeyType.Event || node.Key.type == RKeyType.Graph;
                });

            if (_coroutineId == 0) _coroutineId = CoroutineMgr.Run(Render(), _renderCtx);
        }

        private void OnNotifyPropertyChanged(BaseModel model, string propertyName, object _)
        {
            if (Entry.g == null || !Entry.g.next.TryGetValue(model, out RNode mGraphs)) return;
            using InternalTempCollection<Dictionary<RKey, RNode>, KeyValuePair<RKey, RNode>> graphs = new(mGraphs.next);

            foreach (RNode graph in graphs.Collection.Values)
                graph.Visit(node =>
                {
                    if (node.Key.type == RKeyType.Model && node.Key.Equals(model) &&
                        node.next.TryGetValue(propertyName, out RNode pNode))
                        foreach (RKey key in pNode.next.Keys)
                            if (key.type == RKeyType.Rendering && key.As<Action>() != null)
                                _renderQueue.Add(node.next[key]);

                    return node.Key.type == RKeyType.Model || node.Key.type == RKeyType.Graph;
                });

            if (_coroutineId == 0) _coroutineId = CoroutineMgr.Run(Render(), _renderCtx);
        }

        internal void AddNode(in RGraph graph, BaseModel model, Action renderFn)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (graph.g == null || model == null || renderFn == null) return;
            if (!Entry.g.next.TryGetValue(model, out RNode mGraphs))
            {
                mGraphs = RNode.Create();
                Entry.g.next[model] = mGraphs;
                mGraphs.Key = model;
                mGraphs.In = 1;
                model.OnPropertyChanged += OnNotifyPropertyChanged;
            }

            if (!mGraphs.next.ContainsKey(graph))
            {
                graph.g.In++;
                mGraphs.next[graph] = graph.g;
            }

            if (!graph.g.next.TryGetValue(model, out RNode mNode))
            {
                mNode = RNode.Create();
                mNode.Key = model;
                mNode.In = 1;
                graph.g.next[model] = mNode;
            }

            if (!mNode.next.TryGetValue(renderFn, out RNode rNode))
            {
                rNode = RNode.Create();
                rNode.Key = renderFn;
                rNode.In++;
                mNode.next[renderFn] = rNode;
            }
        }

        internal void AddNode(in RGraph graph, BaseModel model, Action renderFn, params string[] propertyNames)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (graph.g == null || model == null || renderFn == null) return;
            if (propertyNames == null || propertyNames.Length == 0)
            {
                AddNode(in graph, model, renderFn);
                return;
            }

            if (!Entry.g.next.TryGetValue(model, out RNode mGraphs))
            {
                mGraphs = RNode.Create();
                Entry.g.next[model] = mGraphs;
                mGraphs.Key = model;
                mGraphs.In = 1;
                model.OnPropertyChanged += OnNotifyPropertyChanged;
            }

            if (!mGraphs.next.ContainsKey(graph))
            {
                graph.g.In++;
                mGraphs.next[graph] = graph.g;
            }

            if (!graph.g.next.TryGetValue(model, out RNode mNode))
            {
                mNode = RNode.Create();
                mNode.Key = model;
                mNode.In = 1;
                graph.g.next[model] = mNode;
            }

            foreach (string propertyName in propertyNames)
            {
                if (!mNode.next.TryGetValue(propertyName, out RNode pNode))
                {
                    pNode = RNode.Create();
                    pNode.Key = propertyName;
                    pNode.In = 1;
                    mNode.next[propertyName] = pNode;
                }

                if (!pNode.next.TryGetValue(renderFn, out RNode rNode))
                {
                    rNode = RNode.Create();
                    rNode.Key = renderFn;
                    pNode.next[renderFn] = rNode;
                }

                rNode.In++;
            }
        }

        internal void AddNode(in RGraph graph, in EventKey eventKey, Action renderFn)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (graph.g == null || eventKey.Type == EventKeyType.NotEventKey || renderFn == null) return;
            if (!Entry.g.next.TryGetValue(eventKey, out RNode mGraphs))
            {
                mGraphs = RNode.Create();
                Entry.g.next[eventKey] = mGraphs;
                mGraphs.Key = eventKey;
                mGraphs.In = 1;
            }

            if (!mGraphs.next.ContainsKey(graph))
            {
                graph.g.In++;
                mGraphs.next[graph] = graph.g;
            }

            if (!graph.g.next.TryGetValue(eventKey, out RNode eNode))
            {
                eNode = RNode.Create();
                eNode.Key = eventKey;
                eNode.In = 1;
                graph.g.next[eventKey] = eNode;
            }

            if (!eNode.next.TryGetValue(renderFn, out RNode rNode))
            {
                rNode = RNode.Create();
                rNode.Key = renderFn;
                eNode.next[renderFn] = rNode;
            }

            rNode.In++;
        }

        internal void Remove(ref RGraph graph)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (graph.g == null) return;
            using InternalTempCollection<List<RKey>, RKey> keys = new(null);

            //收集所有可达RGraph节点的RKey
            graph.CollectRKeysNoneAlloc(RKeyType.Model | RKeyType.Event, keys);
            foreach (RKey key in keys)
            {
                if (!Entry.g.next.TryGetValue(key, out RNode node)) continue;
                if (node.next.Remove(graph) && node.Out <= 0) node.In = 0; //标记为不可达
            }

            RNode.ForceDispose(graph.g);
            graph = default;

            //收集那些从Entry不可达的节点进行回收
            keys.Collection.Clear();
            foreach (RNode node in Entry.g.next.Values)
                if (!node.Reachable)
                    keys.Collection.Add(node.Key);
            foreach (RKey key in keys)
            {
                if (!Entry.g.next.Remove(key, out RNode node)) continue;
                if (key.type == RKeyType.Model)
                    key.As<BaseModel>().OnPropertyChanged -= OnNotifyPropertyChanged;
                RNode.ForceDispose(node);
            }
        }

        /// <summary>
        /// 销毁所有渲染绑定，同时RGraphs不可再用
        /// </summary>
        internal void Dispose()
        {
            if (Entry.g == null) return;
            ClearAll();
            RNode.ForceDispose(Entry.g);
            Entry = default;
            EventMgr.OnEvent -= OnTriggerEvent;
            CoroutineMgr.Kill(_coroutineId);
            _coroutineId = 0;
            _renderQueue.Clear();
        }

        /// <summary>
        /// 清空所有渲染绑定
        /// </summary>
        public void ClearAll()
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            foreach (RNode node in Entry.g.next.Values)
            {
                if (node.Key.type == RKeyType.Model)
                    node.Key.As<BaseModel>().OnPropertyChanged -= OnNotifyPropertyChanged;
                RNode.ForceDispose(node);
            }
            _renderQueue.Clear();
            Entry.g.next.Clear();
        }

        /// <summary>
        /// 清空所有有关指定model的渲染
        /// </summary>
        /// <param name="model"></param>
        public void Clear(BaseModel model)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (model == null || !Entry.g.next.Remove(model, out RNode mGraphs)) return;

            model.OnPropertyChanged -= OnNotifyPropertyChanged;
            foreach (RNode graph in mGraphs.next.Values)
            {
                --graph.In;
                if (!graph.Reachable)
                    RNode.ForceDispose(graph);
                else
                    RNode.SafeDispose(graph.next[model]); //说明此Graph还可通过其他节点可达，不能强制释放        
            }

            mGraphs.next.Clear();
            RNode.ForceDispose(mGraphs);
        }


        /// <summary>
        /// 清空目标RGraph上所有model的渲染
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="model"></param>
        public void Clear(ref RGraph graph, BaseModel model)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (graph.g == null || model == null) return;
            if (!Entry.g.next.TryGetValue(model, out RNode mGraphs)) return;

            if (mGraphs.next.Remove(graph))
            {
                if (mGraphs.Out <= 0)
                {
                    model.OnPropertyChanged -= OnNotifyPropertyChanged;
                    Entry.g.next.Remove(model);
                    RNode.ForceDispose(mGraphs);
                }

                graph.g.In--;
                if (!graph.g.Reachable)
                {
                    RNode.ForceDispose(graph.g);
                    graph = default;
                }
                else
                {
                    if (graph.g.next.Remove(model, out RNode mNode)) RNode.SafeDispose(mNode);
                }
            }
        }

        /// <summary>
        /// 清空目标RGraph上model指定属性的渲染
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="model"></param>
        /// <param name="propertyNames">属性名称</param>
        public void Clear(ref RGraph graph, BaseModel model, params string[] propertyNames)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (graph.g == null || model == null) return;
            if (propertyNames == null || propertyNames.Length == 0)
            {
                Clear(ref graph, model);
            }
            else
            {
                if (!Entry.g.next.TryGetValue(model, out _)) return;

                if (!graph.g.next.TryGetValue(model, out RNode mNode)) return;

                foreach (string propertyName in propertyNames)
                    if (mNode.next.Remove(propertyName, out RNode pNode))
                        RNode.SafeDispose(pNode);

                if (mNode.Out <= 0)
                    Clear(ref graph, model);
            }
        }

        /// <summary>
        /// 清空所有监听了事件的渲染
        /// </summary>
        /// <param name="eventKey"></param>
        public void Clear(in EventKey eventKey)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (eventKey.Type == EventKeyType.NotEventKey || !Entry.g.next.Remove(eventKey, out RNode node)) return;

            foreach (RNode graph in node.next.Values)
            {
                --graph.In;
                if (!graph.Reachable)
                    RNode.ForceDispose(graph);
                else
                    RNode.SafeDispose(graph.next[eventKey]); //说明此Graph还可通过其他节点可达，不能强制释放        
            }

            node.next.Clear();
            RNode.ForceDispose(node);
        }

        /// <summary>
        /// 清空目标RGraph上所有eventKey的渲染
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="eventKey"></param>
        public void Clear(ref RGraph graph, in EventKey eventKey)
        {
            ExceptionUtils.ThrowIfNull(Entry.g, "RGraphs is disposed!");
            if (graph.g == null || eventKey.Type == EventKeyType.NotEventKey) return;

            if (!Entry.g.next.TryGetValue(eventKey, out RNode eGraphs)) return;

            if (eGraphs.next.Remove(graph))
            {
                if (eGraphs.Out <= 0)
                {

                    Entry.g.next.Remove(eventKey);
                    RNode.ForceDispose(eGraphs);
                }

                graph.g.In--;
                if (graph.g.Reachable)
                {
                    if (!graph.g.next.Remove(eventKey, out RNode eNode)) return;
                    RNode.SafeDispose(eNode);
                }
                else
                {
                    RNode.ForceDispose(graph.g);
                    graph = default;
                }
            }
        }
    }
}