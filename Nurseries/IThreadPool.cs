namespace Nurseries;

using System;

/// <summary>
/// Executes callbacks on other threads.
/// </summary>
public interface IThreadPool
{
    /// <summary>
    /// Executes the given callback with the given state "soon".
    /// </summary>
    /// <remarks>
    /// Implementations are responsible for flowing the execution context.
    /// </remarks>
    void Start(Action<object> callback, object state);
}