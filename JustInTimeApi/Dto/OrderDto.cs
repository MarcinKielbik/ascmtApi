namespace JustInTimeApi.Dto
{
    public class OrderDto
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public int PricePerUnit { get; set; }
        public string Currency { get; set; }
        public string PickupLocation { get; set; }
        public string Destination { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Status { get; set; }
        public int SupplierId { get; set; }
        public int UserId { get; set; }
    }
}
