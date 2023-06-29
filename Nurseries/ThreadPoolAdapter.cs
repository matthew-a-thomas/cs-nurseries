namespace Nurseries;

using System;
using System.Threading;

/// <summary>
/// An implementation of <see cref="IThreadPool"/> that delegates to the <see cref="ThreadPool"/>.
/// </summary>
public sealed class ThreadPoolAdapter : IThreadPool
{
    readonly bool _preferLocal;

    /// <summary>
    /// Creates a new <see cref="ThreadPoolAdapter"/>.
    /// </summary>
    /// <param name="preferLocal">
    /// <c>true</c> to prefer queueing the work item in a queue close to the current thread; <c>false</c> to prefer
    /// queueing the work item to the thread pool's shared queue.
    /// </param>
    public ThreadPoolAdapter(bool preferLocal)
    {
        _preferLocal = preferLocal;
    }

    /// <inheritdoc />
    public void Start(Action<object> callback, object state) =>
        ThreadPool.QueueUserWorkItem(callback, state, _preferLocal);
}