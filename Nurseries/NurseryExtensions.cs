namespace Nurseries;

using System;
using System.Threading;

/// <summary>
/// Extension methods for <see cref="Nursery"/>.
/// </summary>
public static class NurseryExtensions
{
    static readonly ManualResetEventSlim SharedManualResetEvent = new();

    /// <summary>
    /// Sleeps for the given duration, taking into account the nursery's cancellation token.
    /// </summary>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the nursery's cancellation token is canceled before the given duration elapses.
    /// </exception>
    public static void Sleep(
        this Nursery nursery,
        TimeSpan duration)
    {
        var token = nursery.Token;
        SharedManualResetEvent.Wait(duration, token);
    }

    /// <summary>
    /// Repeatedly spawns the given child task until it either completes successfully or else
    /// <paramref name="handleException"/> returns <c>false</c>.
    /// </summary>
    /// <param name="nursery">The <see cref="Nursery"/> in which to spawn the child tasks.</param>
    /// <param name="task">The child task to spawn.</param>
    /// <param name="handleException">
    /// Handles any thrown exceptions. Return <c>true</c> if the child task should be spawned again. Return <c>false</c>
    /// if the exception should trickle up.
    /// </param>
    public static void StartWithRetry(
        this Nursery nursery,
        Action task,
        Func<Exception, bool> handleException)
    {
        nursery.Start(() =>
        {
            while (true)
            {
                try
                {
                    task();
                    break;
                }
                catch (Exception e)
                {
                    if (!handleException(e))
                        throw;
                }
            }
        });
    }
}