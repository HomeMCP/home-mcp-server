using System.Text.Json;
using CSharpFunctionalExtensions;
using Dapper;
using HomeMcp.Domain.Devices;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Infrastructure.Persistence.Repositories;

public sealed class SqliteDeviceRepository : IDeviceRepository
{
    private readonly DbSession _db;
    private readonly TimeProvider _timeProvider;

    public SqliteDeviceRepository(DbSession db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Maybe<Device>> GetByIdAsync(DeviceId id, CancellationToken ct = default)
    {
        var row = await _db.Connection.QuerySingleOrDefaultAsync<DeviceRow>(
            "SELECT id AS Id, primary_user_id AS PrimaryUserId, is_shared AS IsShared, location AS Location, " +
            "token_hash AS TokenHash, capabilities AS Capabilities, paired_at AS PairedAt, last_seen_at AS LastSeenAt " +
            "FROM devices WHERE id = @Id",
            new { Id = id.Value },
            _db.Transaction);

        return row is null ? Maybe<Device>.None : Reconstitute(row);
    }

    public async Task<UnitResult<DomainError>> AddAsync(Device device, CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            @"INSERT INTO devices (id, primary_user_id, is_shared, location, token_hash, capabilities, paired_at)
              VALUES (@Id, @PrimaryUserId, @IsShared, @Location, @TokenHash, @Capabilities, @PairedAt)",
            new
            {
                Id = device.Id.Value,
                PrimaryUserId = device.PrimaryUserId.Value,
                IsShared = (int)device.SharingMode,
                Location = device.Location.GetValueOrDefault(),
                TokenHash = device.TokenHash,
                Capabilities = JsonSerializer.Serialize(device.Capabilities),
                PairedAt = device.PairedAt.ToUnixTimeMilliseconds()
            },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> UpdateAsync(Device device, CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            @"UPDATE devices SET
                token_hash = @TokenHash,
                capabilities = @Capabilities,
                last_seen_at = @LastSeenAt
              WHERE id = @Id",
            new
            {
                Id = device.Id.Value,
                TokenHash = device.TokenHash,
                Capabilities = JsonSerializer.Serialize(device.Capabilities),
                LastSeenAt = device.LastSeenAt.HasValue
                    ? device.LastSeenAt.Value.ToUnixTimeMilliseconds()
                    : (long?)null
            },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    private Device Reconstitute(DeviceRow row)
    {
        var capabilities = JsonSerializer.Deserialize<CapabilitiesDto>(row.Capabilities) ?? new CapabilitiesDto();
        return Device.Reconstitute(
            DeviceId.From(row.Id),
            UserId.From(row.PrimaryUserId),
            (DeviceSharingMode)row.IsShared,
            Maybe.From(row.Location),
            row.TokenHash,
            new DeviceCapabilities(
                capabilities.HasStt,
                capabilities.HasTts,
                capabilities.HasAudioPlayback,
                capabilities.HasMediaPlayer,
                capabilities.HasDisplay,
                capabilities.HasWakeWord),
            DateTimeOffset.FromUnixTimeMilliseconds(row.PairedAt),
            row.LastSeenAt is { } ts
                ? Maybe.From(DateTimeOffset.FromUnixTimeMilliseconds(ts))
                : Maybe<DateTimeOffset>.None,
            _timeProvider);
    }

    private sealed record DeviceRow(
        string Id,
        string PrimaryUserId,
        long IsShared,
        string? Location,
        string TokenHash,
        string Capabilities,
        long PairedAt,
        long? LastSeenAt);

    private sealed class CapabilitiesDto
    {
        public bool HasStt { get; set; }
        public bool HasTts { get; set; }
        public bool HasAudioPlayback { get; set; }
        public bool HasMediaPlayer { get; set; }
        public bool HasDisplay { get; set; }
        public bool HasWakeWord { get; set; }
    }
}
