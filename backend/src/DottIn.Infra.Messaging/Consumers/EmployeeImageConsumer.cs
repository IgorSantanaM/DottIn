using DottIn.Application.Features.Employees.Events;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Storage;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DottIn.Infra.Messaging.Consumers
{
    public class EmployeeImageConsumer(
        IFileStorageService fileStorageService,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        ILogger<EmployeeImageConsumer> logger) : IConsumer<EmployeeImageAdded>
    {
        public async Task Consume(ConsumeContext<EmployeeImageAdded> context)
        {
            var message = context.Message;

            if (message is null)
            {
                logger.LogWarning("The message delivered is null: {Consumer}", nameof(EmployeeImageConsumer));
                return;
            }

            var fileExists = await fileStorageService.ExistsAsync(message.ImageName, context.CancellationToken);
            if (fileExists)
            {
                logger.LogInformation("Image already exists for employee {EmployeeId}", message.EmployeeId);
                return;
            }

            using var imageStream = new MemoryStream(message.ImageData);

            var imageUrl = await fileStorageService.UploadAsync(
                imageStream,
                message.ImageName,
                message.ImageContentType,
                context.CancellationToken);

            await employeeRepository.AddEmployeeImageAsync(message.EmployeeId, imageUrl, context.CancellationToken);

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "Image uploaded for employee {EmployeeId}. URL: {ImageUrl}",
                message.EmployeeId,
                imageUrl);
        }
    }
}
