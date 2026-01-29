using System;
using System.IO;
using System.Runtime.InteropServices;
using Godot;

namespace twodog;

/// <summary>
/// Wraps a native Godot instance for embedding scenarios.
/// Use the <see cref="Create"/> factory method to initialize.
/// </summary>
/// <remarks>
/// Only one Engine instance can exist per process (Godot limitation).
/// Dispose the engine when done to clean up native resources.
/// </remarks>
public sealed partial class Engine : IDisposable
{
    private static readonly object CreateLock = new();
    private static Engine? _current;

    private IntPtr _godotInstancePtr;

    private Engine(IntPtr godotInstancePtr, GodotInstanceHandle instance)
    {
        _godotInstancePtr = godotInstancePtr;
        Instance = instance;
    }

    /// <summary>
    /// Gets the <see cref="GodotInstanceHandle"/> for driving the main loop.
    /// </summary>
    public GodotInstanceHandle Instance { get; }

    /// <summary>
    /// Gets the <see cref="SceneTree"/> for the running Godot instance.
    /// </summary>
    public SceneTree Tree =>
        Godot.Engine.Singleton.GetMainLoop() as SceneTree ??
        throw new InvalidOperationException("SceneTree is not available.");

    // Unix (Linux, macOS) - setenv from libc
    [LibraryImport("libc", EntryPoint = "setenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int UnixSetEnv(string name, string value, int overwrite);

    // Windows - SetEnvironmentVariableW from kernel32
    [LibraryImport("kernel32.dll", EntryPoint = "SetEnvironmentVariableW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WindowsSetEnv(string lpName, string lpValue);

    /// <summary>
    /// Creates and starts a Godot engine instance with the given command-line arguments.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments to pass to Godot (e.g. "myapp", "--path", "./project", "--headless").
    /// </param>
    /// <returns>A running <see cref="Engine"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if an Engine instance already exists, or if Godot fails to initialize.
    /// </exception>
    public static Engine Create(params string[] args)
    {
        lock (CreateLock)
        {
            if (_current != null)
                throw new InvalidOperationException(
                    "A Godot engine instance already exists. Only one instance can exist per process.");

            SetAssemblyDirFromLocation();
            var engine = InitializeGodotCore(args);
            _current = engine;
            return engine;
        }
    }

    /// <summary>
    /// Manually overrides the GODOT_ASSEMBLY_DIR environment variable.
    /// Call this BEFORE <see cref="Create"/> if the GodotSharp assemblies
    /// are in a different location than the twodog assembly.
    /// </summary>
    public static void SetAssemblyDir(string assemblyDir)
    {
        if (string.IsNullOrEmpty(assemblyDir))
            throw new ArgumentException("Assembly directory cannot be null or empty", nameof(assemblyDir));

        SetNativeEnvironmentVariable("GODOT_ASSEMBLY_DIR", Path.GetFullPath(assemblyDir));
    }

    public void Dispose()
    {
        lock (CreateLock)
        {
            if (_godotInstancePtr == IntPtr.Zero)
                return;

            Instance.Dispose();
            LibGodot.libgodot_destroy_godot_instance(_godotInstancePtr);
            _godotInstancePtr = IntPtr.Zero;
            _current = null;
        }
    }

    private static void SetAssemblyDirFromLocation()
    {
        // Only set if not already set (allow explicit override via SetAssemblyDir)
        if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GODOT_ASSEMBLY_DIR")))
            return;

        var assemblyLocation = typeof(Engine).Assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
            throw new InvalidOperationException("Cannot determine twodog assembly location");

        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrEmpty(assemblyDir))
            throw new InvalidOperationException("Cannot determine twodog assembly directory");

        SetNativeEnvironmentVariable("GODOT_ASSEMBLY_DIR", Path.GetFullPath(assemblyDir));
    }

    private static void SetNativeEnvironmentVariable(string name, string value)
    {
        System.Environment.SetEnvironmentVariable(name, value);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            WindowsSetEnv(name, value);
        else
            UnixSetEnv(name, value, 1);
    }

    private static unsafe Engine InitializeGodotCore(string[] args)
    {
        var instancePtr = LibGodot.libgodot_create_godot_instance(
            args.Length,
            args,
            &LibGodot.InitCallback
        );

        if (instancePtr == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create Godot instance");

        if (!LibGodot.CallGodotInstanceStart(instancePtr))
        {
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            throw new InvalidOperationException("Failed to start Godot instance");
        }

        var instance = LibGodot.GetGodotInstanceFromPtr(instancePtr);
        return new Engine(instancePtr, instance);
    }
}
