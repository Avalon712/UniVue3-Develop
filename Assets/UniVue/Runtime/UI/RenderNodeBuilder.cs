using System;
using System.Collections.Generic;
using UniVue.Common;
using UniVue.Event;
using UniVue.Internal;
using UniVue.Model;

namespace UniVue.UI
{
    public sealed class RenderNodeBuilder
    {
        private readonly HashSet<EventKey> _eventKeys = new(4);
        private readonly RenderGraph _graph;

        private readonly HashSet<BaseModel> _models = new(4);
        private readonly Dictionary<BaseModel, HashSet<string>> _properties = new(4);

        private Action _renderFunc;

        internal RenderNodeBuilder(RenderGraph renderGraph)
        {
            _graph = renderGraph;
        }

        public RenderNodeBuilder On(Action renderFunc)
        {
            if (renderFunc == null)
                throw new ArgumentNullException(nameof(renderFunc));
            _renderFunc += renderFunc;
            return this;
        }

        public RenderNodeBuilder On(in EventKey eventKey)
        {
            if (eventKey.Type != EventKeyType.NotEventKey) _eventKeys.Add(eventKey);
            return this;
        }

        [InternalParamsGCOptimization]
        public RenderNodeBuilder On(params EventKey[] eventKeys)
        {
            foreach (EventKey eventKey in eventKeys) On(eventKey);
            return this;
        }

        public RenderNodeBuilder On(in Params<EventKey> eventKeys)
        {
            foreach (EventKey eventKey in eventKeys) On(eventKey);
            return this;
        }

        public RenderNodeBuilder On(BaseModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            _models.Add(model);
            return this;
        }

        [InternalParamsGCOptimization]
        public RenderNodeBuilder On(BaseModel model, params string[] properties)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            _models.Add(model);
            if (properties == null || properties.Length <= 0) return this;

            if (!_properties.TryGetValue(model, out HashSet<string> propList))
            {
                propList = InternalObjectPool<HashSet<string>>.Shared.Rent();
                propList.Clear();
                _properties[model] = propList;
            }

            foreach (string prop in properties)
            {
                if (string.IsNullOrEmpty(prop)) continue;
                propList.Add(prop);
            }

            return this;
        }

        public RenderNodeBuilder On(BaseModel model, in Params<string> properties)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            _models.Add(model);
            if (properties.Length <= 0) return this;

            if (!_properties.TryGetValue(model, out HashSet<string> propList))
            {
                propList = InternalObjectPool<HashSet<string>>.Shared.Rent();
                propList.Clear();
                _properties[model] = propList;
            }

            foreach (string prop in properties)
            {
                if (string.IsNullOrEmpty(prop)) continue;
                propList.Add(prop);
            }

            return this;
        }

        public void Build()
        {
            if (_renderFunc == null)
                throw new InvalidOperationException("Render function is not set. Call On(Action) before Build().");

            if (_models.Count <= 0 && _eventKeys.Count <= 0)
            {
                _eventKeys.Clear();
                _properties.Clear();
                _renderFunc = null;
                return;
            }

            //构建有向无环图

            // 渲染节点
            RenderGraph.Node renderNode = RenderGraph.NewNode(RenderGraph.NodeType.Render, _renderFunc);

            // 构建模型节点
            foreach (BaseModel model in _models)
            {
                if (!_graph.EventOrModelNodes.TryGetValue(model, out RenderGraph.Node modelNode))
                {
                    modelNode = RenderGraph.NewNode(RenderGraph.NodeType.Model, model);
                    _graph.EventOrModelNodes[modelNode.Model] = modelNode;
                }

                if (_properties.TryGetValue(model, out HashSet<string> properties))
                {
                    // 构建属性节点
                    foreach (string propertyName in properties)
                    {
                        if (!modelNode.next.TryGetValue(propertyName, out RenderGraph.Node propertyNode))
                        {
                            propertyNode = RenderGraph.NewNode(RenderGraph.NodeType.Property, propertyName);
                            modelNode.next[propertyNode.PropertyName] = propertyNode;
                        }

                        propertyNode.next[_renderFunc] = renderNode;
                    }

                    properties.Clear();
                    InternalObjectPool<HashSet<string>>.Shared.Return(ref properties); //回收
                }
                else
                {
                    modelNode.next[_renderFunc] = renderNode;
                }
            }

            // 构建事件节点
            foreach (EventKey eventKey in _eventKeys)
            {
                if (!_graph.EventOrModelNodes.TryGetValue(eventKey, out RenderGraph.Node eventNode))
                {
                    eventNode = RenderGraph.NewNode(RenderGraph.NodeType.Event, eventKey);
                    _graph.EventOrModelNodes[eventNode.data] = eventNode;
                }

                eventNode.next[_renderFunc] = renderNode;
            }

            _models.Clear();
            _properties.Clear();
            _eventKeys.Clear();
            _renderFunc = null;

            UIMgr.Renderer.AddGraph(_graph);
        }
    }
}