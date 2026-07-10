using UserService.Domain.Enums;

namespace UserService.Domain.Entities;
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserRole Role { get; set; } = UserRole.Customer;
}