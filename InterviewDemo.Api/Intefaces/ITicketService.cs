using InterviewDemo.Api.Dtos;
using InterviewDemo.Api.Models;

namespace InterviewDemo.Api.Intefaces
{
    public interface ITicketService
    {
        Task<Ticket> CreateTicketAsync(CreateTicketDto ticketDto);

        Task<Ticket> GetByIdAsync(string id);

        Task<(Ticket, string)> AddItemsAsync(string ticketId, List<ItemDto> items);
    }
}
