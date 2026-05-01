using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class RegisterRequest
{
    [Required(ErrorMessage = "Imię i nazwisko jest wymagane.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Imię i nazwisko musi mieć od 3 do 200 znaków.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Hasło musi mieć co najmniej 8 znaków, co najmniej jedną wielką literę, co najmniej jedną małą literę i co najmniej jedną cyfrę.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Powtórzenie hasła jest wymagane.")]
    [Compare(nameof(Password), ErrorMessage = "Hasła muszą być identyczne.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
