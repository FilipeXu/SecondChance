using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SecondChance.Models
{
    public class UserReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ReportedUserId { get; set; }

        [ForeignKey("ReportedUserId")]
        public User ReportedUser { get; set; }

        [Required]
        public string ReporterUserId { get; set; }

        [ForeignKey("ReporterUserId")]
        public User ReporterUser { get; set; }

        [Required]
        public string Reason { get; set; }

        public string Details { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.Now;

        public bool IsResolved { get; set; }

        [AllowNull]
        public string? Resolution { get; set; }

        public DateTime? ResolvedDate { get; set; }

        public string? ResolvedById { get; set; }

        [ForeignKey("ResolvedById")]
        public User? ResolvedBy { get; set; }
    }
}