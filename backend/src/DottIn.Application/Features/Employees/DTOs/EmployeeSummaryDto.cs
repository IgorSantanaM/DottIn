using DottIn.Application.Shared.DTOS;

namespace DottIn.Application.Features.Employees.DTOs;

public record EmployeeSummaryDto(Guid EmployeeId, 
                    string Name, 
                    DocumentDTO Document, 
                    string? ImageUrl, 
                    string BranchName, 
                    TimeOnly StartWorkTime, 
                    TimeOnly EndWorkTime, 
                    TimeOnly IntervalStart, 
                    TimeOnly IntervalEnd, 
                    bool IsActive,
                    bool AllowOvernightShifts);
