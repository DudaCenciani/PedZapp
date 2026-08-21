(() => {
    const root = document.querySelector('[data-checkout-root]');
    if (!root) return;

    const slug = root.dataset.slug;
    const cartKey = `pedzapp-carrinho-${slug}`;
    const clientKey = `pedzapp-cliente-${slug}`;
    const checkoutKey = `pedzapp-checkout-${slug}`;
    const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
    const maxQuantity = 99;
    const form = document.querySelector('[data-checkout-form]');
    const error = document.querySelector('[data-checkout-error]');
    const steps = [...document.querySelectorAll('[data-checkout-step]')];
    const bairroPicker = document.querySelector('[data-bairro-picker]');
    const bairroToggle = document.querySelector('[data-bairro-toggle]');
    const bairroList = document.querySelector('[data-bairro-list]');
    const bairroLabel = document.querySelector('[data-bairro-label]');
    let currentStep = 1;
    let idempotencyKey = '';
    const newIdempotencyKey = () => crypto.randomUUID?.() || 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, character => {
        const random = Math.floor(Math.random() * 16);
        return (character === 'x' ? random : (random & 0x3) | 0x8).toString(16);
    });

    root.style.setProperty('--cor-primaria', root.dataset.corPrimaria || '#F6C445');
    root.style.setProperty('--cor-secundaria', root.dataset.corSecundaria || '#C98D86');

    const price = value => {
        const parsed = Number.parseFloat(value);
        return Number.isFinite(parsed) && parsed >= 0 ? parsed : 0;
    };
    const quantity = value => {
        const parsed = Number.parseInt(value, 10);
        return Number.isInteger(parsed) ? Math.min(maxQuantity, Math.max(1, parsed)) : 1;
    };
    const readStorage = key => { try { return JSON.parse(localStorage.getItem(key) || '{}'); } catch { return {}; } };
    const text = value => typeof value === 'string' ? value.trim() : '';

    function cartItems() {
        const saved = readStorage(cartKey);
        if (!Array.isArray(saved)) return [];
        return saved.filter(item => item?.slug === slug && Number(item.productId) > 0).map(item => ({
            productId: Number(item.productId), name: text(item.name), quantity: quantity(item.quantity), unitPrice: price(item.unitPrice),
            note: text(item.note).slice(0, 500),
            extras: Array.isArray(item.extras) ? item.extras.map(extra => ({ id: Number(extra.id), name: text(extra.name), price: price(extra.price) })).filter(extra => extra.id > 0) : []
        }));
    }

    const itemTotal = item => (item.unitPrice + item.extras.reduce((sum, extra) => sum + extra.price, 0)) * item.quantity;
    const subtotal = () => cartItems().reduce((sum, item) => sum + itemTotal(item), 0);
    const field = selector => form.querySelector(selector);
    const selectedAttendance = () => form.querySelector('[name="atendimento"]:checked')?.value || '';
    const selectedPayment = () => form.querySelector('[name="pagamento"]:checked');
    const selectedBairro = () => field('[data-bairro]').selectedOptions[0];
    const deliveryFee = () => selectedAttendance() === 'entrega' ? price(selectedBairro()?.dataset.fee) : 0;
    const total = () => subtotal() + deliveryFee();

    function showError(message) { error.textContent = message; error.hidden = !message; }
    function showStep(step) {
        currentStep = step;
        steps.forEach(section => section.hidden = Number(section.dataset.checkoutStep) !== step);
        document.querySelectorAll('[data-step-indicator]').forEach(indicator => indicator.classList.toggle('is-current', Number(indicator.dataset.stepIndicator) === step));
        showError('');
        if (step === 4) renderReview();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function updateDeliveryFields() {
        const delivery = selectedAttendance() === 'entrega';
        document.querySelector('[data-delivery-fields]').hidden = !delivery;
        // Mantém o card selecionado sincronizado com o select que as regras existentes já utilizam.
        updateBairroSelection(); updateBairroInfo(); updateSummary(); persistCheckout();
    }

    function updateBairroSelection() {
        const selectedId = field('[data-bairro]').value;
        // A seleção visual apenas espelha o valor do select; nenhum identificador de empresa ou preço é criado no navegador.
        document.querySelectorAll('[data-bairro-select]').forEach(button => {
            const selected = Boolean(selectedId) && button.dataset.bairroId === selectedId;
            button.classList.toggle('is-selected', selected);
            button.setAttribute('aria-pressed', String(selected));
        });
        // O rótulo resumido é derivado da opção já carregada pelo servidor para a empresa do slug atual.
        const option = selectedBairro();
        if (bairroLabel) bairroLabel.textContent = option?.value
            ? option.textContent.trim() + ' — ' + (price(option.dataset.fee) === 0 ? 'Grátis' : money.format(price(option.dataset.fee)))
            : 'Escolha seu bairro';
    }

    // Abre ou fecha somente a lista visual; o select permanece responsável pelo valor usado na validação.
    function setBairroListOpen(open) {
        if (!bairroList || !bairroToggle) return;
        bairroList.hidden = !open;
        bairroToggle.setAttribute('aria-expanded', String(open));
    }

    function updateBairroInfo() {
        const option = selectedBairro();
        const info = document.querySelector('[data-bairro-info]');
        if (!option?.value) { info.textContent = ''; return; }
        // A confirmação textual mostra apenas a taxa, sem expor informações técnicas da configuração de entrega.
        info.textContent = price(option.dataset.fee) === 0 ? 'Entrega grátis' : `Taxa de entrega: ${money.format(price(option.dataset.fee))}`;
    }

    function updatePaymentFields() {
        const payment = selectedPayment();
        const changeContainer = document.querySelector('[data-change-fields]');
        const supportsChange = payment?.dataset.paymentType === '0' && payment.dataset.acceptsChange === 'true';
        changeContainer.hidden = !supportsChange;
        if (!supportsChange) { field('[data-needs-change]').checked = false; field('[data-change-value]').hidden = true; }
        document.querySelector('[data-payment-info]').textContent = payment?.dataset.paymentNote || (payment?.dataset.paymentType === '3' ? 'Pagamento via Pix na entrega ou retirada.' : '');
        persistCheckout();
    }

    function updateChangeField() { field('[data-change-value]').hidden = !field('[data-needs-change]').checked; persistCheckout(); }

    function updateSummary() {
        const items = cartItems();
        const container = document.querySelector('[data-checkout-items]');
        container.replaceChildren();
        if (!items.length) {
            const empty = document.createElement('p'); empty.className = 'checkout-hint'; empty.textContent = 'Seu carrinho está vazio.'; container.append(empty);
        } else {
            items.forEach(item => {
                const itemElement = document.createElement('article'); itemElement.className = 'checkout-summary-item';
                const title = document.createElement('strong'); title.textContent = `${item.quantity}× ${item.name}`;
                const value = document.createElement('span'); value.textContent = money.format(itemTotal(item));
                itemElement.append(title, value);
                if (item.extras.length) { const extras = document.createElement('small'); extras.textContent = `Adicionais: ${item.extras.map(extra => extra.name).join(', ')}`; itemElement.append(extras); }
                container.append(itemElement);
            });
        }
        document.querySelector('[data-checkout-subtotal]').textContent = money.format(subtotal());
        document.querySelector('[data-checkout-delivery]').textContent = selectedAttendance() === 'entrega' ? money.format(deliveryFee()) : 'Grátis';
        document.querySelector('[data-checkout-total]').textContent = money.format(total());
    }

    function validPhone(phone) { return phone.replace(/\D/g, '').length >= 10; }
    function validate(step) {
        if (!cartItems().length) return 'Seu carrinho está vazio. Volte ao cardápio para adicionar produtos.';
        if (step === 1) {
            const attendance = selectedAttendance();
            if (!attendance) return 'Escolha entrega ou retirada.';
            if (attendance === 'entrega') {
                const bairro = selectedBairro();
                if (!bairro?.value) return 'Selecione um bairro para entrega.';
                // Rua é opcional para permitir estradas, zonas rurais e referências; as demais validações de entrega continuam ativas.
                if (!field('[data-no-number]').checked && !text(field('[data-number]').value)) return 'Informe o número do endereço ou marque sem número.';
                const minimum = price(bairro.dataset.minimum);
                if (minimum > 0 && subtotal() < minimum) return `O pedido mínimo para este bairro é ${money.format(minimum)}.`;
            }
        }
        if (step === 2) {
            if (!text(field('[data-client-name]').value)) return 'Informe seu nome.';
            if (!validPhone(text(field('[data-client-phone]').value))) return 'Informe um telefone brasileiro válido.';
        }
        if (step === 3) {
            const payment = selectedPayment();
            if (!payment) return 'Selecione uma forma de pagamento.';
            if (field('[data-needs-change]').checked && price(field('[data-change-for]').value) <= total()) return 'O valor para troco deve ser maior que o total do pedido.';
        }
        return '';
    }

    function reviewLine(label, value) { const line = document.createElement('div'); const title = document.createElement('strong'); title.textContent = label; const content = document.createElement('span'); content.textContent = value; line.append(title, content); return line; }
    function renderReview() {
        updateSummary();
        const review = document.querySelector('[data-checkout-review]'); review.replaceChildren();
        const payment = selectedPayment();
        review.append(reviewLine('Atendimento', selectedAttendance() === 'entrega' ? 'Entrega' : 'Retirada'));
        review.append(reviewLine('Cliente', text(field('[data-client-name]').value)));
        review.append(reviewLine('Telefone', text(field('[data-client-phone]').value)));
        if (selectedAttendance() === 'entrega') {
            const bairro = selectedBairro();
            // Remove partes vazias da revisão para que uma Rua opcional não gere vírgula ou espaço sem conteúdo.
            const address = [text(field('[data-street]').value), field('[data-no-number]').checked ? 'sem número' : text(field('[data-number]').value)].filter(Boolean).join(', ') + (text(field('[data-complement]').value) ? ` · ${text(field('[data-complement]').value)}` : '');
            review.append(reviewLine('Entrega', `${bairro.textContent.split(' · ')[0]} · ${address}`));
        }
        review.append(reviewLine('Pagamento', payment?.parentElement.textContent.trim() || ''));
        if (field('[data-needs-change]').checked) review.append(reviewLine('Troco para', money.format(price(field('[data-change-for]').value))));
        review.append(reviewLine('Total', money.format(total())));
    }

    function persistCheckout() {
        const data = {
            atendimento: selectedAttendance(), bairroId: field('[data-bairro]').value || '', rua: text(field('[data-street]').value), numero: text(field('[data-number]').value),
            semNumero: field('[data-no-number]').checked, complemento: text(field('[data-complement]').value), referencia: text(field('[data-reference]').value),
            pagamentoId: selectedPayment()?.value || '', precisaTroco: field('[data-needs-change]').checked, trocoPara: text(field('[data-change-for]').value),
            // Preserva a escolha operacional do cliente no mesmo checkout associado ao slug atual.
            aceitaAtualizacoesWhatsApp: field('[data-whatsapp-opt-in]').checked, chaveIdempotencia: idempotencyKey
        };
        localStorage.setItem(checkoutKey, JSON.stringify(data));
        localStorage.setItem(clientKey, JSON.stringify({ nome: text(field('[data-client-name]').value), telefone: text(field('[data-client-phone]').value) }));
    }

    function restoreCheckout() {
        const checkout = readStorage(checkoutKey); const client = readStorage(clientKey);
        idempotencyKey = typeof checkout.chaveIdempotencia === 'string' && checkout.chaveIdempotencia.length > 20
            ? checkout.chaveIdempotencia
            : newIdempotencyKey();
        if (checkout.atendimento) form.querySelector(`[name="atendimento"][value="${checkout.atendimento}"]`)?.click();
        field('[data-bairro]').value = checkout.bairroId || ''; field('[data-street]').value = checkout.rua || ''; field('[data-number]').value = checkout.numero || '';
        field('[data-no-number]').checked = Boolean(checkout.semNumero); field('[data-complement]').value = checkout.complemento || ''; field('[data-reference]').value = checkout.referencia || '';
        if (checkout.pagamentoId) form.querySelector(`[name="pagamento"][value="${checkout.pagamentoId}"]`)?.click();
        field('[data-needs-change]').checked = Boolean(checkout.precisaTroco); field('[data-change-for]').value = checkout.trocoPara || '';
        field('[data-client-name]').value = client.nome || ''; field('[data-client-phone]').value = client.telefone || '';
        field('[data-whatsapp-opt-in]').checked = Boolean(checkout.aceitaAtualizacoesWhatsApp);
        // Restaura também o destaque do card correspondente ao bairro salvo para o slug atual.
        updateDeliveryFields(); updatePaymentFields(); updateChangeField(); updateSummary();
    }

    form.addEventListener('change', event => {
        if (event.target.matches('[name="atendimento"]')) updateDeliveryFields();
        // Alterações pelo select sincronizado atualizam destaque, resumo e persistência já existentes.
        if (event.target.matches('[data-bairro]')) { updateBairroSelection(); updateBairroInfo(); updateSummary(); persistCheckout(); }
        if (event.target.matches('[name="pagamento"]')) updatePaymentFields();
        if (event.target.matches('[data-needs-change]')) updateChangeField();
        if (event.target.matches('[data-no-number]')) persistCheckout();
    });
    form.addEventListener('input', () => persistCheckout());
    document.querySelector('[data-bairro-list]')?.addEventListener('click', event => {
        const button = event.target.closest('[data-bairro-select]');
        if (!button) return;
        // O clique do card atualiza o mesmo campo utilizado na validação e dispara seu fluxo normal de mudança.
        field('[data-bairro]').value = button.dataset.bairroId || '';
        field('[data-bairro]').dispatchEvent(new Event('change', { bubbles: true }));
        // Após a seleção, a lista compacta fecha para liberar espaço no checkout.
        setBairroListOpen(false);
    });
    // O botão permite revisar bairros sem alterar o valor atual até uma opção ser escolhida.
    bairroToggle?.addEventListener('click', () => setBairroListOpen(bairroList?.hidden));
    // Um clique fora do seletor fecha apenas a lista visual, sem limpar a seleção já validada.
    document.addEventListener('click', event => { if (bairroPicker && !bairroPicker.contains(event.target)) setBairroListOpen(false); });
    form.querySelectorAll('[data-next]').forEach(button => button.addEventListener('click', () => { const validation = validate(currentStep); if (validation) return showError(validation); showStep(currentStep + 1); }));
    form.querySelectorAll('[data-back]').forEach(button => button.addEventListener('click', () => showStep(currentStep - 1)));
    form.querySelector('[data-confirm]').addEventListener('click', async event => {
        for (let step = 1; step <= 3; step++) { const validation = validate(step); if (validation) { showStep(step); return showError(validation); } }
        const button = event.currentTarget;
        button.disabled = true;
        persistCheckout();
        const dados = new URLSearchParams();
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        dados.set('__RequestVerificationToken', token);
        dados.set('ChaveIdempotencia', idempotencyKey);
        dados.set('TipoAtendimento', selectedAttendance() === 'entrega' ? '0' : '1');
        dados.set('NomeCliente', text(field('[data-client-name]').value));
        dados.set('TelefoneCliente', text(field('[data-client-phone]').value));
        // O backend grava esta autorização junto do pedido e só envia confirmação quando ela foi marcada.
        dados.set('AceitaAtualizacoesWhatsApp', String(field('[data-whatsapp-opt-in]').checked));
        dados.set('BairroEntregaId', field('[data-bairro]').value || '');
        dados.set('Rua', text(field('[data-street]').value));
        dados.set('NumeroEndereco', text(field('[data-number]').value));
        dados.set('SemNumero', String(field('[data-no-number]').checked));
        dados.set('Complemento', text(field('[data-complement]').value));
        dados.set('Referencia', text(field('[data-reference]').value));
        dados.set('FormaPagamentoId', selectedPayment()?.value || '');
        dados.set('PrecisaTroco', String(field('[data-needs-change]').checked));
        dados.set('TrocoPara', text(field('[data-change-for]').value));
        cartItems().forEach((item, index) => {
            dados.set(`Itens[${index}].ProdutoId`, String(item.productId));
            dados.set(`Itens[${index}].Quantidade`, String(item.quantity));
            dados.set(`Itens[${index}].Observacao`, item.note);
            item.extras.forEach((extra, extraIndex) => dados.append(`Itens[${index}].AdicionalIds[${extraIndex}]`, String(extra.id)));
        });
        try {
            const response = await fetch(form.dataset.finalizeUrl, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8' }, body: dados.toString() });
            const payload = await response.json().catch(() => ({}));
            if (!response.ok || !payload.redirectUrl) throw new Error(payload.erro || 'Não foi possível registrar o pedido.');
            window.location.assign(payload.redirectUrl);
        } catch (failure) {
            showError(failure.message || 'Não foi possível registrar o pedido.');
            button.disabled = false;
        }
    });

    // Nenhum preço, taxa ou item armazenado no navegador será aceito para criar o pedido: a próxima etapa deverá recalcular tudo no servidor.
    restoreCheckout();
})();
