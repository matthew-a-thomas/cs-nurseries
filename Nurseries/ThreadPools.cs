namespace Nurseries;

/// <summary>
/// Shared instances of <see cref="IThreadPool"/>.
/// </summary>
public static class ThreadPools
{
    /// <summary>
    /// A <see cref="IThreadPool"/> that schedules work on the default thread pool.
    /// </summary>
    public static readonly ThreadPoolAdapter Default = new(true);
}