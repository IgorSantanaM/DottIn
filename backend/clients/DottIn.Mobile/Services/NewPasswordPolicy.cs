namespace DottIn.Mobile.Services;

public static class NewPasswordPolicy
{
    public static bool IsValid(string currentPassword, string newPassword, string confirmation)
        => !string.IsNullOrWhiteSpace(currentPassword)
           && !string.IsNullOrWhiteSpace(newPassword)
           && newPassword.Length >= 10
           && newPassword.Any(char.IsUpper)
           && newPassword.Any(char.IsLower)
           && newPassword.Any(char.IsDigit)
           && newPassword.Any(ch => !char.IsLetterOrDigit(ch))
           && newPassword != currentPassword
           && newPassword == confirmation;
}
