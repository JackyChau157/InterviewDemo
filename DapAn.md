# Đáp án / sườn chấm (KHÔNG đưa ứng viên)

File này để bạn đối chiếu code ứng viên. Đề giao ứng viên chỉ là `DeBai.md`.

---

## Nghiệp vụ (hình dung 1 quầy POS)

Một **ticket** = một bill của khách.

| Status ticket | Nghĩa |
|---------------|--------|
| `Open` | Đang phục vụ, chưa thu tiền |
| `Paid` | Đã thanh toán, xong |
| `Void` | Hủy bill |

Mỗi món trên bill là một **line** (bảng `Lines`, gắn `TicketId`).

| Status line | Nghĩa |
|-------------|--------|
| `Held` | Đã ghi vào bill, nếu là hàng bán lẻ thì **đang giữ tồn** (người khác không bán nốt) |
| `Sold` | Đã thu tiền, trừ kho thật |
| `Released` | Bỏ món / void — trả tồn nếu có |

Hai loại product:

- **Service** (`IsStockTracked = false`): Haircut, Color — cắt tóc không có tồn kho.
- **Retail** (`IsStockTracked = true`): Shampoo, Gel — có `StockOnHand` (tồn thật) và `HeldQty` (đang giữ trên bill chưa trả tiền).

```
Available = StockOnHand - HeldQty
```

Gel seed: `StockOnHand = 2`, `HeldQty = 2` → **Available = 0** vì `t4` đang hold 2 tuýp.

---

## Seed đang có (ngày 2026-08-01)

```
Products
  p-haircut  60   service
  p-color    40   service
  p-shampoo  15   tồn 5, hold 0  → còn bán 5
  p-gel      12   tồn 2, hold 2  → còn bán 0

Tickets / Lines / Payments
  t1 Alice  Paid   Haircut 60 + Shampoo 15 = 75   có payment Cash 75
  t2 Bob    Paid   Color 40                    có payment Card 40
  t3 Dana   Open   Haircut 60 (Held)           CHƯA thanh toán  ← checkout mẫu
  t4 Eve    Open   Gel x2 (Held)               đang giữ hết Gel
  t5 Frank  Void   Color (Released)
```

Luồng đúng của 1 bill mới:

1. `POST /tickets` → bill `Open`, chưa có line.
2. `POST /tickets/{id}/items` → thêm món. Service: chỉ tạo line `Held`. Retail: nếu `Available >= qty` thì `HeldQty += qty`, line `Held`; không đủ → **409**.
3. `POST /checkout` → chỉ bill `Open`. Cộng tiền các line `Held`, trừ promo, tạo `Payment`, line → `Sold`. Retail: `HeldQty -= qty` **và** `StockOnHand -= qty`. Ticket → `Paid`.
4. `GET /tickets?date=` → lấy header theo ngày, **join** lines + payments (không N+1).

---

## Case chạy tay khi chấm

**Checkout `t3` + SAVE10**

- Line Held: Haircut 60 (service, không đụng kho)
- Subtotal 60, discount 6, total **54**
- Ticket `Paid`, 1 payment 54, line `Sold`

**Thêm Gel vào `t3` (hoặc bill Open khác)** → **409** (t4 đang hold hết).

**Thêm Shampoo qty 1 vào `t3`** → Shampoo `HeldQty = 1`, Available = 4. Checkout không promo: 60+15=**75**, Shampoo `Sold`, `StockOnHand = 4`, `HeldQty = 0`.

**GET list `2026-08-01`:** 5 ticket. `t1` có 2 lines + 1 payment; `t3` có 1 line, 0 payment (nếu chưa checkout).

N+1 **sai:** `foreach (tickets) { lines = Lines.Where(l => l.TicketId == t.Id) }` — với in-memory vẫn “chạy”, nhưng tư duy giống query từng id.  
N+1 **đúng:** một lần lấy toàn bộ lines/payments của các id, rồi `GroupBy(TicketId)`.

---

## Sườn code tham khảo

Ứng viên không cần giống 100%. Cần đúng luồng, `async`, join batch.

### 1. Inject store

```csharp
public sealed class TicketService(PosStore store)
{
}
```

### 2. Tạo bill

```csharp
public Ticket Create(string customerName)
{
    var ticket = new Ticket
    {
        Id = Guid.NewGuid().ToString("N")[..8],
        CustomerName = customerName,
        BusinessDate = new DateOnly(2026, 8, 1),
        Status = TicketStatus.Open,
        CreatedAtUtc = DateTime.UtcNow
    };
    store.Tickets.Add(ticket);
    return ticket;
}
```

### 3. Thêm món + giữ tồn (điểm nghiệp vụ)

```csharp
public Ticket AddItem(string ticketId, string productId, int qty)
{
    var ticket = store.Tickets.FirstOrDefault(t => t.Id == ticketId)
        ?? throw new InvalidOperationException("404 ticket");
    if (ticket.Status != TicketStatus.Open)
        throw new InvalidOperationException("409 chỉ Open mới thêm món");

    var product = store.Products.FirstOrDefault(p => p.Id == productId && !p.IsDeleted)
        ?? throw new InvalidOperationException("404 product");

    // Retail: giữ tồn. Service: bỏ qua kho.
    if (product.IsStockTracked)
    {
        if (product.AvailableQty < qty)
            throw new InvalidOperationException("409 hết hàng");
        product.HeldQty += qty;
    }

    store.Lines.Add(new TicketLine
    {
        Id = Guid.NewGuid().ToString("N")[..8],
        TicketId = ticket.Id,
        ProductId = product.Id,
        ProductName = product.Name,
        Qty = qty,
        UnitPrice = product.Price,
        LineTotal = product.Price * qty,
        IsStockTracked = product.IsStockTracked,
        Status = LineStatus.Held,
        HoldExpiresAtUtc = product.IsStockTracked ? DateTime.UtcNow.AddMinutes(2) : null
    });

    return Attach(ticket); // gán Lines + Payments vào ticket để trả API
}
```

Ví dụ số: thêm `p-gel` qty 1 khi seed `Available = 0` → vào nhánh 409, **không** tạo line.

### 4. Checkout + promo (điểm nghiệp vụ)

```csharp
public Ticket Checkout(string ticketId, string? promoCode)
{
    var ticket = store.Tickets.FirstOrDefault(t => t.Id == ticketId)
        ?? throw new InvalidOperationException("404");
    if (ticket.Status != TicketStatus.Open)
        throw new InvalidOperationException("409");

    var held = store.Lines
        .Where(l => l.TicketId == ticketId && l.Status == LineStatus.Held)
        .ToList();
    if (held.Count == 0)
        throw new InvalidOperationException("409 không có món");

    var subtotal = held.Sum(l => l.LineTotal);
    var discount = promoCode?.ToUpperInvariant() switch
    {
        null or "" => 0m,
        "SAVE10" => subtotal * 0.10m,
        "FLAT20" => 20m,
        _ => throw new InvalidOperationException("400 promo")
    };
    var total = Math.Max(0, subtotal - discount);

    foreach (var line in held)
    {
        line.Status = LineStatus.Sold;
        if (line.IsStockTracked)
        {
            var product = store.Products.First(p => p.Id == line.ProductId);
            product.HeldQty -= line.Qty;       // bỏ giữ
            product.StockOnHand -= line.Qty;   // trừ kho thật
        }
    }

    ticket.Subtotal = subtotal;
    ticket.Discount = discount;
    ticket.Total = total;
    ticket.PromoCode = promoCode;
    ticket.Status = TicketStatus.Paid;

    store.Payments.Add(new Payment
    {
        Id = Guid.NewGuid().ToString("N")[..8],
        TicketId = ticket.Id,
        Amount = total,
        Method = "Cash",
        PaidAtUtc = DateTime.UtcNow
    });

    return Attach(ticket);
}
```

`t3` + `SAVE10`: subtotal 60 → discount 6 → total 54. Haircut không trừ kho.

`t3` thêm Shampoo rồi checkout không mã: 75; Shampoo `StockOnHand` 5→4, `HeldQty` 1→0.

### 5. GET theo ngày — tránh N+1 (điểm tư duy)

```csharp
public List<Ticket> ListByDate(DateOnly date)
{
    var tickets = store.Tickets.Where(t => t.BusinessDate == date).ToList();
    var ids = tickets.Select(t => t.Id).ToHashSet();

    // 1 lần — không foreach ticket rồi Where theo từng Id
    var lines = store.Lines.Where(l => ids.Contains(l.TicketId)).ToList();
    var payments = store.Payments.Where(p => ids.Contains(p.TicketId)).ToList();

    var linesByTicket = lines.GroupBy(l => l.TicketId).ToDictionary(g => g.Key, g => g.ToList());
    var paymentsByTicket = payments.GroupBy(p => p.TicketId).ToDictionary(g => g.Key, g => g.ToList());

    foreach (var ticket in tickets)
    {
        ticket.Lines = linesByTicket.GetValueOrDefault(ticket.Id) ?? [];
        ticket.Payments = paymentsByTicket.GetValueOrDefault(ticket.Id) ?? [];
    }
    return tickets;
}
```

Sai (vẫn ra kết quả, trừ điểm):

```csharp
foreach (var ticket in tickets)
{
    ticket.Lines = store.Lines.Where(l => l.TicketId == ticket.Id).ToList();
    ticket.Payments = store.Payments.Where(p => p.TicketId == ticket.Id).ToList();
}
```

### 6. GET 1 ticket + async (mẫu)

```csharp
public async Task<Ticket> GetByIdAsync(string id, CancellationToken ct)
{
    var ticket = store.Tickets.FirstOrDefault(t => t.Id == id)
        ?? throw new InvalidOperationException("404");

    var linesTask = Task.Run(() => store.Lines.Where(l => l.TicketId == id).ToList(), ct);
    var paymentsTask = Task.Run(() => store.Payments.Where(p => p.TicketId == id).ToList(), ct);
    await Task.WhenAll(linesTask, paymentsTask); // in-memory thì WhenAll mang tính “có ý async”

    ticket.Lines = await linesTask;
    ticket.Payments = await paymentsTask;
    return ticket;
}
```

In-memory không bắt buộc `Task.Run`. Quan trọng: controller/service là `async Task`, không `.Result`. Product chỉ đọc từ seed, không có API product.

---

## Rubric nhanh

| Ứng viên làm | Kết luận |
|--------------|----------|
| API ticket chạy, checkout không trừ kho / không đổi `Paid` | Chưa đạt nghiệp vụ |
| Thêm Gel vẫn 200 | Trượt tồn kho |
| Checkout `t3` + SAVE10 = 54, Gel 409 | Đạt nghiệp vụ |
| List date đúng data nhưng `foreach` từng ticket | Junior; hỏi lại N+1 |
| List batch `GroupBy` + giải thích 3 bảng | Middle |
| `.Result` / logic nhồi controller 200 dòng | Trừ điểm cấu trúc |
