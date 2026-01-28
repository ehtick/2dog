using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace twodog;

/// <summary>
/// Module initializer that runs when the twodog assembly is first loaded,
/// before any types are accessed. This ensures the GODOT_ASSEMBLY_DIR
/// environment variable is set before P/Invoke loads libgodot.
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

    /// <summary>
    /// Sets an environment variable at the native OS level so that both .NET code
    /// and native code (like libgodot's C++) can read it.
    /// </summary>
    private static void SetNativeEnvironmentVariable(string name, string value)
    {
        // Set in .NET runtime first
        Environment.SetEnvironmentVariable(name, value);

        // Then set at native OS level so libgodot's C++ code can see it
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsSetEnv(name, value);
        }
        else
        {
            // Linux, macOS, and other Unix-like systems
            UnixSetEnv(name, value, 1); // 1 = overwrite if exists
        }
    }

    /// <summary>
    /// Sets the GODOT_ASSEMBLY_DIR environment variable to the directory containing
    /// the twodog assembly. This is necessary because libgodot looks for GodotPlugins.dll
    /// and GodotSharp.dll in various locations, and when running via 'dotnet test' or as an
    /// embedded library, the executable directory points to dotnet's location rather than
    /// the application output directory where the assemblies are located.
    /// 
    /// Note: You may still see "ERROR: .NET: Assemblies not found" during Godot initialization.
    /// This is expected - Godot checks multiple paths before finding the assemblies via
    /// GODOT_ASSEMBLY_DIR. The error is logged but does not prevent successful initialization.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Only set if not already set (allow explicit override)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GODOT_ASSEMBLY_DIR")))
        {
            var assemblyLocation = typeof(TwoDogModuleInitializer).Assembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    SetNativeEnvironmentVariable("GODOT_ASSEMBLY_DIR", assemblyDir);
                }
            }
        }
    }
}
