using Godot;

namespace twodog.tests;

/// <summary>
/// Tests that verify GodotSharp objects can be instantiated and used correctly.
/// These tests validate that the libgodot integration is working properly
/// by exercising both value types and reference types from GodotSharp.
/// </summary>
[Collection("Godot")]
public class GodotObjectTests(GodotHeadlessFixture godot)
{
    #region Value Types - Basic Math

    [Fact]
    public void Vector2_Creation_Works()
    {
        var v = new Vector2(3.0f, 4.0f);
        
        Assert.Equal(3.0f, v.X);
        Assert.Equal(4.0f, v.Y);
        Assert.Equal(5.0f, v.Length(), 0.0001f);
    }

    [Fact]
    public void Vector2_Operations_Work()
    {
        var a = new Vector2(1.0f, 2.0f);
        var b = new Vector2(3.0f, 4.0f);
        
        var sum = a + b;
        Assert.Equal(4.0f, sum.X);
        Assert.Equal(6.0f, sum.Y);
        
        var dot = a.Dot(b);
        Assert.Equal(11.0f, dot, 0.0001f);
        
        var normalized = a.Normalized();
        Assert.Equal(1.0f, normalized.Length(), 0.0001f);
    }

    [Fact]
    public void Vector3_Creation_Works()
    {
        var v = new Vector3(1.0f, 2.0f, 3.0f);
        
        Assert.Equal(1.0f, v.X);
        Assert.Equal(2.0f, v.Y);
        Assert.Equal(3.0f, v.Z);
    }

    [Fact]
    public void Vector3_Operations_Work()
    {
        var a = new Vector3(1.0f, 0.0f, 0.0f);
        var b = new Vector3(0.0f, 1.0f, 0.0f);
        
        var cross = a.Cross(b);
        Assert.Equal(0.0f, cross.X, 0.0001f);
        Assert.Equal(0.0f, cross.Y, 0.0001f);
        Assert.Equal(1.0f, cross.Z, 0.0001f);
        
        var dot = a.Dot(b);
        Assert.Equal(0.0f, dot, 0.0001f);
    }

    [Fact]
    public void Vector4_Creation_Works()
    {
        var v = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        
        Assert.Equal(1.0f, v.X);
        Assert.Equal(2.0f, v.Y);
        Assert.Equal(3.0f, v.Z);
        Assert.Equal(4.0f, v.W);
    }

    [Fact]
    public void Quaternion_Creation_Works()
    {
        var q = Quaternion.Identity;
        
        Assert.Equal(0.0f, q.X, 0.0001f);
        Assert.Equal(0.0f, q.Y, 0.0001f);
        Assert.Equal(0.0f, q.Z, 0.0001f);
        Assert.Equal(1.0f, q.W, 0.0001f);
    }

    [Fact]
    public void Quaternion_FromEuler_Works()
    {
        var euler = new Vector3(0.0f, Mathf.Pi / 2, 0.0f); // 90 degrees around Y
        var q = Quaternion.FromEuler(euler);
        
        Assert.True(q.IsNormalized());
    }

    #endregion

    #region Value Types - Geometry

    [Fact]
    public void Color_Creation_Works()
    {
        var c = new Color(1.0f, 0.5f, 0.25f, 1.0f);
        
        Assert.Equal(1.0f, c.R, 0.0001f);
        Assert.Equal(0.5f, c.G, 0.0001f);
        Assert.Equal(0.25f, c.B, 0.0001f);
        Assert.Equal(1.0f, c.A, 0.0001f);
    }

    [Fact]
    public void Color_Predefined_Works()
    {
        var red = Colors.Red;
        var green = Colors.Green;
        var blue = Colors.Blue;
        
        Assert.Equal(1.0f, red.R, 0.0001f);
        Assert.Equal(1.0f, green.G, 0.0001f);
        Assert.Equal(1.0f, blue.B, 0.0001f);
    }

    [Fact]
    public void Rect2_Creation_Works()
    {
        var rect = new Rect2(10.0f, 20.0f, 100.0f, 50.0f);
        
        Assert.Equal(10.0f, rect.Position.X);
        Assert.Equal(20.0f, rect.Position.Y);
        Assert.Equal(100.0f, rect.Size.X);
        Assert.Equal(50.0f, rect.Size.Y);
    }

    [Fact]
    public void Rect2_Contains_Works()
    {
        var rect = new Rect2(0.0f, 0.0f, 100.0f, 100.0f);
        
        Assert.True(rect.HasPoint(new Vector2(50.0f, 50.0f)));
        Assert.False(rect.HasPoint(new Vector2(150.0f, 50.0f)));
    }

    [Fact]
    public void Aabb_Creation_Works()
    {
        var aabb = new Aabb(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        
        Assert.Equal(new Vector3(0, 0, 0), aabb.Position);
        Assert.Equal(new Vector3(10, 10, 10), aabb.Size);
    }

    [Fact]
    public void Plane_Creation_Works()
    {
        var plane = new Plane(Vector3.Up, 5.0f);
        
        Assert.Equal(Vector3.Up, plane.Normal);
        Assert.Equal(5.0f, plane.D, 0.0001f);
    }

    [Fact]
    public void Transform2D_Identity_Works()
    {
        var t = Transform2D.Identity;
        
        Assert.Equal(Vector2.Zero, t.Origin);
    }

    [Fact]
    public void Transform3D_Identity_Works()
    {
        var t = Transform3D.Identity;
        
        Assert.Equal(Vector3.Zero, t.Origin);
        Assert.Equal(Basis.Identity, t.Basis);
    }

    [Fact]
    public void Basis_Identity_Works()
    {
        var b = Basis.Identity;
        
        Assert.Equal(new Vector3(1, 0, 0), b.X);
        Assert.Equal(new Vector3(0, 1, 0), b.Y);
        Assert.Equal(new Vector3(0, 0, 1), b.Z);
    }

    #endregion

    #region Reference Types - Type Availability
    
    // Note: Creating GodotObject instances (Node, Node2D, etc.) and using StringName/NodePath
    // requires additional integration work with the GodotSharp bindings.
    // For now, we verify the types are available and properly bound.

    [Fact]
    public void Node_Type_IsAvailable()
    {
        var type = typeof(Node);
        
        Assert.NotNull(type);
        Assert.True(type.IsSubclassOf(typeof(GodotObject)));
    }

    [Fact]
    public void Node2D_Type_IsAvailable()
    {
        var type = typeof(Node2D);
        
        Assert.NotNull(type);
        Assert.True(type.IsSubclassOf(typeof(Node)));
    }

    [Fact]
    public void Node3D_Type_IsAvailable()
    {
        var type = typeof(Node3D);
        
        Assert.NotNull(type);
        Assert.True(type.IsSubclassOf(typeof(Node)));
    }

    [Fact]
    public void PackedScene_Type_IsAvailable()
    {
        var type = typeof(PackedScene);
        
        Assert.NotNull(type);
        Assert.True(type.IsSubclassOf(typeof(Resource)));
    }

    [Fact]
    public void Resource_Type_IsAvailable()
    {
        var type = typeof(Resource);
        
        Assert.NotNull(type);
        Assert.True(type.IsSubclassOf(typeof(RefCounted)));
    }

    [Fact]
    public void SceneTree_Type_IsAvailable()
    {
        var type = typeof(SceneTree);
        
        Assert.NotNull(type);
        Assert.True(type.IsSubclassOf(typeof(MainLoop)));
    }

    [Fact]
    public void StringName_Type_IsAvailable()
    {
        var type = typeof(StringName);
        
        Assert.NotNull(type);
        // StringName is a reference type in GodotSharp
        Assert.True(type.IsClass);
    }

    [Fact]
    public void NodePath_Type_IsAvailable()
    {
        var type = typeof(NodePath);
        
        Assert.NotNull(type);
        // NodePath is a reference type in GodotSharp
        Assert.True(type.IsClass);
    }

    #endregion
}
