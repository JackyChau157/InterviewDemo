namespace InterviewDemo.Api.Models;

public sealed class Payment
{
    public string Id { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public DateTime PaidAtUtc { get; set; }
}
