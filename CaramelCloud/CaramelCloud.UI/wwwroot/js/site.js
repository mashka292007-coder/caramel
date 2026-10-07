
document.addEventListener('DOMContentLoaded', () => {
    setTimeout(() => {
        document.querySelectorAll('.alert.fade.show').forEach(alert => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        });
    }, 4000);
});

function addToCart(id, name, price, image) {
    const cart = JSON.parse(localStorage.getItem('cc_cart') || '[]');
    const existing = cart.find(i => i.id === id);
    if (existing) {
        existing.qty += 1;
    } else {
        cart.push({ id, name, price: parseFloat(price), image: image || '', qty: 1 });
    }
    localStorage.setItem('cc_cart', JSON.stringify(cart));
    updateCartBadge();
    showCartToast(name);
}

function updateCartBadge() {
    const cart = JSON.parse(localStorage.getItem('cc_cart') || '[]');
    const count = cart.reduce((s, i) => s + i.qty, 0);
    document.querySelectorAll('.cc-cart-badge').forEach(el => {
        el.textContent = count;
        el.style.display = count > 0 ? 'flex' : 'none';
    });
}

function showCartToast(name) {
    const existing = document.getElementById('cc-toast');
    if (existing) existing.remove();
    const toast = document.createElement('div');
    toast.id = 'cc-toast';
    toast.innerHTML = `<i class="bi bi-check-circle me-2" style="color:#15803d;"></i>
                       <span style="color:#171717;font-size:.875rem;">
                           <strong>${name}</strong> додано до кошика
                       </span>`;
    toast.style.cssText = `position:fixed;bottom:1.5rem;right:1.5rem;
        background:#fff;border:1px solid rgba(22,163,74,.2);
        border-radius:12px;padding:.75rem 1.25rem;
        box-shadow:0 8px 24px rgba(0,0,0,.1);
        display:flex;align-items:center;gap:.5rem;
        z-index:9999;animation:slideIn .3s ease;`;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 2500);
}

document.addEventListener('DOMContentLoaded', updateCartBadge);

const CHAT_ENDPOINT = '/Product/Chat';
let chatOpen = false;
let chatHistory = [];

function toggleChat() {
    const popup = document.getElementById('aiChatPopup');
    const toggle = document.getElementById('aiChatToggle');
    if (!popup || !toggle) return;
    chatOpen = !chatOpen;
    popup.classList.toggle('visible', chatOpen);
    toggle.classList.toggle('open', chatOpen);
    if (chatOpen) document.getElementById('aiChatInput')?.focus();
}

async function sendMessage() {
    const input = document.getElementById('aiChatInput');
    if (!input) return;
    const text = input.value.trim();
    if (!text) return;

    input.value = '';
    appendMessage(text, 'user');
    chatHistory.push({ role: 'user', text: text });  

    const typingEl = appendTyping();
    document.getElementById('aiChatSend').disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const res = await fetch(CHAT_ENDPOINT, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { 'RequestVerificationToken': token } : {})
            },
            body: JSON.stringify({
                message: text,
                history: chatHistory.slice(-6)  
            })
        });

        const data = await res.json();
        typingEl.remove();

        const cleanText = data.reply ?? '';
        appendMessage(cleanText, 'bot');
        chatHistory.push({ role: 'bot', text: cleanText });

        if (data.products?.length) {
            appendProductCards(data.products);
            filterByAi(data.products.map(p => p.name));
        }

    } catch {
        typingEl.remove();
        appendMessage('Не вдалось отримати відповідь. Спробуйте пізніше.', 'bot');
    } finally {
        document.getElementById('aiChatSend').disabled = false;
        document.getElementById('aiChatInput')?.focus();
    }
}

function appendMessage(text, role) {
    const wrap = document.getElementById('aiChatMessages');
    if (!wrap) return;
    const div = document.createElement('div');
    div.className = `ai-msg ai-msg--${role}`;
    div.innerHTML = `<div class="ai-msg-bubble">${escapeHtml(text)}</div>`;
    wrap.appendChild(div);
    wrap.scrollTop = wrap.scrollHeight;
    return div;
}

function appendTyping() {
    const wrap = document.getElementById('aiChatMessages');
    const div = document.createElement('div');
    div.className = 'ai-msg ai-msg--bot ai-typing';
    div.innerHTML = `<div class="ai-msg-bubble">
        <span class="ai-typing-dot"></span>
        <span class="ai-typing-dot"></span>
        <span class="ai-typing-dot"></span>
    </div>`;
    wrap?.appendChild(div);
    if (wrap) wrap.scrollTop = wrap.scrollHeight;
    return div;
}

function appendFilterButton(names) {
    const wrap = document.getElementById('aiChatMessages');
    if (!wrap) return;
    const div = document.createElement('div');
    div.className = 'ai-msg ai-msg--bot';
    div.innerHTML = `
        <div class="ai-msg-bubble" style="padding:0; background:transparent; border:none;">
            <button onclick="filterByAi(${JSON.stringify(names)})"
                    style="background:rgba(139,69,19,.1); color:var(--cc-primary);
                           border:1px solid rgba(139,69,19,.3); border-radius:8px;
                           padding:.4rem .85rem; font-size:.78rem; font-weight:500;
                           cursor:pointer; transition:background .15s;"
                    onmouseover="this.style.background='rgba(139,69,19,.2)'"
                    onmouseout="this.style.background='rgba(139,69,19,.1)'">
                <i class="bi bi-funnel me-1"></i>Показати тільки рекомендовані
            </button>
            <button onclick="clearAiFilter()"
                    style="background:none; color:var(--cc-text-muted);
                           border:none; font-size:.78rem; cursor:pointer;
                           padding:.4rem .5rem; margin-left:.25rem;"
                    onmouseover="this.style.color='var(--cc-primary)'"
                    onmouseout="this.style.color='var(--cc-text-muted)'">
                Скинути
            </button>
        </div>`;
    wrap.appendChild(div);
    wrap.scrollTop = wrap.scrollHeight;
}

function filterByAi(names) {
    const cleanNames = names.map(n => n.replace(/[«»""„"'']/g, '').trim());
    const cards = document.querySelectorAll('.product-card-col');
    let visible = 0;
    cards.forEach(card => {
        const cardName = (card.querySelector('h5')?.innerText ?? '')
            .replace(/[«»""„"'']/g, '').trim();
        const match = cleanNames.some(n =>
            cardName.toLowerCase().includes(n.toLowerCase()) ||
            n.toLowerCase().includes(cardName.toLowerCase())
        );
        card.style.display = match ? '' : 'none';
        if (match) visible++;
    });
    const noMsg = document.getElementById('noProductsMsg');
    if (noMsg) noMsg.classList.toggle('d-none', visible > 0);
    document.getElementById('clientProductGrid')
        ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function clearAiFilter() {
    document.querySelectorAll('.product-card-col').forEach(card => {
        card.style.display = '';
        card.style.outline = '';
        card.style.borderRadius = '';
    });
    const noMsg = document.getElementById('noProductsMsg');
    if (noMsg) noMsg.classList.add('d-none');
}

function escapeHtml(str) {
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/\n/g, '<br>');
}

document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('aiChatInput');
    if (input) {
        input.addEventListener('keydown', e => {
            if (e.key === 'Enter') sendMessage();
        });
    }
});

function appendProductCards(products) {
    const wrap = document.getElementById('aiChatMessages');
    const tmpl = document.getElementById('chatProductCard');
    if (!wrap || !tmpl) return;

    const container = document.createElement('div');
    container.className = 'ai-product-list';

    products.forEach(p => {
        const card = tmpl.content.cloneNode(true).querySelector('.ai-product-card');

        const imgEl = card.querySelector('.ai-product-img');
        if (p.imageUrl) {
            imgEl.style.backgroundImage = `url(${p.imageUrl})`;
        } else {
            imgEl.innerHTML = '<i class="bi bi-cake2"></i>';
            imgEl.classList.add('ai-product-img--empty');
        }

        card.querySelector('.ai-product-name').textContent = p.name;
        card.querySelector('.ai-product-price').textContent = `${Math.round(p.price)} ₴`;
        card.querySelector('.ai-product-btn').onclick =
            () => addToCart(p.id, p.name, p.price, p.imageUrl ?? '');

        container.appendChild(card);
    });

    const wrapper = document.createElement('div');
    wrapper.className = 'ai-msg ai-msg--bot';
    wrapper.style.maxWidth = '95%';
    wrapper.appendChild(container);
    wrap.appendChild(wrapper);
    wrap.scrollTop = wrap.scrollHeight;

    const tmplReset = document.getElementById('chatResetBtn');
    if (tmplReset) {
        wrap.appendChild(tmplReset.content.cloneNode(true));
    }
    wrap.scrollTop = wrap.scrollHeight;
}

