using System.ComponentModel.DataAnnotations;

namespace JustInTimeApi.Dto
{
    public class SupplierDto
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string CompanyName { get; set; }
        public string PhoneNumber { get; set; }

    }
}
