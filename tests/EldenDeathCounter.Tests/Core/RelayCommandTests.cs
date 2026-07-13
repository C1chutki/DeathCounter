using EldenDeathCounter.UI;

namespace EldenDeathCounter.Tests.Core;

public sealed class RelayCommandTests
{
    [Fact]
    public async Task AsyncCommandBlocksReentryAndReportsExceptions()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        var command = new RelayCommand(async () =>
        {
            executions++;
            started.SetResult();
            await release.Task;
            throw new InvalidOperationException("expected");
        }, failure.SetResult);

        command.CanExecuteChanged += (_, _) =>
        {
            if (command.CanExecute(null))
            {
                completed.TrySetResult();
            }
        };

        command.Execute(null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        command.Execute(null);

        Assert.False(command.CanExecute(null));
        Assert.Equal(1, executions);

        release.SetResult();

        var exception = await failure.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsType<InvalidOperationException>(exception);
    }
}
