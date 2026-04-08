namespace DottIn.Application.Exceptions
{
    public class SubscriptionLimitExceededException : Exception
    {
        public string LimitType { get; }
        public int CurrentCount { get; }
        public int MaxAllowed { get; }

        public SubscriptionLimitExceededException(string limitType, int currentCount, int maxAllowed)
            : base($"Limite de {limitType} excedido. Atual: {currentCount}, Máximo permitido: {maxAllowed}.")
        {
            LimitType = limitType;
            CurrentCount = currentCount;
            MaxAllowed = maxAllowed;
        }

        public SubscriptionLimitExceededException(string message) : base(message)
        {
            LimitType = "Unknown";
            CurrentCount = 0;
            MaxAllowed = 0;
        }

        public static SubscriptionLimitExceededException ForEmployees(int currentCount, int maxAllowed)
            => new("funcionários", currentCount, maxAllowed);

        public static SubscriptionLimitExceededException ForBranches(int currentCount, int maxAllowed)
            => new("filiais", currentCount, maxAllowed);
    }
}
