using InterviewDemo.Api.Data;
using InterviewDemo.Api.Dtos;
using InterviewDemo.Api.Intefaces;
using InterviewDemo.Api.Models;

namespace InterviewDemo.Api.Services
{
    public class TicketService : ITicketService
    {
        private readonly PosStore _postStore;
        public TicketService(PosStore posStore)
        {
            _postStore = posStore;
        }

        public async Task<(Ticket, string)> AddItemsAsync(string ticketId, List<ItemDto> items)
        {
            var ticket = _postStore.Tickets.Find(x => x.Id == ticketId);
            if (ticket == null)
            {
                return (null, "Không tìm thấy đơn hàng");
            }

            var ids = items.Select(x => x.ProductId).ToList();

            var products = _postStore.Products.Where(x => ids.Contains(x.Id)).ToList();
            if (!products.Any())
            {
                return (ticket, "Không tìm thấy sản phẩm");
            }

            foreach (var product in products)
            {
            }

            return (ticket, string.Empty);
        }

        public async Task<Ticket> CreateTicketAsync(CreateTicketDto ticketDto)
        {
            var ticket = new Ticket
            {
                CustomerName = ticketDto.CustomerName,
            };

            _postStore.Tickets.Add(ticket);

            return ticket;
        }

        public async Task<Ticket> GetById(string id)
        {
            var ticket = _postStore.Tickets.Find(x => x.Id == id);
            if (ticket == null)
            {
                return null;
            }

            var lines = _postStore.Lines.Where(x => x.TicketId == id).ToList();

            if (lines.Any())
            {
                ticket.Lines.AddRange(lines);
            }

            var payments = _postStore.Payments.Where(x => x.TicketId == id).ToList();

            if (payments.Any())
            {
                ticket.Payments.AddRange(payments);
            }

            return ticket;
        }

        public Task<Ticket> GetByIdAsync(string id)
        {
            throw new NotImplementedException();
        }
    }
}
