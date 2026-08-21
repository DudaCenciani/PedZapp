(() => {
    const root = document.querySelector('[data-pedidos-realtime]');
    if (!root) return;
    if (!window.signalR) {
        // Este erro identifica imediatamente falha de carregamento do cliente SignalR, sem ocultar a causa no navegador.
        console.error('[Pedidos realtime] Cliente SignalR não foi carregado.');
        return;
    }

    // O slug vem da View renderizada no servidor; o Hub ainda o confronta com o EmpresaId do usuário autenticado.
    const slug = root.dataset.realtimeSlug;
    if (!slug) return;

    const storageKey = 'pedzapp-pedidos-notificados-' + slug;
    const unreadKey = 'pedzapp-pedidos-nao-vistos-' + slug;
    const knownEvents = new Set(JSON.parse(sessionStorage.getItem(storageKey) || '[]'));
    const maximumRememberedEvents = 100;
    const maximumVisibleToasts = 3;
    const queue = [];
    let soundEnabled = false;
    let reloadScheduled = false;
    let unreadCount = Number.parseInt(sessionStorage.getItem(unreadKey) || '0', 10) || 0;
    const badge = document.querySelector('[data-pedidos-new-badge]');

    const container = document.createElement('section');
    container.className = 'pedidos-realtime-toasts';
    container.setAttribute('aria-live', 'polite');
    container.setAttribute('aria-label', 'Avisos de novos pedidos');
    document.body.append(container);

    function updateBadge() {
        if (!badge) return;
        badge.hidden = unreadCount < 1;
        badge.textContent = unreadCount > 99 ? '99+' : String(unreadCount);
    }

    // Ao abrir Pedidos, os novos já estão visíveis no quadro ou serão carregados no refresh controlado.
    if (root.dataset.realtimePage === 'pedidos') {
        unreadCount = 0;
        sessionStorage.removeItem(unreadKey);
    }
    updateBadge();

    // A política do navegador exige interação humana; a página habilita o som na primeira ação sem tentar burlar essa proteção.
    const activateSound = () => { soundEnabled = true; };
    document.addEventListener('pointerdown', activateSound, { once: true });
    document.addEventListener('keydown', activateSound, { once: true });

    function rememberEvent(eventId) {
        knownEvents.add(eventId);
        sessionStorage.setItem(storageKey, JSON.stringify([...knownEvents].slice(-maximumRememberedEvents)));
    }

    function playSound() {
        if (!soundEnabled || !window.AudioContext) return;

        // Um tom curto gerado localmente evita download de mídia e toca somente uma vez por evento novo.
        const context = new window.AudioContext();
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.frequency.value = 880;
        gain.gain.setValueAtTime(.06, context.currentTime);
        gain.gain.exponentialRampToValueAtTime(.001, context.currentTime + .18);
        oscillator.connect(gain).connect(context.destination);
        oscillator.start();
        oscillator.stop(context.currentTime + .18);
        oscillator.addEventListener('ended', () => context.close());
    }

    function textoMoeda(value) {
        return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(value) || 0);
    }

    function closeToast(toast) {
        toast.remove();
        if (queue.length) showToast(queue.shift());
    }

    function button(text, className) {
        const element = document.createElement('button');
        element.type = 'button';
        element.className = className;
        element.textContent = text;
        return element;
    }

    function showToast(notification) {
        if (container.children.length >= maximumVisibleToasts) {
            queue.push(notification);
            return;
        }

        const toast = document.createElement('article');
        toast.className = 'pedido-realtime-toast';
        const header = document.createElement('div');
        header.className = 'pedido-realtime-toast__header';
        const title = document.createElement('strong');
        title.textContent = '🔔 NOVO PEDIDO';
        const close = button('×', 'pedido-realtime-toast__close');
        close.setAttribute('aria-label', 'Fechar aviso');
        close.addEventListener('click', () => closeToast(toast));
        header.append(title, close);

        const body = document.createElement('div');
        body.className = 'pedido-realtime-toast__body';
        const number = document.createElement('strong');
        number.textContent = 'Pedido #' + notification.numeroPedido;
        const customer = document.createElement('span');
        customer.textContent = notification.nomeCliente;
        const total = document.createElement('span');
        total.textContent = textoMoeda(notification.total);
        const attendance = document.createElement('small');
        attendance.textContent = notification.tipoAtendimento;
        body.append(number, customer, total, attendance);

        const footer = document.createElement('div');
        footer.className = 'pedido-realtime-toast__footer';
        const link = document.createElement('a');
        link.className = 'pedido-realtime-toast__link';
        link.href = notification.urlDetalhes;
        link.textContent = notification.textoAcao || 'Ver pedido';
        footer.append(link);

        // Quando não houve interação anterior, o próprio aviso oferece uma ativação explícita e compatível com autoplay.
        if (!soundEnabled) {
            const sound = button('Ativar som', 'pedido-realtime-toast__sound');
            sound.addEventListener('click', () => {
                soundEnabled = true;
                playSound();
                sound.remove();
            });
            footer.append(sound);
        }

        toast.append(header, body, footer);
        container.append(toast);
        // Quinze segundos deixam o aviso perceptível sem bloquear a operação; o X continua permitindo fechar antes.
        window.setTimeout(() => closeToast(toast), 15000);
    }

    function refreshPedidosIfNecessary() {
        // O recarregamento controlado preserva busca e filtros da URL atual e só ocorre quando o quadro está no dia de hoje.
        if (root.dataset.realtimePage !== 'pedidos' || root.dataset.realtimeExibindoHoje !== 'true' || reloadScheduled) return;
        reloadScheduled = true;
        window.setTimeout(() => window.location.reload(), 5000);
    }

    function receiveNotification(notification) {
        if (!notification?.eventoId || knownEvents.has(notification.eventoId)) return;
        rememberEvent(notification.eventoId);
        // Mantém um indicador extra no acesso de Pedidos até que a lista seja aberta nesta aba.
        unreadCount += 1;
        sessionStorage.setItem(unreadKey, String(unreadCount));
        updateBadge();
        // Em uma data histórica, cada aviso aponta para a lista diária sem inserir um pedido recente no filtro antigo.
        if (root.dataset.realtimePage === 'pedidos' && root.dataset.realtimeExibindoHoje !== 'true') {
            const todayUrl = root.dataset.realtimePedidosHojeUrl;
            if (todayUrl) {
                notification.urlDetalhes = todayUrl;
                notification.textoAcao = 'Ver pedidos de hoje';
            }
        }
        console.info('[Pedidos realtime] NovoPedido recebido:', notification.eventoId);
        playSound();
        showToast(notification);
        refreshPedidosIfNecessary();
    }

    // A reconexão automática não cria pedidos nem reenvia eventos: ela apenas restabelece o canal de aviso.
    const connection = new window.signalR.HubConnectionBuilder()
        .withUrl('/hubs/pedidos?slug=' + encodeURIComponent(slug))
        .withAutomaticReconnect()
        .build();

    connection.on('NovoPedido', receiveNotification);
    connection.onreconnecting(error => console.warn('[Pedidos realtime] Reconectando ao Hub.', error));
    connection.onreconnected(connectionId => console.info('[Pedidos realtime] Hub reconectado:', connectionId));
    connection.onclose(error => console.warn('[Pedidos realtime] Conexão com o Hub encerrada.', error));
    connection.start()
        .then(() => console.info('[Pedidos realtime] Hub conectado para o slug:', slug))
        .catch(error => {
            // A tela continua plenamente utilizável sem SignalR; o banco e a atualização normal seguem como fonte da verdade.
            console.error('[Pedidos realtime] Falha ao conectar ao Hub.', error);
        });

    const testForm = document.querySelector('[data-pedidos-realtime-test]');
    testForm?.addEventListener('submit', async event => {
        event.preventDefault();
        const button = testForm.querySelector('button[type="submit"]');
        const token = testForm.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        if (!button || !token) return;
        button.disabled = true;
        try {
            const response = await fetch(testForm.action, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: new URLSearchParams({ __RequestVerificationToken: token })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok || !result.success) throw new Error('Não foi possível enviar o aviso de teste.');
            console.info('[Pedidos realtime] Aviso de Development solicitado.');
        } catch (error) {
            console.error('[Pedidos realtime] Falha no aviso de Development.', error);
        } finally {
            button.disabled = false;
        }
    });
})();
