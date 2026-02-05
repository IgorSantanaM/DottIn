using DottIn.Application.Features.Employees.Events;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Storage;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DottIn.Infra.Messaging.Consumers
{
    public class UpdateEmployeeImageConsumer(
        IEmployeeRepository employeeRepository,
        IFileStorageService fileStorageService,
        ILogger<UpdateEmployeeImageConsumer> logger,
        IUnitOfWork unitOfWork) : IConsumer<EmployeeImageUpdated>
    {
        public async Task Consume(ConsumeContext<EmployeeImageUpdated> context)
        {
            var message = context.Message;

            if (message is null)
            {
                logger.LogWarning("The message delivered is null: {Consumer}", nameof(UpdateEmployeeImageConsumer));
                return;
            }

            try
            {
                var employee = await employeeRepository.GetByIdAsync(message.EmployeeId);

                if (employee is null)
                {
                    logger.LogWarning("Employee {EmployeeId} not found", message.EmployeeId);
                    return;
                }

                var oldImageUrl = employee.ImageUrl;

                var newImageUrl = await fileStorageService.UploadAsync(
                    message.ImageStream,
                    message.ImageName,
                    message.ImageContentType,
                    context.CancellationToken);

                await employeeRepository.UpdateEmployeeImageAsync(
                    message.EmployeeId,
                    newImageUrl,
                    context.CancellationToken);

                await unitOfWork.SaveChangesAsync(context.CancellationToken);

                logger.LogInformation(
                    "Updated image for employee {EmployeeId}. New URL: {ImageUrl}",
                    message.EmployeeId,
                    newImageUrl);

                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        var oldImageExists = await fileStorageService.ExistsAsync(
                            oldImageUrl,
                            context.CancellationToken);

                        if (oldImageExists)
                        {
                            await fileStorageService.DeleteAsync(oldImageUrl, context.CancellationToken);
                            logger.LogInformation(
                                "Deleted old image for employee {EmployeeId}",
                                message.EmployeeId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Failed to delete old image for employee {EmployeeId}",
                            message.EmployeeId);
                    }
                }
            }
            finally
            {
                await message.ImageStream.DisposeAsync();
            }
        }
    }
}
