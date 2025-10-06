using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }


        [Display(Name = "Order ID")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Placed (UTC)")]
        public DateTimeOffset? OrderDateUtc { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price"), DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Required]
        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public double TotalPrice { get; set; }

        [Required, Display(Name = "Status")]
        public OrderStatus Status { get; set; } = OrderStatus.Submitted;
    }

    public enum OrderStatus
    {
        Submitted,
        Processing,
        Completed,
        Cancelled
    }
}


 

