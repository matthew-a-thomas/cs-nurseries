namespace Nurseries.Tests;

using System;
using System.Threading;
using Xunit;

public class NurseryExtensionsClass
{
    public class SleepMethodShould
    {
        [Fact]
        public void CompleteSuccessfully()
        {
            using var nursery = new Nursery();
            nursery.Sleep(TimeSpan.Zero);
        }

        [Fact]
        public void ThrowOperationCaneledExceptionAfterTheNurseryStops()
        {
            using var nursery = new Nursery();
            nursery.Stop();
            Assert.ThrowsAny<OperationCanceledException>(() => nursery.Sleep(TimeSpan.FromDays(1)));
        }
    }

    public class StartWithRetryMethodShould
    {
        [Fact]
        public void RetryUntilSuccessful()
        {
            var invocationCount = 0;
            using var nursery = new Nursery();
            nursery.StartWithRetry(
                () =>
                {
                    if (Interlocked.Increment(ref invocationCount) < 5)
                        throw new Exception();
                },
                _ => true
            );
        }

        [Fact]
        public void RunJustFineWithNormalTask()
        {
            using var nursery = new Nursery();
            nursery.StartWithRetry(
                () => {},
                _ => throw new Exception()
            );
        }

        [Fact]
        public void PassExceptionsToHandler()
        {
            var nursery = new Nursery();
            var invocationCount = 0;
            var handled = false;
            nursery.StartWithRetry(
                () =>
                {
                    Interlocked.Increment(ref invocationCount);
                    throw new UniqueException();
                },
                _ =>
                {
                    handled = true;
                    return false;
                }
            );
            try
            {
                nursery.Dispose();
            }
            catch (AggregateException e) when (e.GetBaseException() is UniqueException)
            {
                //
            }
            Assert.True(handled);
        }

        sealed class UniqueException : Exception
        {}
    }
}