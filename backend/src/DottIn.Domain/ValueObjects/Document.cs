using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;
using System.Linq;

namespace DottIn.Domain.ValueObjects
{
    public class Document : ValueObject
    {
        public string Value { get; private set; }
        public DocumentType Type { get; private set; }

        private Document() { }

        public Document(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Documento vazio");

            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 11)
            {
                if (!ValidateCpf(digitsOnly))
                    throw new DomainException($"CPF Inválido: {value}");
                Type = DocumentType.CPF;
            }
            else if (digitsOnly.Length == 14)
            {
                if (!ValidateCnpj(digitsOnly))
                    throw new DomainException($"CNPJ Inválido: {value}");
                Type = DocumentType.CNPJ;
            }
            else
            {
                throw new DomainException($"Documento deve ter 11 (CPF) ou 14 (CNPJ) dígitos.");
            }

            Value = digitsOnly;
        }

        private bool ValidateCpf(string cpf)
        {
            if (IsRepeatedDigits(cpf)) return false;

            int[] multiplier1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplier2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int sum = 0;

            for (int i = 0; i < 9; i++)
                sum += (tempCpf[i] - '0') * multiplier1[i]; 

            int remainder = sum % 11;
            remainder = remainder < 2 ? 0 : 11 - remainder;

            string digit = remainder.ToString();
            tempCpf = tempCpf + digit;
            sum = 0;

            for (int i = 0; i < 10; i++)
                sum += (tempCpf[i] - '0') * multiplier2[i]; 

            remainder = sum % 11;
            remainder = remainder < 2 ? 0 : 11 - remainder;

            digit = digit + remainder.ToString();

            return cpf.EndsWith(digit);
        }

        private bool ValidateCnpj(string cnpj)
        {
            if (IsRepeatedDigits(cnpj)) return false;

            int[] multiplier1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplier2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCnpj = cnpj.Substring(0, 12);
            int sum = 0;

            for (int i = 0; i < 12; i++)
                sum += (tempCnpj[i] - '0') * multiplier1[i]; 

            int remainder = (sum % 11);
            remainder = remainder < 2 ? 0 : 11 - remainder;

            string digit = remainder.ToString();
            tempCnpj = tempCnpj + digit;
            sum = 0;

            for (int i = 0; i < 13; i++)
                sum += (tempCnpj[i] - '0') * multiplier2[i]; 

            remainder = (sum % 11);
            remainder = remainder < 2 ? 0 : 11 - remainder;

            digit = digit + remainder.ToString();

            return cnpj.EndsWith(digit);
        }

        private bool IsRepeatedDigits(string input)
        {
            return input.All(c => c == input[0]);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
            yield return Type;
        }
    }
}