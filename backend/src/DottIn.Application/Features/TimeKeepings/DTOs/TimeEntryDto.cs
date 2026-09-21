using DottIn.Application.Shared.DTOS;
﻿using DottIn.Domain.TimeKeepings;

namespace DottIn.Application.Features.TimeKeepings.DTOs;

public record TimeEntryDto(
    DateTime Timestamp,
    TimeKeepingType Type,
    GeolocationDto? Geolocation,
    ClockSource Source);
