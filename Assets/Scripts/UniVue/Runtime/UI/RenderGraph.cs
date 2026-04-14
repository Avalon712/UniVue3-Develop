using System;
using System.Collections.Generic;
using UniVue.Common;
using UniVue.Event;
using UniVue.Internal;
using UniVue.Model;

namespace UniVue.UI
{
    internal sealed class RenderGraph
    {
        public enum NodeType
        {
            None,

            /// <summary>
            /// RenderGraph根节点，后面可以跟Model节点和Event节点
            /// </summary>
            Graph,

            /// <summary>
            /// 模型节点，后面跟Property节点和Render节点
            /// </summary>
            Model,

            /// <summary>
            /// 事件节点，后面跟Render节点
            /// </summary>
            Event,

            /// <summary>
            /// 属性节点，后面跟Render节点
            /// </summary>
            Property,

            /// <summary>
            /// 渲染节点，图的末路节点
            /// </summary>
            Render
        }

        internal readonly RenderNodeBuilder builder;

        public RenderGraph()
        {
            builder = new RenderNodeBuilder(this);
            Root = NewNode(NodeType.Graph, null);
        }

        internal Dictionary<object, Node> EventOrModelNodes => Root.next;

        private Node Root { get; set; }

        internal bool Enable { get; set; }

        /// <summary>
        /// 收集所有类型为collectType的节点，如果collectType为NodeType.None，则收集所有节点
        /// </summary>
        private void CollectAllNode(Node node, NodeType collectType, HashSet<Node> nodes)
        {
            if (node == null) return;
            if (collectType == NodeType.None || node.type == collectType)
                nodes.Add(node);
            foreach (Node nextNode in node.next.Values) CollectAllNode(nextNode, collectType, nodes);
        }

        /// <summary>
        /// 检查不可达节点（即不在Root的子树上的节点）
        /// </summary>
        /// <param name="node"></param>
        /// <param name="checkNodes">所有要检查的节点，最后剩下的节点均是不可达节点</param>
        private void CheckNotReachableNode(Node node, HashSet<Node> checkNodes)
        {
            checkNodes.Remove(node);
            foreach (Node nextNode in node.next.Values) CheckNotReachableNode(nextNode, checkNodes);
        }

        private void Recycle(Node node, bool recycleRenderNode)
        {
            if (node == null || (node.type == NodeType.Render && !recycleRenderNode)) return;

            foreach (Node nextNode in node.next.Values) Recycle(nextNode, recycleRenderNode);

            RecycleImmediately(node);
        }

        /// <summary>
        /// 直接回收当前节点
        /// </summary>
        /// <param name="node"></param>
        private void RecycleImmediately(Node node)
        {
            if (node == null || node.Recycled) return;
            node.next.Clear();
            node.data = null;
            node.type = NodeType.None;
            InternalObjectPool<Node>.Shared.Return(node);
        }

        private void ClearRecycledNodeReference(Node node)
        {
            using InternalTempCollection<Dictionary<object, Node>, KeyValuePair<object, Node>> tempNodes =
                new(node.next);

            foreach (KeyValuePair<object, Node> kv in tempNodes)
            {
                if (kv.Value.Recycled) node.next.Remove(kv.Key);
                ClearRecycledNodeReference(kv.Value);
            }
        }

        public void Remove(Action renderFunc)
        {
            if (renderFunc == null) return;
            using InternalTempCollection<HashSet<Node>, Node> nodes = new(null);
            using InternalTempCollection<HashSet<Node>, Node> recycleNodes = new(null);

            CollectAllNode(Root, NodeType.None, nodes);

            foreach (Node node in nodes)
                if (node.next.Remove(renderFunc, out Node renderNode))
                    recycleNodes.Collection.Add(renderNode);

            foreach (Node renderNode in recycleNodes)
                RecycleImmediately(renderNode);

            ClearRecycledNodeReference(Root);
        }

        /// <summary>
        /// 移除模型的所有渲染函数
        /// </summary>
        /// <param name="model"></param>
        public void Remove(BaseModel model)
        {
            if (!EventOrModelNodes.TryGetValue(model, out Node modelNode)) return;
            using InternalTempCollection<HashSet<Node>, Node> nodes = new(null);
            CollectAllNode(Root, NodeType.Render, nodes);
            Recycle(modelNode, false);
            CheckNotReachableNode(Root, nodes);
            foreach (Node node in nodes) RecycleImmediately(node);
            ClearRecycledNodeReference(Root);
        }

        /// <summary>
        /// 移除模型指定属性的渲染函数（如果properties为null或长度为0，则移除模型的所有渲染函数）
        /// </summary>
        /// <param name="model"></param>
        /// <param name="properties"></param>
        public void Remove(BaseModel model, params string[] properties)
        {
            if (!EventOrModelNodes.TryGetValue(model, out Node modelNode)) return;

            if (properties == null || properties.Length <= 0)
            {
                Remove(model);
                return;
            }

            using InternalTempCollection<HashSet<Node>, Node> nodes = new(null);
            CollectAllNode(Root, NodeType.Render, nodes);
            foreach (string propertyName in properties)
            {
                if (string.IsNullOrEmpty(propertyName)) continue;
                if (modelNode.next.Remove(propertyName, out Node propNode))
                    RecycleImmediately(propNode);
            }

            CheckNotReachableNode(Root, nodes);
            foreach (Node node in nodes) RecycleImmediately(node);
            ClearRecycledNodeReference(Root);
        }

        /// <summary>
        /// 移除模型指定属性的渲染函数（如果properties为null或长度为0，则移除模型的所有渲染函数）（None GC Alloc）
        /// </summary>
        /// <param name="model"></param>
        /// <param name="properties"></param>
        public void Remove(BaseModel model, in Params<string> properties)
        {
            if (!EventOrModelNodes.TryGetValue(model, out Node modelNode)) return;

            if (properties.Length <= 0)
            {
                Remove(model);
                return;
            }

            using InternalTempCollection<HashSet<Node>, Node> nodes = new(null);
            CollectAllNode(Root, NodeType.Render, nodes);
            foreach (string propertyName in properties)
            {
                if (string.IsNullOrEmpty(propertyName)) continue;
                if (modelNode.next.Remove(propertyName, out Node propNode))
                    RecycleImmediately(propNode);
            }

            CheckNotReachableNode(Root, nodes);
            foreach (Node node in nodes) RecycleImmediately(node);
            ClearRecycledNodeReference(Root);
        }

        /// <summary>
        /// 注销对指定事件监听的渲染函数
        /// </summary>
        /// <param name="eventKey"></param>
        public void Remove(in EventKey eventKey)
        {
            if (!EventOrModelNodes.TryGetValue(eventKey, out Node eventNode)) return;
            using InternalTempCollection<HashSet<Node>, Node> nodes = new(null);
            CollectAllNode(Root, NodeType.Render, nodes);
            Recycle(eventNode, false);
            CheckNotReachableNode(Root, nodes);
            foreach (Node node in nodes) RecycleImmediately(node);
            ClearRecycledNodeReference(Root);
        }

        /// <summary>
        /// 注销所有事件监听的渲染函数
        /// </summary>
        public void RemoveAllEvent()
        {
            using InternalTempCollection<HashSet<Node>, Node> nodes = new(null);
            CollectAllNode(Root, NodeType.Event, nodes);
            foreach (Node node in nodes) Remove(node.EventKey);
        }

        public IEnumerable<BaseModel> GetModels()
        {
            if (Root == null) yield break;
            foreach (Node node in EventOrModelNodes.Values)
                if (node.type == NodeType.Model && node.Model != null)
                    yield return node.Model;
        }

        public IEnumerable<EventKey> GetEventKeys()
        {
            if (Root == null) yield break;
            foreach (Node node in EventOrModelNodes.Values)
                if (node.type == NodeType.Event && node.EventKey.Type != EventKeyType.NotEventKey)
                    yield return node.EventKey;
        }

        /// <summary>
        /// 只能被RenderContext在RemoveGraph中调用
        /// </summary>
        internal void Dispose()
        {
            if (Root == null) return;
            Recycle(Root, true);
            Root = null;
            Enable = false;
            InternalObjectPool<RenderGraph>.Shared.Return(this);
        }

        internal static RenderGraph NewGraph()
        {
            RenderGraph graph = InternalObjectPool<RenderGraph>.Shared.Rent();
            graph.Root = NewNode(NodeType.Graph, null);
            graph.Enable = true;
            return graph;
        }

        internal static Node NewNode(NodeType type, object data)
        {
            Node node = InternalObjectPool<Node>.Shared.Rent();
            node.type = type;
            node.data = data;
            node.next.Clear();
            return node;
        }

        public sealed class Node
        {
            /// <summary>
            /// key=下一个节点的data
            /// </summary>
            public readonly Dictionary<object, Node> next = new(4);

            public object data;
            public NodeType type;

            public Action RenderFunc => Recycled ? null : data as Action;

            public BaseModel Model => Recycled ? null : data as BaseModel;

            public string PropertyName => Recycled ? string.Empty : data as string;

            public EventKey EventKey => !Recycled && data is EventKey eventKey ? eventKey : new EventKey();

            /// <summary>
            /// 当前节点是否以及被回收了（即type为NodeType.None），如果为true，则说明当前节点已经被回收了，不应该再被使用了
            /// </summary>
            public bool Recycled => type == NodeType.None;
        }
    }
}