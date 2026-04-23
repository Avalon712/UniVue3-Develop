using System.Collections;
using System.Collections.Generic;
using UniVue.Coroutine;
using UniVue.Event;
using UniVue.Internal;
using UniVue.Model;

namespace UniVue.UI
{
    public sealed class RenderContext
    {
        private readonly Dictionary<EventKey, HashSet<RenderGraph>> _eventGraphs = new(64);
        private readonly Dictionary<BaseModel, HashSet<RenderGraph>> _modelGraphs = new(64);

        /// <summary>
        /// 记录触发重新渲染的事件
        /// </summary>
        private readonly HashSet<EventKey> _needRerenderEvents = new(64);

        /// <summary>
        /// 记录触发重新渲染的属性
        /// </summary>
        private readonly Dictionary<BaseModel, HashSet<string>> _needRerenderProperties = new(64);

        private readonly CoroutineYieldHandleContext _renderCoroutineCtx =
            new(new List<YieldHandler> { new InternalYieldWaitSecondsTime() });

        private ulong _renderCoroutineId;

        internal RenderContext()
        {
            EventMgr.OnEvent += NotifyEventTriggered;
        }

        /// <summary>
        /// 渲染频率，单位秒，默认为0.1秒(即一秒10帧)，如果小于等于0则每帧调用。（也只有存在数据变化时才会真正调用）
        /// </summary>
        public float Frequency { get; set; } = 0.1f;

        /// <summary>
        /// 当前触发渲染的事件
        /// </summary>
        public EventKey CurrentTriggerRenderEvent { get; private set; }

        private IEnumerator Render()
        {
            while (true)
            {
                if (_needRerenderProperties.Count <= 0 && _needRerenderEvents.Count <= 0) yield return null;

                yield return Frequency;

                using InternalTempCollection<HashSet<RenderGraph.Node>, RenderGraph.Node> recordHadRenderedNodes =
                    new(null);
                using InternalTempCollection<List<BaseModel>, BaseModel> visitedModels =
                    new(_needRerenderProperties.Keys);
                using InternalTempCollection<List<EventKey>, EventKey> visitedEventKeys = new(_needRerenderEvents);

                foreach (BaseModel model in visitedModels)
                {
                    if (!_needRerenderProperties.TryGetValue(model, out HashSet<string> propertyNames)) continue;

                    if (_modelGraphs.TryGetValue(model, out HashSet<RenderGraph> graphs))
                    {
                        using InternalTempCollection<HashSet<RenderGraph>, RenderGraph> copyGraphs = new(graphs);
                        using InternalTempCollection<HashSet<string>, string> copyPropertyNames = new(propertyNames);
                        foreach (RenderGraph graph in copyGraphs)
                            Render(graph, copyPropertyNames, model, recordHadRenderedNodes.Collection);
                        foreach (string propertyName in copyPropertyNames)
                            propertyNames.Remove(propertyName);
                    }
                }

                foreach (BaseModel model in visitedModels)
                {
                    if (!_needRerenderProperties.TryGetValue(model, out HashSet<string> propertyNames)) continue;
                    if (propertyNames.Count <= 0)
                    {
                        _needRerenderProperties.Remove(model);
                        InternalObjectPool<HashSet<string>>.Shared.Return(ref propertyNames);
                    }
                }

                foreach (EventKey eventKey in visitedEventKeys)
                {
                    if (!_needRerenderEvents.Remove(eventKey)) continue;
                    if (!_eventGraphs.TryGetValue(eventKey, out HashSet<RenderGraph> graphs)) continue;
                    CurrentTriggerRenderEvent = eventKey;
                    foreach (RenderGraph graph in graphs) Render(graph, eventKey, recordHadRenderedNodes.Collection);
                    CurrentTriggerRenderEvent = new EventKey();
                }
            }
        }

        private void NotifyEventTriggered(EventKey eventKey)
        {
            if (eventKey.Type == EventKeyType.NotEventKey || !_eventGraphs.TryGetValue(eventKey, out _)) return;
            if (_renderCoroutineId == 0) _renderCoroutineId = CoroutineMgr.Run(Render(), _renderCoroutineCtx);
            _needRerenderEvents.Add(eventKey);
        }

        private void NotifyPropertyChanged(BaseModel model, string propertyName, object propertyValue)
        {
            if (!_modelGraphs.TryGetValue(model, out _)) return;
            if (_renderCoroutineId == 0) _renderCoroutineId = CoroutineMgr.Run(Render(), _renderCoroutineCtx);
            if (!_needRerenderProperties.TryGetValue(model, out HashSet<string> propertyNames))
            {
                propertyNames = InternalObjectPool<HashSet<string>>.Shared.Rent();
                propertyNames.Clear();
                _needRerenderProperties[model] = propertyNames;
            }

            propertyNames.Add(propertyName);
        }

        private void Render(RenderGraph graph, HashSet<string> propertyNames, BaseModel model,
                            HashSet<RenderGraph.Node> recordHadRenderedNodes)
        {
            if (!graph.Enable && graph.Dirty) return;
            if (!graph.EventOrModelNodes.TryGetValue(model, out RenderGraph.Node modelNode)) return;

            using InternalTempCollection<HashSet<RenderGraph.Node>, RenderGraph.Node> renderNodes = new(null);

            foreach (RenderGraph.Node next in modelNode.next.Values)
                if (next.type == RenderGraph.NodeType.Render)
                    renderNodes.Collection.Add(next);

            foreach (string propertyName in propertyNames)
                if (modelNode.next.TryGetValue(propertyName, out RenderGraph.Node propNode))
                    foreach (RenderGraph.Node next in propNode.next.Values)
                        if (next.type == RenderGraph.NodeType.Render)
                            renderNodes.Collection.Add(next);

            foreach (RenderGraph.Node renderNode in renderNodes)
            {
                if (renderNode.RenderFunc == null || recordHadRenderedNodes.Contains(renderNode)) continue;
                if (!graph.Enable)
                {
                    graph.Dirty = true;
                    return;
                }

                renderNode.RenderFunc.Invoke();
                recordHadRenderedNodes.Add(renderNode);
            }
        }

        private void Render(RenderGraph graph, in EventKey eventKey, HashSet<RenderGraph.Node> recordHadRenderedNodes)
        {
            if (!graph.Enable && graph.Dirty) return;
            if (!graph.EventOrModelNodes.TryGetValue(eventKey, out RenderGraph.Node eventNode)) return;

            using InternalTempCollection<HashSet<RenderGraph.Node>, RenderGraph.Node> renderNodes = new(null);

            foreach (RenderGraph.Node next in eventNode.next.Values)
                if (next.type == RenderGraph.NodeType.Render)
                    renderNodes.Collection.Add(next);

            foreach (RenderGraph.Node renderNode in renderNodes)
            {
                if (renderNode.RenderFunc == null || recordHadRenderedNodes.Contains(renderNode)) continue;
                if (!graph.Enable)
                {
                    graph.Dirty = true;
                    return;
                }

                renderNode.RenderFunc.Invoke();
                recordHadRenderedNodes.Add(renderNode);
            }
        }

        internal void AddGraph(RenderGraph graph)
        {
            foreach (BaseModel model in graph.GetModels())
            {
                if (!_modelGraphs.TryGetValue(model, out HashSet<RenderGraph> models))
                {
                    models = InternalObjectPool<HashSet<RenderGraph>>.Shared.Rent();
                    models.Clear();
                    _modelGraphs[model] = models;
                    model.OnPropertyChanged += NotifyPropertyChanged;
                }

                models.Add(graph);
            }

            foreach (EventKey eventKey in graph.GetEventKeys())
            {
                if (!_eventGraphs.TryGetValue(eventKey, out HashSet<RenderGraph> events))
                {
                    events = InternalObjectPool<HashSet<RenderGraph>>.Shared.Rent();
                    events.Clear();
                    _eventGraphs[eventKey] = events;
                }

                events.Add(graph);
            }
        }

        internal void RemoveGraph(RenderGraph graph)
        {
            if (graph == null) return;

            using InternalTempCollection<List<BaseModel>, BaseModel> tempModels = new(_modelGraphs.Keys);
            using InternalTempCollection<List<EventKey>, EventKey> tempEventKeys = new(_eventGraphs.Keys);

            foreach (BaseModel model in tempModels)
            {
                if (!_modelGraphs.TryGetValue(model, out HashSet<RenderGraph> graphs) ||
                    !graphs.Remove(graph)) continue;

                if (graphs.Count > 0) continue;

                _modelGraphs.Remove(model);
                model.OnPropertyChanged -= NotifyPropertyChanged;
                InternalObjectPool<HashSet<RenderGraph>>.Shared.Return(ref graphs);
            }

            foreach (EventKey eventKey in tempEventKeys)
            {
                if (!_eventGraphs.TryGetValue(eventKey, out HashSet<RenderGraph> graphs) ||
                    !graphs.Remove(graph)) continue;

                if (graphs.Count > 0) continue;

                _eventGraphs.Remove(eventKey);
                InternalObjectPool<HashSet<RenderGraph>>.Shared.Return(ref graphs);
            }

            graph.Dispose();
        }

        /// <summary>
        /// 清空所有有关指定model的渲染
        /// </summary>
        /// <param name="model"></param>
        public void Clear(BaseModel model)
        {
            if (model == null || !_modelGraphs.Remove(model, out HashSet<RenderGraph> graphs)) return;
            foreach (RenderGraph graph in graphs)
                graph.Remove(model);
            graphs.Clear();
            if (_needRerenderProperties.Remove(model, out HashSet<string> propertyNames))
            {
                propertyNames.Clear();
                InternalObjectPool<HashSet<string>>.Shared.Return(ref propertyNames);
            }

            InternalObjectPool<HashSet<RenderGraph>>.Shared.Return(ref graphs);
            model.OnPropertyChanged -= NotifyPropertyChanged;
        }

        /// <summary>
        /// 清空所有监听了事件的渲染
        /// </summary>
        /// <param name="eventKey"></param>
        public void Clear(in EventKey eventKey)
        {
            if (eventKey.Type == EventKeyType.NotEventKey) return;
            if (!_eventGraphs.Remove(eventKey, out HashSet<RenderGraph> graphs)) return;
            foreach (RenderGraph graph in graphs) graph.Remove(eventKey);
            graphs.Clear();
            InternalObjectPool<HashSet<RenderGraph>>.Shared.Return(ref graphs);
        }

        public void ClearAll()
        {
            using InternalTempCollection<List<BaseModel>, BaseModel> tempModels = new(_modelGraphs.Keys);
            foreach (BaseModel model in tempModels) Clear(model);
            _modelGraphs.Clear();

            using InternalTempCollection<HashSet<EventKey>, EventKey> tempEventKeys = new(_eventGraphs.Keys);
            foreach (EventKey eventKey in tempEventKeys) Clear(eventKey);
            _eventGraphs.Clear();
        }
    }
}