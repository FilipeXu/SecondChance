using System.ComponentModel.DataAnnotations;

namespace SecondChance.ViewModels
{
    public class ReportUserViewModel
    {
        [Required]
        public string ReportedUserId { get; set; }
        
        public string ReportedUserName { get; set; }
        
        [Required(ErrorMessage = "É necessário informar o motivo do relatório.")]
        [Display(Name = "Motivo do Relatório")]
        public string Reason { get; set; }
        
        [Display(Name = "Detalhes Adicionais")]
        public string Details { get; set; }
    }
} 