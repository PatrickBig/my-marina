namespace MyMarina.Application.Search;

public sealed record SearchSlipsQuery(
    decimal Latitude,
    decimal Longitude,
    decimal RadiusMiles,
    DateOnly ArrivesAt,
    DateOnly DepartsAt,
    decimal? VesselLength,
    decimal? VesselBeam,
    decimal? VesselDraft,
    string? SlipType,
    bool? HasElectric,
    bool? HasWater,
    int Page,
    int PageSize,
    bool IncludeDemo
);

public sealed record GetPublicSlipDetailQuery(Guid SlipId, bool IncludeDemo);
