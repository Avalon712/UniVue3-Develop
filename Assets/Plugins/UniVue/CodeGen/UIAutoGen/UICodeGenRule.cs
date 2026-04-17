using System;
using System.Collections.Generic;
using UnityEngine;
using UniVue.UI;

namespace UniVue.Editor
{
    public abstract class UICodeGenRule : IComparable<UICodeGenRule>
    {
        /// <summary>
        /// 值越小越先被调用
        /// </summary>
        public abstract int Order { get; }

        /// <summary>
        /// 内置规则的排序，保证内置规则可以不被外部覆盖
        /// </summary>
        internal virtual int InternalRuleOrder { get; set; }

        /// <summary>
        /// 能否生成代码
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="baseUI">预制体身上挂载的UI组件</param>
        /// <returns>true-能够自动生成UI代码， false-不能（只要被过滤就不会再被生成）</returns>
        protected abstract bool Filter(GameObject prefab, BaseUI baseUI);

        /// <summary>
        /// 尝试生成属性
        /// </summary>
        /// <param name="clazz">生成UI代码的类</param>
        /// <param name="go">预制体</param>
        /// <param name="properties">生成的属性</param>
        /// <returns>true-生成成功， false-生成失败</returns>
        protected abstract bool TryGenProperties(Type clazz, GameObject go, HashSet<GeneratedProperty> properties);

        internal bool InvokeFilter(GameObject prefab, BaseUI baseUI) => Filter(prefab, baseUI);

        internal bool InvokeTryGenProperties(Type clazz, GameObject go, HashSet<GeneratedProperty> properties)
            => TryGenProperties(clazz, go, properties);

        public int CompareTo(UICodeGenRule other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            long order = Order + InternalRuleOrder;
            return order.CompareTo(other.InternalRuleOrder + other.Order);
        }
    }

    public readonly struct GeneratedProperty : IEquatable<GeneratedProperty>
    {
        public readonly string propertyTypeFullName;
        public readonly string propertyName;
        public readonly string path;

        public GeneratedProperty(string propertyTypeFullName, string propertyName, string path)
        {
            this.propertyTypeFullName = propertyTypeFullName;
            this.propertyName = propertyName;
            this.path = path;
        }

        public bool Equals(GeneratedProperty other) => propertyName == other.propertyName;

        public override bool Equals(object obj) => obj is GeneratedProperty other && Equals(other);

        public override int GetHashCode() => propertyName?.GetHashCode() ?? 0;
    }
}
