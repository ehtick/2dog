using System.Reflection;
using Godot;
using JetBrains.Annotations;

namespace twodog.tests;

/// <summary>
/// xUnit fixture for headless Godot testing.
/// 
/// Automatically initializes Godot in headless mode with the game project.
/// </summary>
[UsedImplicitly]
public class GodotHeadlessFixture : IDisposable
{
    public GodotHeadlessFixture()
    {
        Console.WriteLine("Initializing Godot fixture...");

        // Resolve the project path relative to the assembly location
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var projectPath = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "game"));

        // Initialize Godot with the project in headless mode
        TwoDogInitializer.Initialize(projectPath, headless: true);
        
        Console.WriteLine("Godot fixture initialized successfully.");
    }

    public SceneTree Tree => Godot.Engine.Singleton.GetMainLoop() as SceneTree ??
                             throw new NullReferenceException("Failed to get SceneTree.");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Console.WriteLine("Shutting down Godot...");
        TwoDogInitializer.Shutdown();
        Console.WriteLine("Godot fixture disposed.");
    }
}
