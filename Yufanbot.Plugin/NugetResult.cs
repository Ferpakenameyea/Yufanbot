internal readonly struct NugetResult<T> where T : class
{
    public T? Value { get; }
    public NugetResolveStatus Status { get; }
    public bool Success => Status == NugetResolveStatus.Ok;

    public NugetResult(T Data)
    {
        Value = Data;
        Status = NugetResolveStatus.Ok;
    }

    public NugetResult(NugetResolveStatus status)
    {
        if (status == NugetResolveStatus.Ok)
        {
            throw new ArgumentException("Cannot have a nuget result with OK status but no data.");
        }

        Status = status;
        Value = null;
    }

    public readonly NugetResult<T> IfFail(Action<NugetResolveStatus> action)
    {
        if (Status != NugetResolveStatus.Ok)
        {
            action.Invoke(Status);
        }

        return this;
    }
}

internal enum NugetResolveStatus
{
    InvalidString,
    NotFound,
    Ok,
    InvalidVersion,
}