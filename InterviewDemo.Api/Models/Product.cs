namespace InterviewDemo.Api.Models;

public sealed class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsStockTracked { get; set; }
    public int StockOnHand { get; set; }
    public int HeldQty { get; set; }
    public bool IsDeleted { get; set; }

    public int AvailableQty => Math.Max(0, StockOnHand - HeldQty);
}
