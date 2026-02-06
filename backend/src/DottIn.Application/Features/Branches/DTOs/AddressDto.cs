namespace DottIn.Application.Features.Branches.DTOs;

public record AddressDto(string Street,
                    int Number,
                    string? Complement,
                    string City,
                    string State,
                    string ZipCode);
