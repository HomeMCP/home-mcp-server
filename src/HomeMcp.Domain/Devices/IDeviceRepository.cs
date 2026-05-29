using CSharpFunctionalExtensions;
using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Devices;

public interface IDeviceRepository
{
    Task<Maybe<Device>> GetByIdAsync(DeviceId id, CancellationToken ct = default);
    Task<UnitResult<DomainError>> AddAsync(Device device, CancellationToken ct = default);
    Task<UnitResult<DomainError>> UpdateAsync(Device device, CancellationToken ct = default);
}
