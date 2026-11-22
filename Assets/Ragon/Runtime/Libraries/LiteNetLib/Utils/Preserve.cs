// FILE:Runtime/Libraries/LiteNetLib/Utils/Preserve.cs ROLE:vendored LiteNetLib 2.1.5 (MIT) - re-vendor instead of editing DEPS:none
// API:upstream public API

using System;

namespace LiteNetLib.Utils
{
    /// <summary>
    /// PreserveAttribute prevents byte code stripping from removing a class, method, field, or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    public class PreserveAttribute : Attribute
    {
    }
}
