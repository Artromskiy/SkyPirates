using System;

namespace DVG.Core.Ids.Attributes
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class StringIdDrawerAttribute : Attribute
    {
        public readonly DrawerType drawerType;
        public StringIdDrawerAttribute(DrawerType drawerType)
        {
            this.drawerType = drawerType;
        }
    }

    public enum DrawerType
    {
        Simple,
        Editor,
    }
}
