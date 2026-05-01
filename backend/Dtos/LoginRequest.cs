using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class LoginRequest
{
    [Required(ErrorMessage = "E-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane.")]
    public string Password { get; set; } = string.Empty;
}
