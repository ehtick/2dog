---
layout: home
title: 2dog
titleTemplate: :title – Godot in .NET

head:
  - - meta
    - name: title
      content: 2dog – Godot in .NET
  - - meta
    - name: description
      content: Embed Godot Engine in your .NET applications. Full engine control, xUnit testing, and CI/CD support.
  - - meta
    - property: og:type
      content: website
  - - meta
    - property: og:url
      content: https://2dog.dev
  - - meta
    - property: og:title
      content: 2dog – Godot in .NET

hero:
  image:
    src: /logo-animated.svg
    alt: a happy white dog smiling over the soft logotype text '2dog'
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/outfox/2dog

features:
  - title: 🎮 Full Godot Power
    details: Access the complete GodotSharp API  –  scenes, physics, rendering, audio, input  –  everything Godot can do.
  - title: 🔄 Inverted Control
    details: Your .NET process controls Godot, not the other way around. Start, iterate, and stop the engine when you decide.
  - title: 🧪 First-Class Testing
    details: Built-in xUnit fixtures for testing Godot code. Run headless in CI/CD pipelines.

---


## All right, let's cook! 

::: code-group

```csharp [🎮 Basic Example]
using Godot;
using Engine = twodog.Engine;

// Create and start the Godot engine with your project
using var engine = Engine.Create("myapp", "--path", "./project");

// Load a scene
var scene = GD.Load<PackedScene>("res://game.tscn");
engine.Tree.Root.AddChild(scene.Instantiate());

// Run the main loop
while (!engine.Instance.Iteration())
{
    // Your code here  –  every frame
}
```

```csharp [🧪 Unit Test Example]
using Godot;
using twodog.tests;

[Collection("GodotHeadless")]
public class GodotSceneTests(GodotHeadlessFixture fixture)
{
    [Fact]
    public void LoadScene_ValidPath_Succeeds()
    {
        var scene = GD.Load<PackedScene>("res://game.tscn");
        var instance = scene.Instantiate();
        
        fixture.Tree.Root.AddChild(instance);
        
        Assert.NotNull(instance.Parent);
    }
}
```

```csharp [🔨 Tool Example]
// Build with: dotnet build -c Editor
// Enables TOOLS_ENABLED for import and editor features

using Godot;
using Engine = twodog.Engine;

// Create and start the Godot engine with your project
using var engine = Engine.Create("mytool", "--path", "./project");

// Import a texture with custom settings
var importer = ResourceImporterTexture.Singleton;
// Use Godot's full import pipeline
// Access editor-only APIs like EditorImportPlugin, EditorInterface, etc.

// Engine is automatically cleaned up via Dispose
```

:::

## Installation

::: code-group

```bash [🤖 Existing Project]
# Package pending NuGet release
# You can build from source for now: git clone
dotnet add package 2dog
```

```bash [🌱 Fresh Project]
# Template pending NuGet release
# For now: dotnet new install ./templates/twodog
dotnet new 2dog -n LetsCook
```

:::
