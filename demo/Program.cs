using System.Reflection;
using Godot;
using Engine = twodog.Engine;

// Resolve the project path relative to the assembly location
var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
var projectPath = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "game"));

// Create and start the Godot engine
using var engine = Engine.Create("demo", "--path", projectPath);

GD.Print("Hello from GodotSharp.");
GD.Print("Scene Root: ", engine.Tree.CurrentScene.Name);

Console.WriteLine("Godot is running, close window or press 'Q' to quit.");

while (!engine.Instance.Iteration())
{
    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
        break;
}

Console.WriteLine("Godot is shutting down. Thank you for using 2dog. 🦴");
