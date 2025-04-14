using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SecondChance.Models
{
    /// <summary>
    /// Representa uma denúncia feita contra um utilizador na plataforma.
    /// Utilizado para registar e processar queixas de comportamento inadequado.
    /// </summary>
    public class UserReport
    {
        /// <summary>
        /// Identificador único da denúncia
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identificador do utilizador que foi denunciado
        /// </summary>
        [Required]
        public string ReportedUserId { get; set; }

        /// <summary>
        /// Utilizador que foi denunciado
        /// </summary>
        [ForeignKey("ReportedUserId")]
        public User ReportedUser { get; set; }

        /// <summary>
        /// Identificador do utilizador que submeteu a denúncia
        /// </summary>
        [Required]
        public string ReporterUserId { get; set; }

        /// <summary>
        /// Utilizador que submeteu a denúncia
        /// </summary>
        [ForeignKey("ReporterUserId")]
        public User ReporterUser { get; set; }

        /// <summary>
        /// Motivo da denúncia
        /// </summary>
        [Required]
        public string Reason { get; set; }

        /// <summary>
        /// Detalhes adicionais sobre a denúncia
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Data e hora em que a denúncia foi submetida
        /// </summary>
        public DateTime ReportDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica se a denúncia já foi resolvida
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Descrição da resolução aplicada pelo moderador
        /// </summary>
        [AllowNull]
        public string? Resolution { get; set; }

        /// <summary>
        /// Data e hora em que a denúncia foi resolvida
        /// </summary>
        public DateTime? ResolvedDate { get; set; }

        /// <summary>
        /// Identificador do moderador que resolveu a denúncia
        /// </summary>
        public string? ResolvedById { get; set; }

        /// <summary>
        /// Moderador que resolveu a denúncia
        /// </summary>
        [ForeignKey("ResolvedById")]
        public User? ResolvedBy { get; set; }
    }
}