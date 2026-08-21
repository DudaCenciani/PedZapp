namespace PedZapp.Hubs
{
    /// <summary>
    /// Centraliza o nome interno dos grupos SignalR de pedidos.
    /// O navegador nunca informa esse nome: ele é montado no servidor a partir do EmpresaId validado.
    /// </summary>
    public static class PedidosHubGroups
    {
        /// <summary>
        /// Retorna o grupo privado usado para distribuir avisos somente aos usuários da empresa informada.
        /// </summary>
        public static string DaEmpresa(int empresaId) => $"empresa:{empresaId}";
    }
}
