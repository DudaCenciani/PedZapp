using PedZapp.Enums;
namespace PedZapp.ViewModels.Horario
{
    public class HorariosIndexViewModel { public string Slug { get; set; } = string.Empty; public bool AbertoAgora { get; set; } public IReadOnlyList<HorarioDiaViewModel> Dias { get; set; } = []; }
    public class HorarioDiaViewModel { public int Id { get; set; } public DiaSemana DiaSemana { get; set; } public bool Fechado { get; set; } public TimeOnly? Abertura1 { get; set; } public TimeOnly? Fechamento1 { get; set; } public TimeOnly? Abertura2 { get; set; } public TimeOnly? Fechamento2 { get; set; } }
}
