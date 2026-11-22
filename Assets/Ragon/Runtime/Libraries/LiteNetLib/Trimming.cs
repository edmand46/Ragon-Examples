// FILE:Runtime/Libraries/LiteNetLib/Trimming.cs ROLE:vendored LiteNetLib 2.1.5 (MIT) - re-vendor instead of editing DEPS:none
// API:upstream public API

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace LiteNetLib
{
    internal static class Trimming
    {
        internal const DynamicallyAccessedMemberTypes SerializerMemberTypes = PublicProperties | NonPublicProperties;
    }
}
#endif
