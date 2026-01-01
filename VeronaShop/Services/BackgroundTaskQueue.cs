using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VeronaShop.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<Func<IServiceProvider, CancellationToken, Task>> _workItems = new();
        private readonly SemaphoreSlim _signal = new(0);

        public ValueTask QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));
            _workItems.Enqueue(workItem);
            _signal.Release();
            return ValueTask.CompletedTask;
        }

        public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem;
        }
    }
}
