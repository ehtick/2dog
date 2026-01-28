using System;
using System.IO;
using System.Runtime.InteropServices;

namespace twodog;

/// <summary>
/// Static initializer for the Godot runtime in embedding scenarios.
/// Provides a simple API to initialize, run, and shut down Godot from a .NET application.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TwoDogInitializer"/> is the main entry point for using twodog. It handles:
/// </para>
/// <list type="bullet">
///   <item><description>Setting up the <c>GODOT_ASSEMBLY_DIR</c> environment variable for GodotSharp</description></item>
///   <item><description>Creating and starting a Godot instance with your project</description></item>
///   <item><description>Providing access to the <see cref="Godot.SceneTree"/> for scene management</description></item>
///   <item><description>Managing the Godot instance lifecycle with proper cleanup</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong> All public methods are thread-safe. The initializer uses locking
/// to ensure only one Godot instance is created, even if <see cref="Initialize"/> is called from
/// multiple threads simultaneously.
/// </para>
/// <para>
/// <strong>Single Instance:</strong> Only one Godot instance can exist per process. Calling
/// <see cref="Initialize"/> multiple times is safe and will simply return if already initialized.
/// To reinitialize with different settings, call <see cref="Shutdown"/> first.
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Basic Usage:</strong></para>
/// <code>
/// using Godot;
/// using twodog;
/// 
/// // Initialize Godot with your project
/// TwoDogInitializer.Initialize("./project", headless: false);
/// 
/// // Access the scene tree
/// var tree = TwoDogInitializer.Tree;
/// GD.Print("Root node: ", tree.Root.Name);
/// 
/// // Load and instantiate a scene
/// var scene = GD.Load&lt;PackedScene&gt;("res://main.tscn");
/// tree.Root.AddChild(scene.Instantiate());
/// 
/// // Run the main loop
/// var godot = TwoDogInitializer.Instance!;
/// while (!godot.Iteration())
/// {
///     // Your per-frame logic here
/// }
/// 
/// // Clean up when done
/// TwoDogInitializer.Shutdown();
/// </code>
/// </example>
/// <example>
/// <para><strong>Headless Testing:</strong></para>
/// <code>
/// // For unit tests, use headless mode (no window)
/// TwoDogInitializer.Initialize("./project", headless: true);
/// 
/// // Run tests against Godot APIs
/// var node = new Node();
/// node.Name = "TestNode";
/// TwoDogInitializer.Tree.Root.AddChild(node);
/// 
/// // Shutdown after tests
/// TwoDogInitializer.Shutdown();
/// </code>
/// </example>
/// <seealso cref="GodotInstanceHandle"/>
public static partial class TwoDogInitializer
{
    /// <summary>
    /// The shared Godot instance pointer created during initialization.
    /// </summary>
    /// <remarks>
    /// This is used internally to manage the native Godot instance.
    /// External code should use <see cref="Instance"/> instead.
    /// </remarks>
    internal static IntPtr SharedGodotInstancePtr { get; private set; } = IntPtr.Zero;

    /// <summary>
    /// Gets a value indicating whether Godot has been initialized.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Initialize"/> has been called successfully
    /// and <see cref="Shutdown"/> has not been called; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Check this property before accessing <see cref="Tree"/> or <see cref="Instance"/>
    /// to avoid exceptions. This property is thread-safe.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!TwoDogInitializer.IsInitialized)
    /// {
    ///     TwoDogInitializer.Initialize("./project");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Initialize"/>
    /// <seealso cref="Shutdown"/>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the <see cref="GodotInstanceHandle"/> for the initialized Godot instance.
    /// </summary>
    /// <value>
    /// A <see cref="GodotInstanceHandle"/> that can be used to iterate the Godot main loop,
    /// or <see langword="null"/> if Godot has not been initialized.
    /// </value>
    /// <remarks>
    /// <para>
    /// Use this handle to drive Godot's main loop by calling <see cref="GodotInstanceHandle.Iteration"/>.
    /// Each call to <c>Iteration()</c> processes one frame of the Godot engine.
    /// </para>
    /// <para>
    /// The returned handle should not be disposed directly; use <see cref="Shutdown"/> instead
    /// to properly clean up all Godot resources.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// TwoDogInitializer.Initialize("./project");
    /// 
    /// // Get the instance handle
    /// var godot = TwoDogInitializer.Instance;
    /// if (godot != null)
    /// {
    ///     // Run for 100 frames
    ///     for (int i = 0; i &lt; 100; i++)
    ///     {
    ///         if (godot.Iteration())
    ///             break; // Godot requested exit
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Initialize"/>
    /// <seealso cref="GodotInstanceHandle"/>
    public static GodotInstanceHandle? Instance { get; private set; }

    /// <summary>
    /// Gets the <see cref="Godot.SceneTree"/> for the initialized Godot instance.
    /// </summary>
    /// <value>
    /// The active <see cref="Godot.SceneTree"/> which provides access to the scene hierarchy,
    /// signals, groups, and other scene management functionality.
    /// </value>
    /// <remarks>
    /// <para>
    /// The <see cref="Godot.SceneTree"/> is the heart of Godot's scene system. Use it to:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Access the root node via <c>Tree.Root</c></description></item>
    ///   <item><description>Change scenes with <c>Tree.ChangeSceneToFile()</c></description></item>
    ///   <item><description>Manage node groups</description></item>
    ///   <item><description>Control the game loop (pause, quit)</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if Godot has not been initialized or if the SceneTree is not available.
    /// Call <see cref="Initialize"/> before accessing this property.
    /// </exception>
    /// <example>
    /// <code>
    /// TwoDogInitializer.Initialize("./project");
    /// 
    /// // Access the scene tree
    /// var tree = TwoDogInitializer.Tree;
    /// 
    /// // Add a node to the root
    /// var node = new Node2D();
    /// node.Name = "Player";
    /// tree.Root.AddChild(node);
    /// 
    /// // Load and change to a new scene
    /// tree.ChangeSceneToFile("res://levels/level1.tscn");
    /// </code>
    /// </example>
    /// <seealso cref="Initialize"/>
    /// <seealso cref="IsInitialized"/>
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
    /// </summary>
    /// <param name="projectPath">
    /// Path to the Godot project directory containing <c>project.godot</c>.
    /// Can be relative or absolute; relative paths are resolved from the current directory.
    /// </param>
    /// <param name="headless">
    /// If <see langword="true"/>, runs Godot in headless mode without creating a window.
    /// Use this for unit tests, CI/CD pipelines, or server applications.
    /// Default is <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// This is the main entry point for using twodog. Call this method before using any
    /// Godot functionality. The method is idempotent - calling it multiple times is safe
    /// and subsequent calls will return immediately if already initialized.
    /// </para>
    /// <para>
    /// The method automatically:
    /// </para>
    /// <list type="number">
    ///   <item><description>Creates a Godot instance with the specified project</description></item>
    ///   <item><description>Starts the Godot engine and initializes the scene tree</description></item>
    /// </list>
    /// <para>
    /// <strong>Note:</strong> The <c>GODOT_ASSEMBLY_DIR</c> environment variable is automatically
    /// set by twodog's module initializer when the assembly loads, before any user code runs.
    /// This ensures libgodot can find the GodotSharp assemblies.
    /// </para>
    /// <para>
    /// <strong>Headless Mode:</strong> When <paramref name="headless"/> is <see langword="true"/>,
    /// Godot runs without a display server. This is useful for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Running unit tests in CI/CD environments</description></item>
    ///   <item><description>Server-side game logic processing</description></item>
    ///   <item><description>Batch processing or tooling</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if Godot fails to initialize. This can happen if:
    /// <list type="bullet">
    ///   <item><description>The project path is invalid or missing <c>project.godot</c></description></item>
    ///   <item><description>The Godot native library cannot be loaded</description></item>
    ///   <item><description>GodotSharp assemblies cannot be found</description></item>
    /// </list>
    /// </exception>
    /// <example>
    /// <code>
    /// // Initialize with a window
    /// TwoDogInitializer.Initialize("./my-godot-project");
    /// 
    /// // Use Godot APIs
    /// GD.Print("Hello from Godot!");
    /// var tree = TwoDogInitializer.Tree;
    /// </code>
    /// </example>
    public static void Initialize(string projectPath, bool headless = false)
    {
        lock (InitLock)
        {
            if (IsInitialized)
                return;

            // Note: GODOT_ASSEMBLY_DIR is already set by the module initializer (TwoDogModuleInitializer)
            // which runs when the twodog assembly is loaded, before any user code.

            // Build args
            var args = headless
                ? new[] { "twodog", "--path", Path.GetFullPath(projectPath), "--headless" }
                : new[] { "twodog", "--path", Path.GetFullPath(projectPath) };

            InitializeGodotCore(args);
        }
    }

    /// <summary>
    /// Initializes Godot with custom command-line arguments.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments to pass to Godot. These are the same arguments you would
    /// pass to the Godot executable from the command line.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method is for advanced use cases where you need fine-grained control over
    /// Godot's startup. Most users should use <see cref="Initialize"/> instead.
    /// </para>
    /// <para>
    /// Common arguments include:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>--path &lt;dir&gt;</c> - Path to the project directory</description></item>
    ///   <item><description><c>--headless</c> - Run without a display</description></item>
    ///   <item><description><c>--verbose</c> - Enable verbose output</description></item>
    ///   <item><description><c>--debug</c> - Enable debug mode</description></item>
    ///   <item><description><c>--rendering-driver &lt;driver&gt;</c> - Specify rendering driver</description></item>
    /// </list>
    /// <para>
    /// The first argument is typically the application name (e.g., "twodog").
    /// </para>
    /// <para>
    /// <strong>Note:</strong> The <c>GODOT_ASSEMBLY_DIR</c> environment variable is automatically
    /// set by twodog's module initializer when the assembly loads.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if Godot fails to initialize.
    /// </exception>
    /// <example>
    /// <code>
    /// // Initialize with custom arguments
    /// TwoDogInitializer.InitializeWithArgs(
    ///     "myapp",
    ///     "--path", "./project",
    ///     "--headless",
    ///     "--verbose"
    /// );
    /// </code>
    /// </example>
    public static void InitializeWithArgs(params string[] args)
    {
        lock (InitLock)
        {
            if (IsInitialized)
                return;

            // Note: GODOT_ASSEMBLY_DIR is already set by the module initializer
            InitializeGodotCore(args);
        }
    }

    /// <summary>
    /// Shuts down Godot and releases all resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method when you're done using Godot to ensure proper cleanup of native
    /// resources. This prevents memory leaks and "unreferenced static string" errors
    /// that occur when Godot is not properly shut down.
    /// </para>
    /// <para>
    /// After calling <see cref="Shutdown"/>:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="IsInitialized"/> returns <see langword="false"/></description></item>
    ///   <item><description><see cref="Instance"/> returns <see langword="null"/></description></item>
    ///   <item><description><see cref="Tree"/> will throw <see cref="InvalidOperationException"/></description></item>
    ///   <item><description>You can call <see cref="Initialize"/> again to restart Godot</description></item>
    /// </list>
    /// <para>
    /// This method is safe to call multiple times or when Godot is not initialized.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// TwoDogInitializer.Initialize("./project");
    /// 
    /// try
    /// {
    ///     // Use Godot APIs
    ///     var godot = TwoDogInitializer.Instance!;
    ///     while (!godot.Iteration()) { }
    /// }
    /// finally
    /// {
    ///     // Always clean up
    ///     TwoDogInitializer.Shutdown();
    /// }
    /// </code>
    /// </example>
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
    /// Manually overrides the <c>GODOT_ASSEMBLY_DIR</c> environment variable.
    /// </summary>
    /// <param name="assemblyDir">
    /// The directory containing the GodotSharp assemblies (<c>GodotSharp.dll</c>,
    /// <c>GodotPlugins.dll</c>, etc.). Can be relative or absolute.
    /// </param>
    /// <remarks>
    /// <para>
    /// By default, the module initializer automatically sets <c>GODOT_ASSEMBLY_DIR</c>
    /// based on the location of the twodog assembly when it loads. Most users don't
    /// need to call this method.
    /// </para>
    /// <para>
    /// <strong>When to use:</strong> Call this method BEFORE <see cref="Initialize"/>
    /// if the GodotSharp assemblies are in a different location than the twodog assembly.
    /// This will override the value set by the module initializer.
    /// </para>
    /// <para>
    /// The environment variable is set at both the .NET runtime level and the native
    /// OS level to ensure both managed and native code can read it.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="assemblyDir"/> is <see langword="null"/> or empty.
    /// </exception>
    /// <example>
    /// <code>
    /// // Override assembly directory before initialization
    /// TwoDogInitializer.SetAssemblyDir("/custom/path/to/assemblies");
    /// TwoDogInitializer.Initialize("./project");
    /// </code>
    /// </example>
    public static void SetAssemblyDir(string assemblyDir)
    {
        if (string.IsNullOrEmpty(assemblyDir))
            throw new ArgumentException("Assembly directory cannot be null or empty", nameof(assemblyDir));

        var absolutePath = Path.GetFullPath(assemblyDir);
        SetNativeEnvironmentVariable("GODOT_ASSEMBLY_DIR", absolutePath);
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

