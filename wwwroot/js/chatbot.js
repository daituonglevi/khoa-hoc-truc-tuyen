(function () {
    const root = document.getElementById('ai-chatbot-root');
    if (!root) return;

    const toggleBtn = document.getElementById('ai-chatbot-toggle');
    const panel = document.getElementById('ai-chatbot-panel');
    const closeBtn = document.getElementById('ai-chatbot-close');
    const clearBtn = document.getElementById('ai-chatbot-clear');
    const form = document.getElementById('ai-chatbot-form');
    const input = document.getElementById('ai-chatbot-input');
    const sendBtn = document.getElementById('ai-chatbot-send');
    const imageInput = document.getElementById('ai-chatbot-image');
    const imageName = document.getElementById('ai-chatbot-image-name');
    const imagePreview = document.getElementById('ai-chatbot-image-preview');
    const imagePreviewImg = document.getElementById('ai-chatbot-image-preview-img');
    const imagePreviewName = document.getElementById('ai-chatbot-image-preview-name');
    const imageRemoveBtn = document.getElementById('ai-chatbot-image-remove');
    const messages = document.getElementById('ai-chatbot-messages');
    let inputPreviewUrl = null;

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

    function appendImageMessage(imageSrc, role, altText) {
        const item = document.createElement('div');
        item.className = 'ai-chatbot-msg ai-chatbot-msg-image ' + (role === 'user' ? 'user' : 'bot');

        const image = document.createElement('img');
        image.src = imageSrc;
        image.alt = altText || 'Ảnh đã gửi';
        image.loading = 'lazy';

        item.appendChild(image);
        messages.appendChild(item);
        messages.scrollTop = messages.scrollHeight;

        return item;
    }

    function clearInputImagePreview(revokeObjectUrl = true) {
        if (inputPreviewUrl && revokeObjectUrl) {
            URL.revokeObjectURL(inputPreviewUrl);
        }

        inputPreviewUrl = null;

        imagePreview.classList.add('d-none');
        imagePreviewImg.removeAttribute('src');
        imagePreviewName.textContent = '';
    }

    function showInputImagePreview(file) {
        clearInputImagePreview();
        inputPreviewUrl = URL.createObjectURL(file);
        imagePreviewImg.src = inputPreviewUrl;
        imagePreviewName.textContent = file.name;
        imagePreview.classList.remove('d-none');
    }

    function setSelectedImage(file) {
        if (!file) {
            imageInput.value = '';
            imageName.textContent = '';
            clearInputImagePreview();
            return;
        }

        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        imageInput.files = dataTransfer.files;
        imageName.textContent = 'Đã chọn ảnh: ' + file.name;
        showInputImagePreview(file);
    }

    function extractImageFromClipboardEvent(event) {
        const clipboardItems = event.clipboardData && event.clipboardData.items
            ? event.clipboardData.items
            : null;

        if (!clipboardItems) {
            return null;
        }

        for (const item of clipboardItems) {
            if (item.kind === 'file' && item.type.startsWith('image/')) {
                const file = item.getAsFile();
                if (file) {
                    return file;
                }
            }
        }

        return null;
    }

    async function ask(message, imageFile) {
        const formData = new FormData();
        formData.append('message', message || '');
        formData.append('currentUrl', window.location.href || '');
        formData.append('currentPageTitle', document.title || '');
        if (imageFile) {
            formData.append('image', imageFile);
        }

        const res = await fetch('/api/chatbot/ask', {
            method: 'POST',
            body: formData
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

    async function loadHistory() {
        const res = await fetch('/api/chatbot/history', {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        });

        if (!res.ok) {
            throw new Error('Không thể tải lịch sử chat');
        }

        const payload = await res.json();
        return Array.isArray(payload.history) ? payload.history : [];
    }

    toggleBtn.addEventListener('click', function () {
        setOpen(true);
    });

    closeBtn.addEventListener('click', function () {
        setOpen(false);
    });

    imageInput.addEventListener('change', function () {
        const file = imageInput.files && imageInput.files[0];
        if (!file) {
            setSelectedImage(null);
            return;
        }

        setSelectedImage(file);
    });

    input.addEventListener('paste', function (event) {
        const imageFile = extractImageFromClipboardEvent(event);
        if (!imageFile) {
            return;
        }

        event.preventDefault();
        const extension = (imageFile.type.split('/')[1] || 'png').toLowerCase();
        const pastedFile = new File([imageFile], 'pasted-image.' + extension, { type: imageFile.type });
        setSelectedImage(pastedFile);
    });

    clearBtn.addEventListener('click', async function () {
        messages.innerHTML = '';
        setSelectedImage(null);
        appendMessage('Mình đã xóa lịch sử hội thoại trong phiên hiện tại.', 'bot');
        try {
            await clearHistory();
        } catch {
        }
    });

    imageRemoveBtn.addEventListener('click', function () {
        setSelectedImage(null);
        input.focus();
    });

    async function handleSend() {
        if (!form) return;

        const message = input.value.trim();
        const imageFile = imageInput.files && imageInput.files[0] ? imageInput.files[0] : null;
        if (!message && !imageFile) return;

        input.value = '';

        const userText = message || 'Người dùng đã gửi ảnh để hỏi.';
        appendMessage(userText, 'user');
        if (imageFile) {
            const sentImageUrl = URL.createObjectURL(imageFile);
            appendImageMessage(sentImageUrl, 'user', imageFile.name);
        }
        setSelectedImage(null);

        const typing = document.createElement('div');
        typing.className = 'ai-chatbot-msg bot typing';
        typing.textContent = 'Đang trả lời...';
        messages.appendChild(typing);
        messages.scrollTop = messages.scrollHeight;

        try {
            const result = await ask(message, imageFile);
            typing.remove();
            appendMessage(result.answer || 'Hiện chưa có câu trả lời phù hợp.', 'bot');
        } catch (error) {
            typing.remove();
            appendMessage(error.message || 'Chatbot tạm thời không khả dụng.', 'bot');
        }
    }

    if (sendBtn) {
        sendBtn.addEventListener('click', function () {
            handleSend();
        });
    }

    input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            handleSend();
        }
    });

    (async function initializeChat() {
        try {
            const history = await loadHistory();
            if (history.length > 0) {
                for (const item of history) {
                    const role = item.role === 'user' ? 'user' : 'bot';
                    const content = typeof item.content === 'string' ? item.content : '';
                    if (content) {
                        appendMessage(content, role);
                    }
                    const imageUrl = typeof item.imageUrl === 'string' ? item.imageUrl : '';
                    if (imageUrl) {
                        appendImageMessage(imageUrl, role, 'Ảnh trong lịch sử chat');
                    }
                }
                return;
            }
        } catch {
        }

        appendMessage('Xin chào! Bạn có thể nhập câu hỏi hoặc tải ảnh để mình hỗ trợ.', 'bot');
    })();
})();
