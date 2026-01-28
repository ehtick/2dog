using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace twodog.tests;

/// <summary>
/// Module initializer that sets GODOT_ASSEMBLY_DIR environment variable
/// BEFORE any twodog code is loaded.
/// 
/// This is critical because libgodot reads the environment variable when it's
/// first loaded by the .NET runtime. The twodog.xunit assembly is loaded before
/// twodog.dll (since it references it), so this module initializer runs first
/// and can set the environment variable before libgodot.so is loaded.
/// </summary>
internal static partial class TwoDogXunitModuleInitializer
{
    // Unix (Linux, macOS) - setenv from libc
    [LibraryImport("libc", EntryPoint = "setenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int UnixSetEnv(string name, string value, int overwrite);

    // Windows - SetEnvironmentVariableW from kernel32
    [LibraryImport("kernel32.dll", EntryPoint = "SetEnvironmentVariableW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WindowsSetEnv(string lpName, string lpValue);

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
    internal static void Initialize()
#pragma warning restore CA2255
    {
        // Get the directory where twodog.xunit.dll is located
        // This will be the same directory as GodotPlugins.dll and other assemblies
        var assemblyLocation = typeof(TwoDogXunitModuleInitializer).Assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
            return; // Can't determine location, hope for the best

        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrEmpty(assemblyDir))
            return;

        var absolutePath = Path.GetFullPath(assemblyDir);

        // Set at .NET runtime level
        Environment.SetEnvironmentVariable("GODOT_ASSEMBLY_DIR", absolutePath);

        // Set at native OS level so libgodot can see it when it's loaded later
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            WindowsSetEnv("GODOT_ASSEMBLY_DIR", absolutePath);
        else
            UnixSetEnv("GODOT_ASSEMBLY_DIR", absolutePath, 1);
    }
}
