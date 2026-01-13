(function () {
    const input = document.getElementById('math-input');
    const preview = document.getElementById('math-preview');

    function post(type, text) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ type, text });
        }
    }

    function renderPreview(text) {
        if (window.katex) {
            try {
                window.katex.render(text || '\\quad', preview, {
                    throwOnError: false,
                    displayMode: true
                });
                return;
            } catch (error) {
            }
        }
        preview.textContent = text || '';
    }

    function updatePreview() {
        const text = input.value.trim();
        renderPreview(text);
        post('preview', text);
    }

    input.addEventListener('input', () => {
        updatePreview();
    });

    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' && event.ctrlKey) {
            event.preventDefault();
            const text = input.value.trim();
            post('commit', text);
        }
    });

    input.addEventListener('blur', () => {
        const text = input.value.trim();
        post('commit', text);
    });

    window.editor = {
        setLatex(text) {
            input.value = text || '';
            updatePreview();
        }
    };

    updatePreview();
})();