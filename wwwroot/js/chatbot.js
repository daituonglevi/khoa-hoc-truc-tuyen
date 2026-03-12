(function () {
    const root = document.getElementById('ai-chatbot-root');
    if (!root) return;

    const toggleBtn = document.getElementById('ai-chatbot-toggle');
    const panel = document.getElementById('ai-chatbot-panel');
    const closeBtn = document.getElementById('ai-chatbot-close');
    const clearBtn = document.getElementById('ai-chatbot-clear');
    const form = document.getElementById('ai-chatbot-form');
    const input = document.getElementById('ai-chatbot-input');
    const messages = document.getElementById('ai-chatbot-messages');

    function setOpen(open) {
        panel.classList.toggle('open', open);
        toggleBtn.classList.toggle('d-none', open);
        if (open) input.focus();
    }

    function appendMessage(content, role) {
        const item = document.createElement('div');
        item.className = 'ai-chatbot-msg ' + (role === 'user' ? 'user' : 'bot');
        item.textContent = content;
        messages.appendChild(item);
        messages.scrollTop = messages.scrollHeight;
    }

    async function ask(message) {
        const res = await fetch('/api/chatbot/ask', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message })
        });

        if (!res.ok) {
            const payload = await res.json().catch(() => ({ error: 'Có lỗi xảy ra' }));
            throw new Error(payload.error || 'Có lỗi xảy ra');
        }

        return res.json();
    }

    async function clearHistory() {
        await fetch('/api/chatbot/clear', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
    }

    toggleBtn.addEventListener('click', function () {
        setOpen(true);
    });

    closeBtn.addEventListener('click', function () {
        setOpen(false);
    });

    clearBtn.addEventListener('click', async function () {
        messages.innerHTML = '';
        appendMessage('Mình đã xóa lịch sử hội thoại trong phiên hiện tại.', 'bot');
        try {
            await clearHistory();
        } catch {
        }
    });

    form.addEventListener('submit', async function (event) {
        event.preventDefault();

        const message = input.value.trim();
        if (!message) return;

        input.value = '';
        appendMessage(message, 'user');

        const typing = document.createElement('div');
        typing.className = 'ai-chatbot-msg bot typing';
        typing.textContent = 'Đang trả lời...';
        messages.appendChild(typing);
        messages.scrollTop = messages.scrollHeight;

        try {
            const result = await ask(message);
            typing.remove();
            appendMessage(result.answer || 'Hiện chưa có câu trả lời phù hợp.', 'bot');
        } catch (error) {
            typing.remove();
            appendMessage(error.message || 'Chatbot tạm thời không khả dụng.', 'bot');
        }
    });

    appendMessage('Xin chào! Mình là trợ lý AI của LMS VJU. Bạn cần hỗ trợ khóa học nào?', 'bot');
})();
