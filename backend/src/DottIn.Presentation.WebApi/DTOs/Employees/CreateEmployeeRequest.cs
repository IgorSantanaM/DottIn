namespace DottIn.Presentation.WebApi.DTOs.Employees
{
    public record CreateEmployeeRequest(
            string Name,
            string Cpf,
            IFormFile Image,
            TimeOnly StartWorkTime,
            TimeOnly EndWorkTime,
            TimeOnly IntervalStart,
            TimeOnly IntervalEnd);
}
