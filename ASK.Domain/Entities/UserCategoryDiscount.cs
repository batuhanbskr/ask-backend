namespace ASK.Domain.Entities;

public class UserCategoryDiscount : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal DiscountRate { get; set; }
}
