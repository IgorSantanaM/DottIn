using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Employees
{
    public class Employee : Entity<Guid>, IAggregateRoot
    {
        public string Name { get; private set; }
        public string CPF { get; private set; }
        public string ImageUrl { get; private set; }
        public Guid BranchId { get; private set; }
        public DateTime? Created { get; private init; }
        public TimeOnly StartWorkTime { get; private set; }
        public TimeOnly EndWorkTime { get; private set; }
        public TimeOnly IntervalTime { get; private set; }

        private Employee() { }

        public Employee(string name,
                        string cpf,
                        string imageUrl,
                        Guid branchId,
                        TimeOnly startWorkTime,
                        TimeOnly endWorkTime,
                        TimeOnly intervalTime)
        {
            if (string.IsNullOrEmpty(name))
                throw new DomainException("O nome não pode ser vazio.");

            if (string.IsNullOrEmpty(cpf))
                throw new DomainException("O CPF não pode vazio.");

            if (string.IsNullOrEmpty(imageUrl))
                throw new DomainException("A Imagem deve ser inserida.");

            if (branchId == Guid.Empty)
                throw new DomainException("O funcionário tem que ser vinculado à uma empresa.");

            if (TimeOnly.MinValue == startWorkTime)
                throw new DomainException("O inicio do horario de funcionamento é obrigatorio.");

            if (TimeOnly.MinValue == endWorkTime)
                throw new DomainException("O fim do horario de funcionamento é obrigatorio.");

            if (TimeOnly.MinValue == intervalTime)
                throw new DomainException("O inicio do horario de intervalo é obrigatorio.");

            Name = name;
            CPF = cpf;
            ImageUrl = imageUrl;
            BranchId = branchId;
            Created = DateTime.UtcNow;
            StartWorkTime = startWorkTime;
            EndWorkTime = endWorkTime;
            IntervalTime = intervalTime;
        }
    }
}
