using System;
using System.Threading;
using System.Threading.Tasks;

namespace VeronaShop.Services
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem);
        ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
