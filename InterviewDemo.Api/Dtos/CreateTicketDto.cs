using System.ComponentModel.DataAnnotations;

namespace InterviewDemo.Api.Dtos
{
    public class CreateTicketDto
    {
        [Required]
        public string CustomerName { get; set; }
    }
}
