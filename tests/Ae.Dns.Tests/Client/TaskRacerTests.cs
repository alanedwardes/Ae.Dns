using System;
using Ae.Dns.Client.Internal;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class TaskRacerTests
    {
        [Fact]
        public async Task TestUseFastestResult()
        {
            var task1 = Task.Run(new Func<bool>(() => true));
            var task2 = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return false;
            });

            Assert.True(await await TaskRacer.RaceTasks(new[] { task1, task2 }));
        }

        [Fact]
        public async Task TestUseNonFaultedResult()
        {
            var task1 = Task.Run<bool>(new Func<bool>(() => throw new Exception()));
            var task2 = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return true;
            });

            Assert.True(await await TaskRacer.RaceTasks(new[] { task1, task2 }));
        }

        [Fact]
        public async Task TestCancelledResultThrows()
        {
            var cancelledToken = new CancellationTokenSource();
            cancelledToken.Cancel();

            var task1 = Task.Run(() => true, cancelledToken.Token);
            var task2 = Task.Run(() => true);

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await await TaskRacer.RaceTasks(new[] { task1, task2 }));
        }

        [Fact]
        public async Task TestAllResultsFaulted()
        {
            var task1 = Task.Run(new Func<bool>(() => throw new InvalidOperationException()));
            var task2 = Task.Run(new Func<bool>(() => throw new AbandonedMutexException()));

            // Will always be the last exception
            await Assert.ThrowsAsync<AbandonedMutexException>(async () => await await TaskRacer.RaceTasks(new[] { task1, task2 }));
        }

        [Fact]
        public async Task TestUseSuccessfulResult()
        {
            var task1 = Task.Run(new Func<bool>(() => true));
            var task2 = Task.Run(new Func<bool>(() => false));

            // It's not possible to determine which task wins due to timing varience
            await await TaskRacer.RaceTasks(new[] { task1, task2 });
        }

        [Fact]
        public async Task TestNoTasksSupplied()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await await TaskRacer.RaceTasks(Array.Empty<Task<bool>>()));
        }
    }
}
