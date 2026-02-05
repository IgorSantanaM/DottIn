using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Employees.Events;
using DottIn.Domain.Storage;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DottIn.Infra.Messaging.Consumers
{
    public class EmployeeImageConsumer(IFileStorageService fileStorageService,
                            IEmployeeRepository employeeRepository,
                            IUnitOfWork unitOfWork,
                            ILogger<EmployeeImageConsumer> logger)
                            : IConsumer<EmployeeImageAdded>
    {
        public async Task Consume(ConsumeContext<EmployeeImageAdded> context)
        {
            var message = context.Message;

            if (message is null)
            {
                logger.LogInformation($"The message that was delivered by the application is null: {nameof(EmployeeImageConsumer)}");
                return;
            }

            var fileExists = await fileStorageService.ExistsAsync(message.ImageName, context.CancellationToken);
            if (fileExists)
            {
                logger.LogInformation("Image already exists for employee.");  // TODO: USE SIGNALR FOR NOTIFICATION
                return;
            }

            var imageUrl = await fileStorageService.UploadAsync(message.ImageStream, message.ImageName, message.ImageContentType, context.CancellationToken);

            await employeeRepository.AddEmployeeImageAsync(message.EmployeeId, imageUrl, context.CancellationToken);

            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
