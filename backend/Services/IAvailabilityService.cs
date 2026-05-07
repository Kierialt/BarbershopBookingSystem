namespace backend.Services;

public interface IAvailabilityService
{
    Task<IReadOnlyList<DateTime>> GetAvailableSlotsUtcAsync(int barberId, int serviceId, DateOnly localDate,
        CancellationToken cancellationToken = default);
}
