using InterviewDemo.Api.Models;

namespace InterviewDemo.Api.Data;

public sealed class PosStore
{
    public List<Product> Products { get; } = [];
    public List<Ticket> Tickets { get; } = [];
    public List<TicketLine> Lines { get; } = [];
    public List<Payment> Payments { get; } = [];

    public PosStore()
    {
        Seed();
    }

    private void Seed()
    {
        var day1 = new DateOnly(2026, 8, 1);
        var now = DateTime.UtcNow;

        Products.AddRange(
        [
            new Product { Id = "p-haircut", Name = "Haircut", Price = 60, IsStockTracked = false },
            new Product { Id = "p-color", Name = "Color", Price = 40, IsStockTracked = false },
            new Product { Id = "p-shampoo", Name = "Shampoo", Price = 15, IsStockTracked = true, StockOnHand = 5, HeldQty = 0 },
            new Product { Id = "p-gel", Name = "Gel", Price = 12, IsStockTracked = true, StockOnHand = 2, HeldQty = 2 }
        ]);

        Tickets.AddRange(
        [
            new Ticket { Id = "t1", CustomerName = "Alice", BusinessDate = day1, Status = TicketStatus.Paid, Subtotal = 75, Total = 75, CreatedAtUtc = now.AddHours(-3) },
            new Ticket { Id = "t2", CustomerName = "Bob", BusinessDate = day1, Status = TicketStatus.Paid, Subtotal = 40, Total = 40, CreatedAtUtc = now.AddHours(-2) },
            new Ticket { Id = "t3", CustomerName = "Dana", BusinessDate = day1, Status = TicketStatus.Open, CreatedAtUtc = now.AddMinutes(-10) },
            new Ticket { Id = "t4", CustomerName = "Eve", BusinessDate = day1, Status = TicketStatus.Open, CreatedAtUtc = now.AddMinutes(-5) },
            new Ticket { Id = "t5", CustomerName = "Frank", BusinessDate = day1, Status = TicketStatus.Void, CreatedAtUtc = now.AddHours(-1) }
        ]);

        Lines.AddRange(
        [
            new TicketLine { Id = "l1a", TicketId = "t1", ProductId = "p-haircut", ProductName = "Haircut", Qty = 1, UnitPrice = 60, LineTotal = 60, Status = LineStatus.Sold },
            new TicketLine { Id = "l1b", TicketId = "t1", ProductId = "p-shampoo", ProductName = "Shampoo", Qty = 1, UnitPrice = 15, LineTotal = 15, IsStockTracked = true, Status = LineStatus.Sold },
            new TicketLine { Id = "l2", TicketId = "t2", ProductId = "p-color", ProductName = "Color", Qty = 1, UnitPrice = 40, LineTotal = 40, Status = LineStatus.Sold },
            new TicketLine { Id = "l3", TicketId = "t3", ProductId = "p-haircut", ProductName = "Haircut", Qty = 1, UnitPrice = 60, LineTotal = 60, Status = LineStatus.Held },
            new TicketLine { Id = "l4", TicketId = "t4", ProductId = "p-gel", ProductName = "Gel", Qty = 2, UnitPrice = 12, LineTotal = 24, IsStockTracked = true, Status = LineStatus.Held, HoldExpiresAtUtc = now.AddMinutes(30) },
            new TicketLine { Id = "l5", TicketId = "t5", ProductId = "p-color", ProductName = "Color", Qty = 1, UnitPrice = 40, LineTotal = 40, Status = LineStatus.Released }
        ]);

        Payments.AddRange(
        [
            new Payment { Id = "pay1", TicketId = "t1", Amount = 75, Method = "Cash", PaidAtUtc = now.AddHours(-3) },
            new Payment { Id = "pay2", TicketId = "t2", Amount = 40, Method = "Card", PaidAtUtc = now.AddHours(-2) }
        ]);
    }
}
