
// Configura el endpoint del backend (MVC o API directa)
const CHAT_ENDPOINT = '/assistant/chat'; // cámbialo si usas otra ruta

function renderMarkdown(text) {
    // Conversión muy básica: títulos, negritas, listas y saltos de línea.
    let html = text || '';
    // Escapar básico para evitar HTML injection
    html = html.replace(/[&<>]/g, s => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[s]));
    // Negritas **texto**
    html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    // Títulos # ##
    html = html.replace(/^###\s(.+)$/gm, '<h3>$1</h3>')
        .replace(/^##\s(.+)$/gm, '<h2>$1</h2>')
        .replace(/^#\s(.+)$/gm, '<h1>$1</h1>');
    // Listas simples
    html = html.replace(/^\s*-\s(.+)$/gm, '<li>$1</li>')
        .replace(/(<li>.+<\/li>)(\n<li>)/g, '$1$2'); // merge
    html = html.replace(/(?:<li>.+<\/li>\n?)+/g, m => `<ul>${m}</ul>`);
    // Saltos de línea
    html = html.replace(/\n/g, '<br/>');
    return html;
}

function addMessage(cls, html) {
    const log = document.getElementById('chat-log');
    const div = document.createElement('div');
    div.className = 'msg ' + cls;
    const bubble = document.createElement('div');
    bubble.className = 'bubble';
    bubble.innerHTML = html;
    div.appendChild(bubble);
    log.appendChild(div);
    log.scrollTop = log.scrollHeight;
    return div;
}

function setTyping(on) {
    const log = document.getElementById('chat-log');
    let tip = log.querySelector('.typing');
    if (on) {
        if (!tip) {
            const holder = document.createElement('div');
            holder.className = 'msg assistant';
            const t = document.createElement('div');
            t.className = 'bubble';
            t.innerHTML = '<span class="typing" aria-label="Escribiendo..."></span>';
            holder.appendChild(t);
            log.appendChild(holder);
            log.scrollTop = log.scrollHeight;
        }
    } else if (tip) {
        tip.closest('.msg.assistant')?.remove();
    }
}

async function sendMessage(text) {
    const input = document.getElementById('chat-text');
    const btn = document.getElementById('chat-send');

    // Añade el mensaje del usuario
    addMessage('user', renderMarkdown(text));

    // Estado tipando + bloqueo de botón
    setTyping(true);
    btn.disabled = true;

    // Petición al backend con timeout
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 30000); // 30s

    try {
        const resp = await fetch(CHAT_ENDPOINT, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: text }),
            signal: controller.signal
        });

        let data;
        try { data = await resp.json(); } catch { data = null; }

        if (!resp.ok) {
            const msg = (data && data.error) ? data.error : `Error ${resp.status}`;
            addMessage('assistant', renderMarkdown(msg));
        } else {
            const reply = (data && (data.reply || data.answer)) || 'Sin respuesta.';
            addMessage('assistant', renderMarkdown(reply));
        }
    } catch (e) {
        const msg = e.name === 'AbortError' ? 'Tiempo de espera agotado.' : `Error de red: ${e}`;
        addMessage('assistant', renderMarkdown(msg));
    } finally {
        clearTimeout(timer);
        setTyping(false);
        btn.disabled = false;
        input.focus();
    }
}

(function init() {
    const btn = document.getElementById('chat-send');
    const input = document.getElementById('chat-text');

    const fire = () => {
        const text = input.value.trim();
        if (!text) return;
        input.value = '';
        sendMessage(text);
    };

    btn.addEventListener('click', fire);
    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            fire();
        }
    });
})();

(function () {
    const chat = document.getElementById("assistant-chat");
    const toggle = document.getElementById("chat-toggle");

    toggle.addEventListener("click", () => {
        const minimized = chat.classList.toggle("chat-minimized");
        toggle.textContent = minimized ? "+" : "–";
    });
})();
