using System.ComponentModel.DataAnnotations;

namespace SecondChance.ViewModels
{
    /// <summary>
    /// Modelo de vista para gestão de denúncias de utilizadores.
    /// Utilizado para capturar informações sobre denúncias de comportamento inadequado.
    /// </summary>
    public class ReportUserViewModel
    {
        /// <summary>
        /// Identificador do utilizador denunciado
        /// </summary>
        [Required]
        public string ReportedUserId { get; set; }
        
        /// <summary>
        /// Nome do utilizador denunciado
        /// </summary>
        public string ReportedUserName { get; set; }
        
        /// <summary>
        /// Motivo da denúncia
        /// </summary>
        [Required(ErrorMessage = "É necessário informar o motivo do relatório.")]
        [Display(Name = "Motivo do Relatório")]
        public string Reason { get; set; }

        /// <summary>
        /// Detalhes adicionais sobre a denúncia
        /// </summary>
        [Required(ErrorMessage = "É necessário detalhar o relatório.")]
        [Display(Name = "Detalhes Adicionais")]
        public string Details { get; set; }
    }
}