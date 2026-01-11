#if NET472 || NETFRAMEWORK || NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

// Polyfill for record/init support on .NET Framework targets.
internal static class IsExternalInit { }
#endif
