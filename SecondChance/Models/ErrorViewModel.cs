namespace SecondChance.Models
{
    /// <summary>
    /// Modelo de vista para apresentação de erros na aplicação.
    /// Utilizado para mostrar informações de diagnóstico quando ocorrem erros.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Identificador único do pedido que resultou em erro
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Indica se o identificador do pedido deve ser mostrado
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
