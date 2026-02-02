using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;
using System.Text.RegularExpressions;

namespace DottIn.Domain.ValueObjects
{
    public class Address : ValueObject
    {
        public string Street { get; private set; }
        public int Number { get; private set; }
        public string? Complement { get; private set; }
        public string City { get; private set; }
        public string State { get; private set; }
        public string ZipCode { get; private set; }

        private Address() { }

        public Address(string street, int number, string city, string state, string zipCode, string? complement = null)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new DomainException("A Rua é obrigatória.");

            if (number < 0)
                throw new DomainException("O Número deve ser maior que zero. Se for S/N, use o número 0 ou trate como regra específica.");

            if (string.IsNullOrWhiteSpace(city))
                throw new DomainException("A Cidade é obrigatória.");

            if (string.IsNullOrWhiteSpace(state))
                throw new DomainException("O Estado é obrigatório.");

            if (state.Length != 2)
                throw new DomainException("O Estado (UF) deve ter exatamente 2 letras (ex: SP, RJ).");

            if (string.IsNullOrWhiteSpace(zipCode))
                throw new DomainException("O CEP é obrigatório.");

            var zipDigits = SanitizeZipCode(zipCode);

            if (zipDigits.Length != 8)
                throw new DomainException($"CEP Inválido: {zipCode}. O CEP deve conter 8 números.");

            ZipCode = zipDigits;

            Street = street;
            Number = number;
            City = city;
            State = state.ToUpper(); 
            Complement = complement;
        }

        public string GetFormattedZipCode()
        {
            return Convert.ToUInt64(ZipCode).ToString(@"00000\-000");
        }

        private static string SanitizeZipCode(string value)
        {
            char[] buffer = new char[value.Length];
            int index = 0;
            foreach (var c in value)
            {
                if (char.IsDigit(c))
                    buffer[index++] = c;
            }
            return new string(buffer, 0, index);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ZipCode;
            yield return Number;
            yield return Complement ?? string.Empty;
        }
    }
}