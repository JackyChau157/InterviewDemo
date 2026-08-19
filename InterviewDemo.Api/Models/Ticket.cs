namespace InterviewDemo.Api.Models;

public sealed class Ticket
{
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly BusinessDate { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? PromoCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<TicketLine> Lines { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}
