using System.ComponentModel.DataAnnotations;
using PedZapp.Enums;

namespace PedZapp.ViewModels.Mesa
{
    public class MesasIndexViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public int TotalMesas { get; set; }
        public int TotalLivres { get; set; }
        public int TotalOcupadas { get; set; }
        public MesaFormViewModel NovaMesa { get; set; } = new();
        public IReadOnlyList<MesaCardViewModel> Mesas { get; set; } = [];
    }
    public class MesaFormViewModel
    {
        [Required, StringLength(80)] public string Nome { get; set; } = string.Empty;
        public int? Numero { get; set; }
        [Range(1, 999)] public int? Capacidade { get; set; }
        public int OrdemExibicao { get; set; }
        public bool Ativa { get; set; } = true;
        [StringLength(300)] public string? Observacao { get; set; }
    }
    public class MesaCardViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int? Numero { get; set; }
        public int? Capacidade { get; set; }
        public StatusMesa Status { get; set; }
        public bool Ativa { get; set; }
        public int? ComandaId { get; set; }
        public DateTime? DataAbertura { get; set; }
        public string? Funcionario { get; set; }
        public decimal TotalAtual { get; set; }
        public int QuantidadeItens { get; set; }
    }
}
