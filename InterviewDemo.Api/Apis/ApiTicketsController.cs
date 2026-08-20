using InterviewDemo.Api.Dtos;
using InterviewDemo.Api.Intefaces;
using InterviewDemo.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InterviewDemo.Api.Apis
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiTicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        public ApiTicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto ticket)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.CustomerName))
            {
                return BadRequest("Tên khách hàng k được để trống");
            }

            var res = await _ticketService.CreateTicketAsync(ticket);
            return Ok(res);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var ticket = _ticketService.GetByIdAsync(id);
            if (ticket == null)
            {
                return NotFound("Không tìm thấy ticket");
            }

            return Ok(ticket);
        }

        [HttpPost]
        [Route("{id}/items")]
        public async Task<IActionResult> AddItems(string id, [FromBody] List<ItemDto> items)
        {
            if (!items.Any())
            {
                return BadRequest("Items không được để trống");
            }

            var (ticket, error) = await _ticketService.AddItemsAsync(id, items);

            if (!string.IsNullOrEmpty(error))
            {
                return NotFound(error);
            }

            return Ok(ticket);
        }

    }
}
