namespace PedZapp.ViewModels.PainelEmpresa
{
    public class DashboardEmpresaVM
    {
        public string NomeFantasia { get; set; }
            = string.Empty;

        public string Slug { get; set; }
            = string.Empty;

        public bool Ativa { get; set; }

        public bool PlanoAtivo { get; set; }

        // Resultado real da regra compartilhada de disponibilidade da loja.
        public bool LojaAberta { get; set; }

        public int TotalCategorias { get; set; }

        public int TotalProdutos { get; set; }

        public int TotalBairrosEntregaAtivos { get; set; }

        public int TotalFormasPagamentoAtivas { get; set; }

        public int TotalMesasOcupadas { get; set; }

        public int PedidosHoje { get; set; }

        public int PedidosEmAndamento { get; set; }

        public decimal VendasHoje { get; set; }

        public decimal VendasSemana { get; set; }

        public decimal VendasMes { get; set; }

        public decimal TicketMedio { get; set; }

        // Pendências calculadas pelo serviço após a validação do slug e do vínculo da empresa.
        public PendenciasEmpresaResultadoViewModel Pendencias { get; set; } = new();
    }
}
