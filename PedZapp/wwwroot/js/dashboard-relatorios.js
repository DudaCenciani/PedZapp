// Aguarda o DOM para localizar o container e os dados iniciais renderizados pelo servidor.
document.addEventListener("DOMContentLoaded", () => {
    // Interrompe silenciosamente em páginas que não são o dashboard de relatórios.
    const pagina = document.getElementById("dashboard-relatorios");
    if (!pagina) return;

    // Lê o JSON seguro produzido pelo Razor, sem fazer cálculos financeiros no navegador.
    const dadosIniciais = document.getElementById("dashboard-dados-iniciais");
    let dadosAtuais = dadosIniciais ? JSON.parse(dadosIniciais.textContent) : null;
    // Mantém uma referência por gráfico para destruí-lo antes de redesenhar.
    const graficos = {};
    // Padroniza valores monetários na cultura apresentada ao usuário.
    const moeda = valor => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(valor || 0);
    // Protege atualização da página caso a biblioteca de gráfico não tenha carregado.
    const podeDesenhar = () => typeof Chart !== "undefined";

    // Atualiza texto de um elemento sem procurar ou alterar outros componentes da tela.
    const texto = (id, valor) => { const elemento = document.getElementById(id); if (elemento) elemento.textContent = valor; };
    // Alterna o estado vazio de um gráfico preservando o canvas para a próxima atualização.
    const vazio = (id, ativo) => document.getElementById(id)?.classList.toggle("visible", ativo);

    // Destroi a instância anterior e cria uma nova somente quando Chart.js está disponível.
    const desenhar = (nome, canvasId, configuracao) => {
        if (!podeDesenhar()) return;
        if (graficos[nome]) graficos[nome].destroy();
        const canvas = document.getElementById(canvasId);
        if (canvas) graficos[nome] = new Chart(canvas, configuracao);
    };

    // Atualiza os cinco cards principais e os indicadores secundários a partir do retorno do servidor.
    const atualizarCards = dados => {
        const cards = dados.cards;
        texto("vendas-hoje", moeda(cards.vendasHoje)); texto("vendas-semana", moeda(cards.vendasSemana)); texto("vendas-mes", moeda(cards.vendasMes));
        texto("pedidos-hoje", cards.pedidosHoje); texto("ticket-medio", moeda(cards.ticketMedioHoje)); texto("pedidos-hoje-complemento", `${cards.pedidosHoje} pedidos concluídos`);
        texto("media-mes", `Média diária: ${moeda(dados.resumoMes.mediaDiaria)}`); texto("delivery-hoje", cards.deliveryHoje); texto("retirada-hoje", cards.retiradaHoje);
        texto("mesas-hoje", cards.mesasHoje); texto("manuais-hoje", cards.pedidosManuaisHoje); texto("taxas-entrega", moeda(cards.taxasEntregaHoje));
        texto("taxas-servico", moeda(cards.taxasServicoHoje)); texto("descontos-hoje", moeda(cards.descontosHoje)); texto("cancelamentos-hoje", cards.cancelamentosHoje);
        texto("ultima-atualizacao", `Atualizado às ${new Date(dados.atualizadoEm).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}`);
    };

    // Atualiza os resumos de semana, mês e a situação operacional sem realizar fechamento automático.
    const atualizarResumos = dados => {
        texto("semana-total", moeda(dados.resumoSemana.totalVendido)); texto("semana-pedidos", dados.resumoSemana.pedidos); texto("semana-melhor-dia", dados.resumoSemana.melhorDia); texto("semana-produto", dados.resumoSemana.produtoMaisVendido);
        texto("mes-total", moeda(dados.resumoMes.totalVendido)); texto("mes-pedidos", dados.resumoMes.pedidos); texto("mes-melhor-dia", dados.resumoMes.melhorDia); texto("mes-produto", dados.resumoMes.produtoMaisVendido);
        texto("status-dia", dados.statusFechamento.situacao); texto("pedidos-abertos", dados.statusFechamento.pedidosAbertos); texto("comandas-abertas", dados.statusFechamento.comandasAbertas); texto("mesas-ocupadas", dados.statusFechamento.mesasOcupadas);
    };

    // Desenha gráficos de série com tooltip que inclui valor, pedidos e ticket médio calculados no servidor.
    const graficoSerie = (nome, canvasId, pontos, cor, vazioId) => {
        const semDados = !pontos.some(ponto => ponto.valor > 0);
        vazio(vazioId, semDados);
        desenhar(nome, canvasId, { type: "line", data: { labels: pontos.map(ponto => ponto.rotulo), datasets: [{ label: "Faturamento", data: pontos.map(ponto => ponto.valor), borderColor: cor, backgroundColor: `${cor}22`, fill: true, tension: .35 }, { label: "Pedidos", data: pontos.map(ponto => ponto.quantidadePedidos), borderColor: "#c98d86", borderDash: [5, 5], tension: .35, yAxisID: "y1" }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: "bottom" }, tooltip: { callbacks: { afterBody: contexto => { const ponto = pontos[contexto[0].dataIndex]; return [`Pedidos: ${ponto.quantidadePedidos}`, `Ticket médio: ${moeda(ponto.ticketMedio)}`]; } } } }, scales: { y: { ticks: { callback: valor => moeda(valor) } }, y1: { position: "right", grid: { drawOnChartArea: false }, beginAtZero: true } } } });
    };

    // Desenha os gráficos de distribuição do dia com rótulos, valores e percentuais do servidor.
    const graficoRosca = (nome, canvasId, itens, vazioId) => {
        vazio(vazioId, itens.length === 0);
        desenhar(nome, canvasId, { type: "doughnut", data: { labels: itens.map(item => item.rotulo), datasets: [{ data: itens.map(item => item.valor), backgroundColor: ["#f6c445", "#c98d86", "#e9cfcb", "#8fba9d", "#d8c6a2", "#9b9190"] }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: "bottom" }, tooltip: { callbacks: { label: contexto => { const item = itens[contexto.dataIndex]; return `${item.rotulo}: ${moeda(item.valor)} · ${item.quantidadePedidos} pedidos · ${item.percentual}%`; } } } } } });
    };

    // Desenha o movimento por hora com duas séries sem preencher artificialmente horários sem pedido.
    const graficoHorarios = pontos => {
        vazio("vazio-horarios", pontos.length === 0);
        desenhar("horarios", "grafico-horarios", { type: "bar", data: { labels: pontos.map(ponto => ponto.rotulo), datasets: [{ label: "Pedidos", data: pontos.map(ponto => ponto.quantidadePedidos), backgroundColor: "#f6c445" }, { label: "Faturamento", data: pontos.map(ponto => ponto.valor), backgroundColor: "#c98d86", yAxisID: "y1" }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: "bottom" } }, scales: { y: { beginAtZero: true }, y1: { position: "right", grid: { drawOnChartArea: false }, ticks: { callback: valor => moeda(valor) } } } } });
    };

    // Preenche o ranking textual e a barra proporcional usando o período escolhido pelo usuário.
    const atualizarRanking = (dados, periodo) => {
        const itens = dados.produtosMaisVendidos[periodo] || [];
        const lista = document.getElementById("ranking-produtos");
        if (!lista) return;
        lista.innerHTML = "";
        const maiorQuantidade = Math.max(...itens.map(item => item.quantidade), 1);
        itens.forEach((item, indice) => { const linha = document.createElement("li"); linha.innerHTML = `<b>${indice + 1}</b><span>${item.nome}<small>${item.quantidade} un. · ${moeda(item.faturamento)}</small><i class="ranking-bar"><i style="width:${item.quantidade / maiorQuantidade * 100}%"></i></i></span><strong>${moeda(item.faturamento)}</strong>`; lista.appendChild(linha); });
        vazio("vazio-ranking", itens.length === 0);
    };

    // Atualiza todos os elementos visuais com o mesmo conjunto de dados recebido do endpoint protegido.
    const renderizar = dados => {
        if (!dados) return;
        dadosAtuais = dados;
        atualizarCards(dados); atualizarResumos(dados);
        graficoSerie("seteDias", "grafico-sete-dias", dados.graficos.ultimosSeteDias, "#f6c445", "vazio-sete-dias");
        graficoSerie("mes", "grafico-mes", dados.graficos.diasDoMes, "#c98d86", "vazio-mes");
        graficoRosca("pagamentos", "grafico-pagamentos", dados.graficos.formasPagamentoHoje, "vazio-pagamentos");
        graficoRosca("atendimentos", "grafico-atendimentos", dados.graficos.tiposAtendimentoHoje, "vazio-atendimentos");
        graficoHorarios(dados.graficos.horariosPicoHoje); atualizarRanking(dados, document.querySelector("[data-ranking].active")?.dataset.ranking || "hoje");
    };

    // Busca uma atualização leve; em caso de falha, preserva os dados já renderizados.
    const atualizar = async () => {
        const botao = document.getElementById("atualizar-dashboard");
        if (botao) { botao.disabled = true; botao.textContent = "Atualizando..."; }
        try { const resposta = await fetch(pagina.dataset.dashboardUrl, { headers: { "X-Requested-With": "XMLHttpRequest" }, credentials: "same-origin" }); if (!resposta.ok) throw new Error("Falha ao atualizar dashboard."); renderizar(await resposta.json()); }
        catch (erro) { console.warn("Não foi possível atualizar os dados do dashboard.", erro); }
        finally { if (botao) { botao.disabled = false; botao.textContent = "↻ Atualizar dados"; } }
    };

    // Troca a fonte do ranking sem emitir nova consulta nem recalcular valores no cliente.
    document.querySelectorAll("[data-ranking]").forEach(botao => botao.addEventListener("click", () => { document.querySelectorAll("[data-ranking]").forEach(item => item.classList.remove("active")); botao.classList.add("active"); atualizarRanking(dadosAtuais, botao.dataset.ranking); }));
    // Permite atualização manual discreta pelo botão do cabeçalho.
    document.getElementById("atualizar-dashboard")?.addEventListener("click", atualizar);
    // Renderiza o servidor inicialmente e atualiza no máximo uma vez por minuto.
    renderizar(dadosAtuais);
    window.setInterval(atualizar, 60000);
});
