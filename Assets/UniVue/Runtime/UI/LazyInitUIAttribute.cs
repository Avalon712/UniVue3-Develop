using System;

namespace UniVue.UI
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class LazyInitUIAttribute : Attribute
    {
        public string Path { get; }

        public LazyInitUIAttribute(string path) => Path = path;
    }
}
