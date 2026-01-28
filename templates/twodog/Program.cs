using Godot;
using twodog;

// Initialize Godot with your project
TwoDogInitializer.Initialize("./project");

// Load your main scene
var scene = GD.Load<PackedScene>("res://main.tscn");
TwoDogInitializer.Tree.Root.AddChild(scene.Instantiate());

GD.Print("2dog is running! Close window or press 'Q' to quit.");
Console.WriteLine("Press 'Q' to quit.");

// Main game loop - runs until window closes or 'Q' is pressed
var godot = TwoDogInitializer.Instance!;
while (!godot.Iteration())
{
    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
        break;
    
    // Your per-frame logic here
}

Console.WriteLine("Shutting down...");
