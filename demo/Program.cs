using System.Reflection;
using Godot;
using twodog;

// Resolve the project path relative to the assembly location
var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
var projectPath = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "game"));

// Initialize Godot with the project
TwoDogInitializer.Initialize(projectPath);

GD.Print("Hello from GodotSharp.");
GD.Print("Scene Root: ", TwoDogInitializer.Tree.CurrentScene.Name);

Console.WriteLine("Godot is running, close window or press 'Q' to quit.");

// Main game loop - runs until window closes or 'Q' is pressed
var godot = TwoDogInitializer.Instance!;
while (!godot.Iteration())
{
    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
        break;
}

Console.WriteLine("Godot is shutting down. Thank you for using 2dog. 🦴");
