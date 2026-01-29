using System;

namespace twodog;

/// <summary>
/// A handle to a native GodotInstance that uses P/Invoke for method calls.
/// This avoids the crash that occurs when trying to use GodotObject.InstanceFromId
/// in the libgodot embedding scenario.
/// </summary>
public sealed class GodotInstanceHandle : IDisposable
{
    private readonly nint _nativePtr;
    private bool _disposed;

    internal GodotInstanceHandle(nint nativePtr)
    {
        if (nativePtr == IntPtr.Zero)
            throw new ArgumentException("Native pointer cannot be zero", nameof(nativePtr));
        _nativePtr = nativePtr;
    }

    /// <summary>
    /// Gets the native pointer to the GodotInstance.
    /// </summary>
    public nint NativePtr => _nativePtr;

    /// <summary>
    /// Runs one iteration of the Godot main loop.
    /// </summary>
    /// <returns>True if the engine should quit, false otherwise.</returns>
    public bool Iteration()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GodotInstanceHandle));
        return LibGodot.CallGodotInstanceIteration(_nativePtr);
    }

    // Note: GodotInstance::stop() is not exposed via ClassDB in Godot,
    // so we cannot call it.

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Note: The Godot instance lifecycle is managed by Engine.
        // We don't destroy here to avoid double-free issues.
    }
}
