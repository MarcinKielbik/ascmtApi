using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.ConstrainedExecution;

namespace JustInTimeApi.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public string ProductName { get; set; }       
        public int Quantity { get; set; }
        public int PricePerUnit { get; set; }
        public string Currency { get; set; }
        public string PickupLocation { get; set; }
        public string Destination { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Status { get; set; } // Wysłano, w drodze, Na miejscu
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
