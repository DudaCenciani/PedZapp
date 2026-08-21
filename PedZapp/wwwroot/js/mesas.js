(() => {
    const fechamento = document.querySelector('[data-fechamento-form]');
    if (fechamento) {
        const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
        const subtotal = Number(fechamento.querySelector('[data-subtotal]').dataset.subtotal);
        const aplicar = fechamento.querySelector('[data-taxa-ativa]');
        const percentual = fechamento.querySelector('[data-percentual-taxa]');
        const formaPagamento = fechamento.querySelector('[data-forma-pagamento]');
        const blocoTroco = fechamento.querySelector('[data-troco-field]');
        const precisaTroco = fechamento.querySelector('[data-precisa-troco]');
        const valorTroco = fechamento.querySelector('[data-troco-valor]');
        const atualizarFechamento = () => {
            const taxa = aplicar.checked ? subtotal * Math.min(100, Math.max(0, Number(String(percentual.value).replace(',', '.')) || 0)) / 100 : 0;
            fechamento.querySelector('[data-taxa-valor]').textContent = moeda.format(taxa);
            fechamento.querySelector('[data-total-valor]').textContent = moeda.format(subtotal + taxa);
        };
        aplicar.addEventListener('change', atualizarFechamento);
        percentual.addEventListener('input', atualizarFechamento);
        const atualizarTroco = () => {
            const aceita = formaPagamento.selectedOptions[0]?.dataset.aceitaTroco === 'true';
            blocoTroco.hidden = !aceita;
            if (!aceita) { precisaTroco.checked = false; valorTroco.hidden = true; }
        };
        formaPagamento.addEventListener('change', atualizarTroco);
        precisaTroco.addEventListener('change', () => { valorTroco.hidden = !precisaTroco.checked; });
        atualizarTroco();
        atualizarFechamento();

        fechamento.addEventListener('submit', async evento => {
            evento.preventDefault();
            const botao = fechamento.querySelector('button[type="submit"]');
            if (botao.disabled) return;
            const textoOriginal = botao.textContent;
            botao.disabled = true;
            botao.textContent = 'Fechando conta...';
            try {
                const resposta = await fetch(fechamento.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }, body: new FormData(fechamento) });
                const corpo = await resposta.text();
                const resultado = corpo ? JSON.parse(corpo) : {};
                if (!resposta.ok || !resultado.success) throw new Error(resultado.message || 'Não foi possível fechar a conta.');
                if (resultado.printUrl) {
                    const janelaImpressao = window.open(resultado.printUrl, '_blank', 'noopener');
                    if (!janelaImpressao) { window.location.assign(resultado.printUrl); return; }
                }
                window.location.assign(resultado.redirectUrl);
            } catch (erro) {
                botao.disabled = false;
                botao.textContent = textoOriginal;
                window.alert(erro.message || 'Não foi possível fechar a conta.');
            }
        });
    }

    const root = document.querySelector('[data-comanda-catalogo]');
    if (!root) return;

    const produtos = JSON.parse(root.dataset.comandaCatalogo || '[]');
    const adicionais = JSON.parse(root.dataset.comandaAdicionais || '[]');
    const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
    const busca = root.querySelector('[data-produto-busca]');
    const filtroCategoria = root.querySelector('[data-categoria-filtro]');
    const resultados = root.querySelector('[data-produto-resultados]');
    const vazio = root.querySelector('[data-produto-vazio]');
    const modal = document.querySelector('[data-produto-modal]');
    let produtoSelecionado = null;

    const escapar = valor => String(valor ?? '').replace(/[&<>'"]/g, caractere => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[caractere]);
    const precoProduto = produto => Number(produto.Preco);

    const renderProdutos = () => {
        const termo = (busca.value || '').trim().toLocaleLowerCase('pt-BR');
        const categoria = filtroCategoria.value;
        const filtrados = produtos.filter(produto => (!categoria || String(produto.CategoriaId) === categoria) && (!termo || produto.Nome.toLocaleLowerCase('pt-BR').includes(termo)));
        resultados.innerHTML = filtrados.map(produto => `<article class="produto-card"><span>${escapar(produto.Categoria)}</span><h3>${escapar(produto.Nome)}</h3><strong>${moeda.format(precoProduto(produto))}</strong><button type="button" class="mesa-secondary" data-produto-id="${produto.Id}">Adicionar</button></article>`).join('');
        vazio.hidden = filtrados.length !== 0;
    };

    const atualizarTotal = () => {
        if (!produtoSelecionado) return;
        const quantidade = Math.max(1, Number(modal.querySelector('[data-modal-quantidade]').value) || 1);
        const extras = [...modal.querySelectorAll('input[name="AdicionalIds"]:checked')].reduce((total, campo) => total + Number(campo.dataset.preco), 0);
        modal.querySelector('[data-modal-total]').textContent = moeda.format((precoProduto(produtoSelecionado) + extras) * quantidade);
    };

    const renderAdicionais = () => {
        if (!produtoSelecionado) return;
        const termo = (modal.querySelector('[data-adicional-busca]').value || '').trim().toLocaleLowerCase('pt-BR');
        const validos = adicionais.filter(adicional => adicional.CategoriaId === produtoSelecionado.CategoriaId && (!termo || adicional.Nome.toLocaleLowerCase('pt-BR').includes(termo)));
        const destino = modal.querySelector('[data-adicional-resultados]');
        destino.innerHTML = validos.map(adicional => `<label class="adicional-option"><input type="checkbox" name="AdicionalIds" value="${adicional.Id}" data-preco="${adicional.Preco}" /><span>${escapar(adicional.Nome)}</span><strong>+ ${moeda.format(Number(adicional.Preco))}</strong></label>`).join('');
        modal.querySelector('[data-adicional-vazio]').hidden = validos.length !== 0;
        modal.querySelector('[data-adicional-contagem]').textContent = validos.length ? `${validos.length} disponível(is)` : '';
        atualizarTotal();
    };

    const abrirProduto = produto => {
        produtoSelecionado = produto;
        modal.querySelector('[data-modal-produto-id]').value = produto.Id;
        modal.querySelector('[data-modal-categoria]').textContent = produto.Categoria;
        modal.querySelector('[data-modal-nome]').textContent = produto.Nome;
        modal.querySelector('[data-modal-preco]').textContent = moeda.format(precoProduto(produto));
        modal.querySelector('[data-modal-quantidade]').value = 1;
        modal.querySelector('input[name="Observacao"]').value = '';
        modal.querySelector('[data-adicional-busca]').value = '';
        renderAdicionais();
        modal.showModal();
    };

    busca.addEventListener('input', renderProdutos);
    filtroCategoria.addEventListener('change', renderProdutos);
    resultados.addEventListener('click', evento => {
        const botao = evento.target.closest('[data-produto-id]');
        if (!botao) return;
        abrirProduto(produtos.find(produto => produto.Id === Number(botao.dataset.produtoId)));
    });
    modal.querySelector('[data-adicional-busca]').addEventListener('input', renderAdicionais);
    modal.addEventListener('change', evento => { if (evento.target.matches('[data-modal-quantidade], input[name="AdicionalIds"]')) atualizarTotal(); });
    modal.querySelector('[data-modal-quantidade]').addEventListener('input', atualizarTotal);

    document.querySelectorAll('[data-confirm]').forEach(form => form.addEventListener('submit', evento => {
        if (!window.confirm(form.dataset.confirm)) evento.preventDefault();
    }));

    const enviar = root.querySelector('[data-enviar-cozinha]');
    enviar?.addEventListener('submit', async evento => {
        evento.preventDefault();
        const botao = enviar.querySelector('button');
        // O lote é enviado para confirmação administrativa, não diretamente para a cozinha.
        if (botao.disabled || !window.confirm(`Enviar ${enviar.dataset.pendentes} item(ns) novo(s) para confirmação?`)) return;
        const textoOriginal = botao.textContent;
        botao.disabled = true;
        botao.textContent = 'Enviando...';
        try {
            const resposta = await fetch(enviar.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }, body: new FormData(enviar) });
            const corpo = await resposta.text();
            let resultado;
            try { resultado = corpo ? JSON.parse(corpo) : {}; }
            catch { throw new Error('A resposta do servidor não pôde ser processada. Atualize a página e tente novamente.'); }
            if (!resposta.ok || !resultado.success) throw new Error(resultado.message || resultado.erro || 'Não foi possível enviar os itens.');
            if (resultado.printUrl) {
                // A aba só é criada depois de o servidor confirmar o envio e fornecer uma URL válida.
                const janelaImpressao = window.open(resultado.printUrl, '_blank', 'noopener');
                if (!janelaImpressao) {
                    window.location.assign(resultado.printUrl);
                    return;
                }
            } else if (resultado.message) {
                window.alert(resultado.message);
            }
            window.location.assign(resultado.redirectUrl);
        } catch (erro) {
            botao.disabled = false;
            botao.textContent = textoOriginal;
            window.alert(erro.message || 'Não foi possível enviar os itens.');
        }
    });

    renderProdutos();
})();
