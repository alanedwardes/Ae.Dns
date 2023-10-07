using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Internal
{
    internal static class TaskRacer
    {
        public static async Task<Task<TResult>> RaceTasks<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            var queue = tasks.ToList();

            Task<TResult> task;
            do
            {
                task = await Task.WhenAny(queue);
                queue.Remove(task);
            }
            while (task.IsFaulted && queue.Count > 0);

            return task;
        }
    }
}
