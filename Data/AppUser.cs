using System.ComponentModel.DataAnnotations;

namespace BlutdruckErfassungApp.Data;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(120)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string UserNameNormalized { get; set; } = string.Empty;

    [Required]
    [StringLength(400)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
