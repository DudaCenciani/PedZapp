using PedZapp.Enums;
namespace PedZapp.Models
{
    public class HorarioFuncionamento
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public DiaSemana DiaSemana { get; set; }
        public bool Fechado { get; set; } = true;
        public TimeOnly? Abertura1 { get; set; }
        public TimeOnly? Fechamento1 { get; set; }
        public TimeOnly? Abertura2 { get; set; }
        public TimeOnly? Fechamento2 { get; set; }
        public bool Ativo { get; set; } = true;
        public int OrdemExibicao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataAtualizacao { get; set; }
    }
}
