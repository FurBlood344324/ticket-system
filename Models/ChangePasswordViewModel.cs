using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifreyi tekrar girin.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre (Tekrar)")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
