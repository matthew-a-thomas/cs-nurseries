// ReSharper disable AccessToModifiedClosure
// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AccessToDisposedClosure
namespace Nurseries.Tests;

using System;
using System.Threading;
using Xunit;

public class NurseryClass
{
    public class DisposeMethodShould
    {
        [Fact]
        public void ReturnWhenAllSpawnedTasksHaveCompleted()
        {
            using var nursery = new Nursery();
            nursery.Start(() =>
            {
                nursery.Start(() =>
                {
                    //
                });
            });
        }

        [Fact]
        public void ThrowAnyExceptionsFromSpawnedTasks()
        {
            var nursery = new Nursery();
            nursery.Start(() =>
            {
                nursery.Start(() =>
                {
                    nursery.Start(() => throw new UniqueException());
                });
            });
            try
            {
                nursery.Dispose();
                throw new Exception("Did not throw the exception");
            }
            catch (AggregateException aggregateException)
            {
                Assert.Collection(
                    aggregateException.InnerExceptions,
                    e => Assert.True(e is UniqueException)
                );
            }
        }

        sealed class UniqueException : Exception
        {}
    }

    public class StartMethodShould
    {
        [Fact]
        public void KickOffWorkThatWillBeCompleted()
        {
            var completionCount = 0;
            using (var nursery = new Nursery())
            {
                nursery.Start(() =>
                {
                    nursery.Start(() =>
                    {
                        nursery.Start(() =>
                        {
                            Interlocked.Increment(ref completionCount);
                        });
                        Interlocked.Increment(ref completionCount);
                    });
                    Interlocked.Increment(ref completionCount);
                });
            }
            Assert.Equal(3, completionCount);
        }

        [Fact]
        public void RunWorkOnGivenThreadPool()
        {
            var threadPool = new RunCountingThreadPoolDecorator(ThreadPools.Default);
            using (var nursery = new Nursery(threadPool))
            {
                nursery.Start(() => {});
            }
            Assert.Equal(1, threadPool.RunCount);
        }

        [Fact]
        public void ThrowIfNurseryHasAlreadyStopped()
        {
            Nursery nursery;
            using (nursery = new Nursery())
            {
                //
            }

            bool threw;
            try
            {
                threw = false;
                nursery.Start(default!);
            }
            catch (Exception)
            {
                threw = true;
            }
            Assert.True(threw);
        }

        [Fact]
        public void ExposeExceptionsFromOtherFailedTasks()
        {
            var exception = new Exception();
            Nursery nursery = default!;
            try
            {
                using (nursery = new Nursery())
                {
                    nursery.Start(() => throw exception);
                }
            }
            catch
            {
                //
            }

            bool okay;
            try
            {
                okay = false;
                nursery.Start(default!);
            }
            catch (Exception e) when (e.InnerException!.GetBaseException() == exception)
            {
                okay = true;
            }
            Assert.True(okay);
        }

        sealed class RunCountingThreadPoolDecorator : IThreadPool
        {
            int _runCount;
            readonly IThreadPool _threadPool;

            public RunCountingThreadPoolDecorator(IThreadPool threadPool)
            {
                _threadPool = threadPool;
            }

            public int RunCount => _runCount;

            public void Start(Action<object> callback, object state)
            {
                Interlocked.Increment(ref _runCount);
                _threadPool.Start(callback, state);
            }
        }
    }

    public class StopMethodShould
    {
        [Fact]
        public void CancelTheCancellationToken()
        {
            using var nursery = new Nursery();
            nursery.Start(() =>
            {
                Assert.False(nursery.Token.IsCancellationRequested);
                nursery.Stop();
                Assert.True(nursery.Token.IsCancellationRequested);
            });
        }
    }
}