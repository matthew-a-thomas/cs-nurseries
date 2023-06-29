namespace Nurseries;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

/// <summary>
/// An abstraction of threading in which it's impossible for exceptions to escape unnoticed.
/// </summary>
/// <remarks>
/// This is inspired by
/// <a href="https://vorpus.org/blog/notes-on-structured-concurrency-or-go-statement-considered-harmful/">this article</a>.
/// </remarks>
public sealed class Nursery : IDisposable
{
    CancellationTokenSource? _cancellationTokenSource;
    readonly List<Exception> _exceptions = new();
    readonly object _gate = new();
    int _numRunningThreads;
    readonly IThreadPool _threadPool;

    /// <summary>
    /// Creates a new <see cref="Nursery"/> that spawns tasks onto the given thread pool.
    /// </summary>
    public Nursery(IThreadPool? threadPool = null)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _threadPool = threadPool ?? ThreadPools.Default;
        Token = _cancellationTokenSource.Token;
    }

    /// <summary>
    /// A <see cref="CancellationToken"/> that is canceled when the nursery stops or an exception is thrown in a child
    /// task.
    /// </summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// Waits for all spawned tasks to complete.
    /// </summary>
    /// <exception cref="AggregateException">Thrown when child tasks throw any exceptions.</exception>
    public void Dispose()
    {
        lock (_gate)
        {
            try
            {
                while (_numRunningThreads > 0)
                {
                    Monitor.Wait(_gate);
                }

                if (_exceptions.Count == 0)
                    return;
                throw new AggregateException(_exceptions);
            }
            finally
            {
                StopCore();
            }
        }
    }

    /// <summary>
    /// Spawns a new child task.
    /// </summary>
    /// <param name="task">The child task.</param>
    /// <exception cref="Exception">Thrown if the <see cref="Nursery"/> has already stopped.</exception>
    public void Start(Action task)
    {
        lock (_gate)
        {
            if (_cancellationTokenSource is null)
            {
                if (_exceptions.Count == 0)
                    throw new Exception("This nursery has already stopped");
                throw new Exception("This nursery has already stopped because of failures in other tasks", new AggregateException(_exceptions));
            }
            ++_numRunningThreads;
            var context = new ThreadContext(
                this,
                task);
            _threadPool.Start(ThreadStart, context);
            Monitor.Pulse(_gate);
        }
    }


    /// <summary>
    /// Requests that all current tasks end promptly, and prevents future calls to <see cref="Start"/> from succeeding.
    /// </summary>
    public void Stop()
    {
        lock (_gate)
        {
            StopCore();
        }
    }

    void StopCore()
    {
        Debug.Assert(Monitor.IsEntered(_gate));
        if (_cancellationTokenSource is null)
            return;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }

    static void ThreadStart(object state)
    {
        var (nursery, task) = (ThreadContext)state;
        nursery.ThreadStartCore(task);
    }

    void ThreadStartCore(Action task)
    {
        try
        {
            Token.ThrowIfCancellationRequested();
            task();
            Monitor.Enter(_gate);
        }
        catch (Exception e)
        {
            Monitor.Enter(_gate);
            _exceptions.Add(e);
            StopCore();
        }
        finally
        {
            try
            {
                --_numRunningThreads;
                Monitor.Pulse(_gate);
            }
            finally
            {
                Monitor.Exit(_gate);
            }
        }
    }

    sealed record ThreadContext(
        Nursery Nursery,
        Action Task);
}