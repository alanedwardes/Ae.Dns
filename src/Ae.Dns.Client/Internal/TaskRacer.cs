using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Internal
{
    internal static class TaskRacer
    {
        public static async Task<Task<TResult>> RaceTasks<TResult>(IEnumerable<Task<TResult>> tasks, Func<Task<TResult>, Task<bool>> isFailed)
        {
            var queue = tasks.ToList();

            Task<TResult> task;
            do
            {
                task = await Task.WhenAny(queue);
                queue.Remove(task);
            }
            while (await isFailed(task) && queue.Count > 0);

            return task;
        }

        public static async Task<Task<TResult>> RaceTasks<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return await RaceTasks(tasks, task => Task.FromResult(task.IsFaulted));
        }
    }
}
