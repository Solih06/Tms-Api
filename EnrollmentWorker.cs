public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ProcessBatchAsync()
    {
        // Create a new scope for each batch
        using var scope = _scopeFactory.CreateScope();

        // Resolve the scoped service inside this scope
        var service = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // Example usage
        var record = await service.EnrollAsync("student123", "courseABC");
        Console.WriteLine($"Enrollment record created: {record.Id}");
    }
}

