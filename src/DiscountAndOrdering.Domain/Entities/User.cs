using DiscountAndOrdering.Domain.Enums;

namespace DiscountAndOrdering.Domain.Entities;

public sealed class User
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public UserTier Tier { get; private set; }

    public User(Guid id, string name, string email, UserTier tier)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Id = id;
        Name = name;
        Email = email;
        Tier = tier;
    }

    public void UpdateDetails(string name, string email, UserTier tier)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Name = name;
        Email = email;
        Tier = tier;
    }
}
