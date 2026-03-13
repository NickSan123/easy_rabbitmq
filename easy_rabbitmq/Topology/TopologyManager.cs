namespace easy_rabbitmq.Topology;

public class TopologyManager
{
    private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Ready => _tcs.Task;

    public void SetReady() => _tcs.TrySetResult(true);

    public void SetFailed(Exception ex) => _tcs.TrySetException(ex);
}
