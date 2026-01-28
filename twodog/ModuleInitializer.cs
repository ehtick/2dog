using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace twodog;

/// <summary>
/// Module initializer that sets GODOT_ASSEMBLY_DIR environment variable
/// before any other code in the twodog assembly runs.
/// 
/// This is critical because libgodot reads this environment variable when it
/// initializes its .NET runtime. If we don't set it before libgodot loads,
/// Godot will look for assemblies in the wrong location.
/// </summary>
internal static partial class TwoDogModuleInitializer
{
    // Unix (Linux, macOS) - setenv from libc
    [LibraryImport("libc", EntryPoint = "setenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int UnixSetEnv(string name, string value, int overwrite);

    // Windows - SetEnvironmentVariableW from kernel32
    [LibraryImport("kernel32.dll", EntryPoint = "SetEnvironmentVariableW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WindowsSetEnv(string lpName, string lpValue);

    [ModuleInitializer]
    internal static void Initialize()
    {
        // Get the directory where twodog.dll is located
        var assemblyLocation = typeof(TwoDogModuleInitializer).Assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
            return; // Can't determine location, hope for the best

        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrEmpty(assemblyDir))
            return;

        var absolutePath = Path.GetFullPath(assemblyDir);

        // Set at .NET runtime level
        Environment.SetEnvironmentVariable("GODOT_ASSEMBLY_DIR", absolutePath);

        // Set at native OS level so libgodot can see it
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            WindowsSetEnv("GODOT_ASSEMBLY_DIR", absolutePath);
        else
            UnixSetEnv("GODOT_ASSEMBLY_DIR", absolutePath, 1);
    }
}
