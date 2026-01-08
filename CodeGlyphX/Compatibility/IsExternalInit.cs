#if NET472 || NETFRAMEWORK
namespace System.Runtime.CompilerServices;

// Polyfill for record/init support on .NET Framework targets.
internal static class IsExternalInit { }
#endif
