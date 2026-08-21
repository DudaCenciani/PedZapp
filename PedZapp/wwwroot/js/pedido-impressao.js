(() => {
    document.querySelectorAll('.pedido-card-actions form').forEach(form => {
        const button = form.querySelector('button');
        if (!button || !button.textContent.includes('Confirmar')) return;
        form.addEventListener('submit', async event => {
            event.preventDefault();
            if (button.disabled) return;
            const printWindow = window.open('', '_blank');
            button.disabled = true;
            try {
                const response = await fetch(form.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: new FormData(form) });
                const result = await response.json();
                if (!response.ok || !result.printUrl) throw new Error();
                if (printWindow) printWindow.location.assign(result.printUrl);
                else window.location.assign(result.printUrl);
                if (printWindow) window.location.assign(result.redirectUrl);
            } catch {
                if (printWindow) printWindow.close();
                window.location.reload();
            }
        });
    });

    const printPage = document.querySelector('[data-pedido-impressao]');
    if (!printPage) return;
    let concluded = false;
    const conclude = async () => {
        if (concluded) return;
        concluded = true;
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        try { await fetch(printPage.dataset.concluirUrl, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token }, body: new URLSearchParams({ __RequestVerificationToken: token }), keepalive: true }); } catch { }
        window.setTimeout(() => { if (window.opener) window.close(); else window.location.assign(printPage.dataset.voltarUrl); }, 250);
    };
    window.addEventListener('beforeprint', () => { printPage.dataset.printing = 'true'; });
    window.addEventListener('afterprint', conclude);
    window.addEventListener('load', () => window.setTimeout(() => window.print(), 180));
})();
