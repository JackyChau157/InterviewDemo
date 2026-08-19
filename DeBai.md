# Mini POS Ticket — 60 phút

Viết ASP.NET Core Web API, chạy local, không cần database / internet.

```powershell
dotnet run --project InterviewDemo.Api --launch-profile http
```

Swagger: `http://localhost:5177/swagger`

Có sẵn: model + dữ liệu mẫu trong `PosStore` (singleton). Bạn tự viết controller, service và toàn bộ logic. Dùng `async`/`await`, không `.Result` / `.Wait()`.

---

## 1. Nghiệp vụ (đọc trước khi code)

Đây là **quầy POS salon**: khách mở bill, nhân viên thêm dịch vụ / sản phẩm, rồi thu tiền.

Một **ticket** = một bill.

| Trạng thái bill | Ý nghĩa |
|-----------------|--------|
| `Open` | Đang phục vụ, chưa thu tiền |
| `Paid` | Đã thanh toán, xong |
| `Void` | Hủy bill, không tính doanh thu |

Mỗi món trên bill là một **line** (gắn `TicketId`).

| Trạng thái món | Ý nghĩa |
|----------------|--------|
| `Held` | Đã ghi vào bill. Nếu là hàng bán lẻ thì **đang giữ tồn** — khách khác không lấy nốt số đó |
| `Sold` | Đã thu tiền. Hàng bán lẻ thì trừ kho thật |
| `Released` | Bỏ món / hủy bill — trả lại tồn nếu đang giữ |

Hai loại hàng:

- **Dịch vụ** (`IsStockTracked = false`): Haircut, Color. Không có kho. Thêm vào bill chỉ tạo line `Held`.
- **Bán lẻ** (`IsStockTracked = true`): Shampoo, Gel. Có kho:

```
Còn bán = StockOnHand - HeldQty
```

Ví dụ Gel trong seed: tồn 2, đang giữ 2 → còn bán **0**. Thêm Gel nữa phải báo hết hàng.

Luồng một bill mới:

1. Mở bill → ticket `Open`.
2. Thêm món. Bán lẻ: còn hàng thì `HeldQty` tăng, line `Held`. Hết hàng thì không thêm được.
3. Checkout: cộng tiền các line `Held`, trừ mã giảm giá, tạo payment, line đổi `Sold`. Bán lẻ: bỏ giữ **và** trừ `StockOnHand`. Ticket đổi `Paid`.

Mã giảm giá (nếu có): `SAVE10` giảm 10% trên tạm tính, `FLAT20` giảm 20. Tổng thanh toán không được âm. Không gửi mã thì không giảm.

Dữ liệu tách **4 list** trong `PosStore`, coi như 4 bảng SQL: `Products`, `Tickets`, `Lines`, `Payments`.

- **Product đã seed sẵn** — chỉ đọc khi thêm món (`p-haircut`, `p-color`, `p-shampoo`, `p-gel`). Không viết API product.
- Ticket **không** chứa sẵn lines/payments — phải ghép khi trả API.

---

## 2. Dữ liệu mẫu (ngày `2026-08-01`)

| Product | Giá | Kho |
|---------|-----|-----|
| `p-haircut` Haircut | 60 | Dịch vụ, không tồn |
| `p-color` Color | 40 | Dịch vụ, không tồn |
| `p-shampoo` Shampoo | 15 | Tồn 5, đang giữ 0 → còn bán 5 |
| `p-gel` Gel | 12 | Tồn 2, đang giữ 2 → còn bán 0 |

| Ticket | Khách | Bill | Trên bill |
|--------|--------|------|-----------|
| `t1` | Alice | Paid | Haircut 60 + Shampoo 15 = 75, đã thu Cash 75 |
| `t2` | Bob | Paid | Color 40, đã thu Card 40 |
| `t3` | Dana | **Open** | Haircut 60, chưa thu — dùng để checkout |
| `t4` | Eve | Open | Gel x2 đang giữ hết kho |
| `t5` | Frank | Void | Color đã bỏ, không tính tiền |

---

## 3. Việc cần làm

Chỉ viết **API Ticket**. Product lấy từ `PosStore.Products` (seed), không tạo endpoint product.

Tự thiết kế DTO nếu cần. HTTP status hợp lý: không thấy → 404, sai trạng thái / hết hàng → 409, dữ liệu / promo sai → 400.

- `POST /api/tickets` — tạo bill `Open`
- `GET /api/tickets/{id}` — **kèm** lines và payments
- `POST /api/tickets/{id}/items` — body gồm `productId`, `qty`. Chỉ bill `Open`. Tra product trong seed. Bán lẻ hết hàng → 409. Dịch vụ không đụng kho.
- `POST /api/tickets/{id}/checkout` — chỉ bill `Open`. Body có thể có `promoCode`. Tính tiền như mục 1, line `Held` → `Sold`, tạo payment, bill `Paid`.
- `GET /api/tickets?date=2026-08-01` — mọi bill ngày đó, **kèm** lines và payments