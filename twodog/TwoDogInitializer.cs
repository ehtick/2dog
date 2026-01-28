using System;
using System.IO;
using System.Runtime.InteropServices;

namespace twodog;

/// <summary>
/// Static initializer for the Godot runtime in embedding scenarios.
/// 
/// Call Initialize() before using any Godot functionality:
/// <code>
/// TwoDogInitializer.Initialize("/path/to/godot/project", headless: true);
/// </code>
/// </summary>
public static partial class TwoDogInitializer
{
    /// <summary>
    /// The shared Godot instance pointer created during initialization.
    /// </summary>
    internal static IntPtr SharedGodotInstancePtr { get; private set; } = IntPtr.Zero;

    /// <summary>
    /// Whether Godot has been initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the GodotInstanceHandle for the initialized Godot instance.
    /// Returns null if Godot has not been initialized.
    /// </summary>
    public static GodotInstanceHandle? Instance { get; private set; }

    /// <summary>
    /// Gets the SceneTree for the initialized Godot instance.
    /// Throws if Godot has not been initialized.
    /// </summary>
    public static Godot.SceneTree Tree => 
        Godot.Engine.Singleton.GetMainLoop() as Godot.SceneTree ??
        throw new InvalidOperationException("Godot is not initialized or SceneTree is not available. Call Initialize() first.");

    private static readonly object InitLock = new();

    // Unix (Linux, macOS) - setenv from libc
    [LibraryImport("libc", EntryPoint = "setenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int UnixSetEnv(string name, string value, int overwrite);

    // Windows - SetEnvironmentVariableW from kernel32
    [LibraryImport("kernel32.dll", EntryPoint = "SetEnvironmentVariableW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WindowsSetEnv(string lpName, string lpValue);

    /// <summary>
    /// Initializes Godot with a project.
    /// 
    /// This is the main entry point for using twodog. Call this before any Godot functionality.
    /// </summary>
    /// <param name="projectPath">Path to the Godot project directory (containing project.godot)</param>
    /// <param name="headless">If true, runs in headless mode (no window)</param>
    /// <exception cref="InvalidOperationException">Thrown if Godot fails to initialize</exception>
    public static void Initialize(string projectPath, bool headless = false)
    {
        lock (InitLock)
        {
            if (IsInitialized)
                return;

            // Automatically set GODOT_ASSEMBLY_DIR based on twodog's location
            SetAssemblyDirFromTwoDogLocation();

            // Build args
            var args = headless
                ? new[] { "twodog", "--path", Path.GetFullPath(projectPath), "--headless" }
                : new[] { "twodog", "--path", Path.GetFullPath(projectPath) };

            InitializeGodotCore(args);
        }
    }

    /// <summary>
    /// Initializes Godot with custom command-line arguments.
    /// 
    /// For advanced use cases. Most users should use Initialize(projectPath, headless) instead.
    /// </summary>
    /// <param name="args">Command-line arguments to pass to Godot</param>
    /// <exception cref="InvalidOperationException">Thrown if Godot fails to initialize</exception>
    public static void InitializeWithArgs(params string[] args)
    {
        lock (InitLock)
        {
            if (IsInitialized)
                return;

            SetAssemblyDirFromTwoDogLocation();
            InitializeGodotCore(args);
        }
    }

    /// <summary>
    /// Shuts down Godot and releases all resources.
    /// 
    /// Call this when you're done using Godot to ensure proper cleanup.
    /// After calling Shutdown(), you can call Initialize() again if needed.
    /// </summary>
    public static void Shutdown()
    {
        lock (InitLock)
        {
            if (!IsInitialized || SharedGodotInstancePtr == IntPtr.Zero)
                return;

            LibGodot.libgodot_destroy_godot_instance(SharedGodotInstancePtr);
            SharedGodotInstancePtr = IntPtr.Zero;
            Instance = null;
            IsInitialized = false;
        }
    }

    /// <summary>
    /// Manually set the GODOT_ASSEMBLY_DIR environment variable.
    /// 
    /// Call this BEFORE Initialize() if you need to override the default assembly location.
    /// Most users don't need this - Initialize() sets it automatically.
    /// </summary>
    public static void SetAssemblyDir(string assemblyDir)
    {
        if (string.IsNullOrEmpty(assemblyDir))
            throw new ArgumentException("Assembly directory cannot be null or empty", nameof(assemblyDir));

        var absolutePath = Path.GetFullPath(assemblyDir);
        SetNativeEnvironmentVariable("GODOT_ASSEMBLY_DIR", absolutePath);
    }

    private static void SetAssemblyDirFromTwoDogLocation()
    {
        var assemblyLocation = typeof(TwoDogInitializer).Assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
            throw new InvalidOperationException("Cannot determine twodog assembly location");

        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrEmpty(assemblyDir))
            throw new InvalidOperationException("Cannot determine twodog assembly directory");

        SetAssemblyDir(assemblyDir);
    }

    private static void SetNativeEnvironmentVariable(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            WindowsSetEnv(name, value);
        else
            UnixSetEnv(name, value, 1);
    }

    private static unsafe void InitializeGodotCore(string[] args)
    {
        if (SharedGodotInstancePtr != IntPtr.Zero)
            return;

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

        SharedGodotInstancePtr = instancePtr;
        Instance = LibGodot.GetGodotInstanceFromPtr(instancePtr);
        IsInitialized = true;
    }
}
