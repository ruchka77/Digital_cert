namespace DigitalCertHelper.Helpers;

public static class StaTask
{
    /// <summary>Runs COM work on its own STA thread so that a slow Outlook start does not freeze the UI.</summary>
    public static Task Run(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "Outlook STA",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
