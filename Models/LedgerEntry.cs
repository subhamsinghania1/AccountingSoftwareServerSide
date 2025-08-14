using System.ComponentModel.DataAnnotations;

namespace AccountingAPI.Models
{
    public class LedgerEntry
    {
        public int Id { get; set; }

        [Required]
        public int VendorId { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; set; }

        [Required]
        [RegularExpression("Credit|Debit")]
        public string Type { get; set; } = string.Empty; // "Credit" or "Debit"

        [Required]
        public DateTime Date { get; set; }

        public string Description { get; set; } = string.Empty;

        // Navigation property
        public Vendor? Vendor { get; set; }
    }
}