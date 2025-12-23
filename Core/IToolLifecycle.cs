namespace ClickUpDesktopPowerTools.Core;

/// <summary>
/// Lifecycle contract for tools that need runtime integration.
/// Tools implement this interface to receive runtime availability notifications.
/// Per RULE.md: No framework-style abstractions - this is a simple contract.
/// </summary>
public interface IToolLifecycle
{
    /// <summary>
    /// Called when the tool is enabled.
    /// Tools MUST NOT block in this method. Use fire-and-forget async patterns if needed.
    /// </summary>
    void OnEnable();

    /// <summary>
    /// Called when the tool is disabled.
    /// Tools MUST NOT block in this method. Use fire-and-forget async patterns if needed.
    /// </summary>
    void OnDisable();

    /// <summary>
    /// Called when the runtime is connected and ready.
    /// Tools receive a RuntimeContext that provides safe access to runtime capabilities.
    /// Tools MUST NOT block in this method. Use fire-and-forget async patterns if needed.
    /// </summary>
    void OnRuntimeReady(RuntimeContext ctx);

    /// <summary>
    /// Called when the runtime disconnects.
    /// Tools should clear runtime-dependent state.
    /// Tools MUST NOT block in this method. Use fire-and-forget async patterns if needed.
    /// </summary>
    void OnRuntimeDisconnected();
}

