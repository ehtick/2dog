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

        Engine = Engine.Create("twodog.tests", "--path", projectPath, "--headless");

        Console.WriteLine("Godot fixture initialized successfully.");
    }

    public Engine Engine { get; }

    public SceneTree Tree => Engine.Tree;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Console.WriteLine("Shutting down Godot...");
        Engine.Dispose();
        Console.WriteLine("Godot fixture disposed.");
    }
}
