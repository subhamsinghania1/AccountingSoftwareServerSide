using System.ComponentModel.DataAnnotations;

namespace AccountingAPI.Models
{
    public class LedgerEntry
    {
        public int Id { get; set; }

        [Required]
        public int VendorId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Type { get; set; } = string.Empty; // "Credit" or "Debit"

        [Required]
        public DateTime Date { get; set; }

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        // Navigation property
        public Vendor? Vendor { get; set; }
    }
}