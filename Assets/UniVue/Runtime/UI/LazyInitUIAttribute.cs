using System;

namespace UniVue.UI
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class LazyInitAttribute : Attribute
    {
        public string Path { get; }
        
        public LazyInitAttribute(string path)
        {
            Path = path;
        }
    }
}