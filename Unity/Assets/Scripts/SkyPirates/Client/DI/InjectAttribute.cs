using System;

namespace SkyPirates.Client.DI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class InjectAttribute : Attribute { }
}
