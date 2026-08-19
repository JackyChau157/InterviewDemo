namespace InterviewDemo.Api.Models;

public sealed class TicketLine
{
    public string Id { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsStockTracked { get; set; }
    public LineStatus Status { get; set; } = LineStatus.Held;
    public DateTime? HoldExpiresAtUtc { get; set; }
}
