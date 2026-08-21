(() => {
    const root = document.querySelector('[data-cart-root]');
    if (!root) return;

    const slug = root.dataset.slug;
    // O carrinho é separado por slug para nunca reutilizar produtos exibidos por outra empresa.
    const storageKey = `pedzapp-carrinho-${slug}`;
    const maxQuantity = 99;
    const maxNoteLength = 500;
    const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
    const drawer = document.querySelector('[data-cart-drawer]');
    const backdrop = document.querySelector('[data-cart-backdrop]');
    const itemsContainer = document.querySelector('[data-cart-items]');
    const suggestions = document.querySelector('[data-cart-suggestions]');
    const suggestionList = document.querySelector('[data-cart-suggestion-list]');
    const addedModal = document.querySelector('#item-adicionado');
    const catalog = new Map();
    let activeAddedIndex = null;

    root.style.setProperty('--cor-primaria', root.dataset.corPrimaria || '#F6C445');
    root.style.setProperty('--cor-secundaria', root.dataset.corSecundaria || '#C98D86');

    // O catálogo visual é extraído somente dos formulários renderizados pelo CardapioController para este slug.
    document.querySelectorAll('[data-product-form]').forEach(form => {
        const productId = Number(form.dataset.productId);
        const extras = new Map();
        form.querySelectorAll('[data-extra-id]').forEach(input => extras.set(Number(input.dataset.extraId), {
            id: Number(input.dataset.extraId),
            name: input.dataset.extraName || '',
            price: toPrice(input.dataset.extraPrice)
        }));
        catalog.set(productId, {
            id: productId,
            image: form.dataset.productImage || '',
            name: form.dataset.productName || '',
            price: toPrice(form.dataset.productPrice),
            // A marcação visual impede sugestões e inclusões locais; o PedidoService confirma novamente no banco.
            available: form.dataset.productAvailable === 'true',
            extras
        });
    });

    let cart = loadCart();

    function toPrice(value) {
        const parsed = Number.parseFloat(value);
        return Number.isFinite(parsed) && parsed >= 0 ? parsed : 0;
    }

    function clampQuantity(value) {
        const parsed = Number.parseInt(value, 10);
        return Number.isInteger(parsed) ? Math.min(maxQuantity, Math.max(1, parsed)) : 1;
    }

    function normalizeItem(item) {
        if (!item || item.slug !== slug) return null;
        const product = catalog.get(Number(item.productId));
        if (!product) return null;
        const requestedExtraIds = Array.isArray(item.extras) ? item.extras.map(extra => Number(extra.id)) : [];
        const extras = [...new Set(requestedExtraIds)].map(id => product.extras.get(id)).filter(Boolean);
        const note = typeof item.note === 'string' ? item.note.trim().slice(0, maxNoteLength) : '';

        // Nome, imagem e preço local são reconstruídos do catálogo atual da mesma página; o servidor recalcula tudo no pedido.
        return { slug, productId: product.id, image: product.image, name: product.name, unitPrice: product.price, quantity: clampQuantity(item.quantity), extras, note };
    }

    function loadCart() {
        try {
            const saved = JSON.parse(localStorage.getItem(storageKey) || '[]');
            return Array.isArray(saved) ? saved.map(normalizeItem).filter(Boolean) : [];
        } catch {
            return [];
        }
    }

    function saveCart() {
        // Valores locais atendem apenas à prévia visual; PedidoService valida produtos, adicionais e valores no servidor.
        localStorage.setItem(storageKey, JSON.stringify(cart));
    }

    function itemTotal(item) {
        return (item.unitPrice + item.extras.reduce((sum, extra) => sum + extra.price, 0)) * item.quantity;
    }

    function subtotal() {
        return cart.reduce((sum, item) => sum + itemTotal(item), 0);
    }

    function signature(item) {
        return `${item.productId}|${item.extras.map(extra => extra.id).sort((a, b) => a - b).join(',')}|${item.note}`;
    }

    function updateCounter() {
        const count = cart.reduce((sum, item) => sum + item.quantity, 0);
        document.querySelectorAll('[data-cart-count]').forEach(element => element.textContent = count);
        document.querySelectorAll('[data-cart-subtotal], [data-cart-drawer-subtotal], [data-cart-drawer-total]').forEach(element => element.textContent = money.format(subtotal()));
    }

    function actionButton(text, label, action, itemIndex) {
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = text;
        button.dataset.cartAction = action;
        button.dataset.itemIndex = itemIndex;
        button.setAttribute('aria-label', label);
        return button;
    }

    function productImage(item) {
        const frame = document.createElement('span');
        frame.className = 'cart-item__image';
        if (item.image) {
            const image = document.createElement('img');
            image.src = item.image;
            image.alt = '';
            image.loading = 'lazy';
            image.addEventListener('error', () => image.remove());
            frame.append(image);
        } else {
            frame.textContent = '◈';
            frame.setAttribute('aria-hidden', 'true');
        }
        return frame;
    }

    function renderSuggestions() {
        if (!suggestions || !suggestionList) return;
        suggestionList.replaceChildren();
        const idsInCart = new Set(cart.map(item => item.productId));
        // Sugestões usam o catálogo já projetado para a empresa atual, sem algoritmo ou consulta adicional.
        const options = [...catalog.values()].filter(product => product.available && !idsInCart.has(product.id)).slice(0, 6);
        suggestions.hidden = !options.length;
        options.forEach(product => {
            const card = document.createElement('article');
            card.className = 'cart-suggestion';
            if (product.image) {
                const image = document.createElement('img');
                image.src = product.image;
                image.alt = '';
                image.loading = 'lazy';
                image.addEventListener('error', () => image.remove());
                card.append(image);
            }
            const name = document.createElement('strong'); name.textContent = product.name;
            const price = document.createElement('span'); price.textContent = money.format(product.price);
            const add = document.createElement('button'); add.type = 'button'; add.textContent = '+'; add.dataset.cartSuggestion = product.id; add.setAttribute('aria-label', `Adicionar ${product.name}`);
            card.append(name, price, add);
            suggestionList.append(card);
        });
    }

    function renderCart() {
        itemsContainer.replaceChildren();
        drawer.classList.toggle('cart-drawer--empty', !cart.length);
        if (!cart.length) {
            const empty = document.createElement('section'); empty.className = 'cart-empty';
            empty.innerHTML = '<span aria-hidden="true">🛒</span><h3>Seu carrinho está vazio.</h3><p>Escolha produtos do cardápio para começar seu pedido.</p>';
            empty.append(actionButton('Ver cardápio', 'Ver cardápio', 'close', ''));
            itemsContainer.append(empty);
        } else {
            cart.forEach((item, index) => {
                const article = document.createElement('article'); article.className = 'cart-item';
                const overview = document.createElement('div'); overview.className = 'cart-item__overview';
                const detail = document.createElement('div'); detail.className = 'cart-item__details';
                const title = document.createElement('h3'); title.textContent = `${item.quantity}× ${item.name}`;
                const unit = document.createElement('small'); unit.textContent = `Preço unitário: ${money.format(item.unitPrice)}`;
                detail.append(title, unit);
                if (item.extras.length) {
                    const extras = document.createElement('ul'); extras.className = 'cart-item__extras';
                    item.extras.forEach(extra => { const extraItem = document.createElement('li'); extraItem.textContent = `+ ${extra.name}`; extras.append(extraItem); });
                    detail.append(extras);
                }
                if (item.note) { const note = document.createElement('p'); note.className = 'cart-item__note'; const label = document.createElement('strong'); label.textContent = 'Observação:'; note.append(label, document.createTextNode(` ${item.note}`)); detail.append(note); }
                overview.append(productImage(item), detail);
                const controls = document.createElement('div'); controls.className = 'cart-item__controls';
                controls.append(actionButton('−', `Diminuir quantidade de ${item.name}`, 'decrease', index));
                const quantity = document.createElement('span'); quantity.className = 'cart-item__quantity'; quantity.textContent = item.quantity; quantity.setAttribute('aria-label', `Quantidade: ${item.quantity}`); controls.append(quantity);
                controls.append(actionButton('+', `Aumentar quantidade de ${item.name}`, 'increase', index));
                const edit = actionButton('Editar', `Editar ${item.name}`, 'edit', index); edit.className = 'cart-edit'; controls.append(edit);
                const remove = actionButton('Remover', `Remover ${item.name}`, 'remove', index); remove.className = 'cart-remove'; controls.append(remove);
                const total = document.createElement('p'); total.className = 'cart-item__total'; const totalLabel = document.createElement('span'); totalLabel.textContent = 'Subtotal'; const totalValue = document.createElement('strong'); totalValue.textContent = money.format(itemTotal(item)); total.append(totalLabel, totalValue);
                article.append(overview, controls, total); itemsContainer.append(article);
            });
        }
        renderSuggestions();
        updateCounter();
    }

    function saveAndRender() { saveCart(); renderCart(); }

    function openDrawer() { drawer.classList.add('is-open'); drawer.setAttribute('aria-hidden', 'false'); backdrop.hidden = false; drawer.focus(); }
    function closeDrawer() { drawer.classList.remove('is-open'); drawer.setAttribute('aria-hidden', 'true'); backdrop.hidden = true; document.querySelector('[data-cart-open]')?.focus(); }

    function selectedExtras(form) {
        return [...form.querySelectorAll('[data-extra-id]:checked')].map(input => ({ id: Number(input.dataset.extraId), name: input.dataset.extraName || '', price: toPrice(input.dataset.extraPrice) }));
    }

    function updateProductTotal(form) {
        const product = catalog.get(Number(form.dataset.productId));
        if (!product) return;
        const total = (product.price + selectedExtras(form).reduce((sum, extra) => sum + extra.price, 0)) * clampQuantity(form.querySelector('[data-quantity]').value);
        form.querySelector('[data-product-total]').textContent = money.format(total);
    }

    function enforceExtraLimit(input) {
        const form = input.closest('[data-product-form]');
        const maximums = [...form.querySelectorAll('[data-extra-id]:checked')].map(extra => Number(extra.dataset.extraMax)).filter(value => Number.isInteger(value) && value > 0);
        if (maximums.length && form.querySelectorAll('[data-extra-id]:checked').length > Math.min(...maximums)) {
            input.checked = false;
            window.alert(`Esta seleção permite no máximo ${Math.min(...maximums)} adicional(is).`);
        }
    }

    function showAddedFeedback(index) {
        activeAddedIndex = index;
        const item = cart[index];
        if (!item || !addedModal) return openDrawer();
        document.querySelector('[data-added-name]').textContent = item.name;
        document.querySelector('[data-added-quantity]').textContent = item.quantity;
        document.querySelector('[data-added-note]').value = item.note;
        window.bootstrap?.Modal.getOrCreateInstance(addedModal).show();
    }

    function openEditor(index) {
        const item = cart[index];
        const form = document.querySelector(`[data-product-form][data-product-id="${item?.productId}"]`);
        if (!item || !form) return;
        form.dataset.editIndex = String(index);
        form.querySelector('[data-quantity]').value = item.quantity;
        form.querySelectorAll('[data-extra-id]').forEach(input => input.checked = item.extras.some(extra => extra.id === Number(input.dataset.extraId)));
        const note = form.querySelector('[data-note]'); if (note) note.value = item.note;
        updateProductTotal(form);
        closeDrawer();
        window.bootstrap?.Modal.getOrCreateInstance(form.closest('.modal')).show();
    }

    document.querySelectorAll('[data-product-form]').forEach(form => {
        form.addEventListener('input', () => updateProductTotal(form));
        form.addEventListener('change', event => { if (event.target.matches('[data-extra-id]')) enforceExtraLimit(event.target); updateProductTotal(form); });
        form.addEventListener('submit', event => {
            event.preventDefault();
            const product = catalog.get(Number(form.dataset.productId));
            if (!product) return;
            // Não permite inclusão local quando o cardápio já informou indisponibilidade; a validação crítica continua no servidor.
            if (!product.available) return window.alert('Este produto não está disponível no momento.');
            const item = { slug, productId: product.id, image: product.image, name: product.name, unitPrice: product.price, quantity: clampQuantity(form.querySelector('[data-quantity]').value), extras: selectedExtras(form), note: (form.querySelector('[data-note]')?.value || '').trim().slice(0, maxNoteLength) };
            const editIndex = Number(form.dataset.editIndex);
            let itemIndex;
            if (Number.isInteger(editIndex) && cart[editIndex]) { cart[editIndex] = item; itemIndex = editIndex; }
            else {
                const existingIndex = cart.findIndex(existingItem => signature(existingItem) === signature(item));
                if (existingIndex >= 0) { cart[existingIndex].quantity = Math.min(maxQuantity, cart[existingIndex].quantity + item.quantity); itemIndex = existingIndex; }
                else { cart.push(item); itemIndex = cart.length - 1; }
            }
            delete form.dataset.editIndex;
            saveAndRender();
            window.bootstrap?.Modal.getOrCreateInstance(form.closest('.modal')).hide();
            form.reset(); updateProductTotal(form); showAddedFeedback(itemIndex);
        });
    });

    // A busca filtra apenas o conteúdo presente na tela pública atual e oculta títulos de categorias sem resultados.
    document.querySelector('[data-menu-search]')?.addEventListener('input', event => {
        const term = event.target.value.trim().toLocaleLowerCase('pt-BR');
        let matches = 0;
        document.querySelectorAll('[data-menu-product]').forEach(card => {
            const visible = !term || (card.dataset.menuSearchable || '').includes(term);
            card.hidden = !visible;
            if (visible) matches++;
        });
        document.querySelectorAll('[data-menu-category]').forEach(section => section.hidden = ![...section.querySelectorAll('[data-menu-product]')].some(card => !card.hidden));
        document.querySelector('[data-menu-search-empty]').hidden = matches > 0;
    });

    document.querySelectorAll('[data-cart-open]').forEach(button => button.addEventListener('click', openDrawer));
    document.querySelectorAll('[data-cart-close], [data-cart-backdrop]').forEach(element => element.addEventListener('click', closeDrawer));
    document.querySelector('[data-cart-clear]')?.addEventListener('click', () => { cart = []; saveAndRender(); });
    itemsContainer.addEventListener('click', event => {
        const button = event.target.closest('[data-cart-action]'); if (!button) return;
        const action = button.dataset.cartAction; const index = Number(button.dataset.itemIndex);
        if (action === 'close') return closeDrawer();
        if (!Number.isInteger(index) || !cart[index]) return;
        if (action === 'increase') cart[index].quantity = Math.min(maxQuantity, cart[index].quantity + 1);
        if (action === 'decrease') cart[index].quantity > 1 ? cart[index].quantity-- : cart.splice(index, 1);
        if (action === 'remove') cart.splice(index, 1);
        if (action === 'edit') return openEditor(index);
        saveAndRender();
    });
    suggestionList?.addEventListener('click', event => {
        const button = event.target.closest('[data-cart-suggestion]'); const product = catalog.get(Number(button?.dataset.cartSuggestion));
        if (!product) return;
        cart.push({ slug, productId: product.id, image: product.image, name: product.name, unitPrice: product.price, quantity: 1, extras: [], note: '' });
        saveAndRender(); showAddedFeedback(cart.length - 1);
    });
    document.querySelectorAll('[data-added-action]').forEach(button => button.addEventListener('click', () => {
        const item = cart[activeAddedIndex]; if (!item) return;
        item.quantity = button.dataset.addedAction === 'increase' ? Math.min(maxQuantity, item.quantity + 1) : Math.max(1, item.quantity - 1);
        saveAndRender(); document.querySelector('[data-added-quantity]').textContent = item.quantity;
    }));
    document.querySelector('[data-added-note]')?.addEventListener('input', event => {
        const item = cart[activeAddedIndex]; if (!item) return;
        item.note = event.target.value.trim().slice(0, maxNoteLength); saveAndRender();
    });
    document.querySelector('[data-added-go-cart]')?.addEventListener('click', () => { window.bootstrap?.Modal.getOrCreateInstance(addedModal).hide(); openDrawer(); });
    addedModal?.addEventListener('hidden.bs.modal', () => { activeAddedIndex = null; });
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && drawer.classList.contains('is-open')) closeDrawer(); });
    document.querySelectorAll('[data-menu-img]').forEach(image => image.addEventListener('error', () => image.closest('.menu-product__image')?.replaceChildren('◈')));
    document.querySelector('[data-share]')?.addEventListener('click', async function () { const url = location.href; if (navigator.share) await navigator.share({ title: document.title, url }); else { await navigator.clipboard.writeText(url); this.textContent = 'Link copiado'; } });
    renderCart();
})();
