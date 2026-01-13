(function () {
    const input = document.getElementById('math-input');
    const preview = document.getElementById('math-preview');

    function escapeHtml(text) {
        return text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function renderMath(text) {
        let html = escapeHtml(text);
        html = html.replace(/\^\{([^}]+)\}/g, '<sup>$1</sup>');
        html = html.replace(/_\{([^}]+)\}/g, '<sub>$1</sub>');
        html = html.replace(/\^([0-9a-zA-Z]+)/g, '<sup>$1</sup>');
        html = html.replace(/_([0-9a-zA-Z]+)/g, '<sub>$1</sub>');
        return html;
    }

    function post(type, text) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ type, text });
        }
    }

    function updatePreview() {
        const text = input.innerText.trim();
        preview.innerHTML = renderMath(text || '');
        post('preview', text);
    }

    input.addEventListener('input', () => {
        updatePreview();
    });

    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            const text = input.innerText.trim();
            post('commit', text);
        }
    });

    input.addEventListener('blur', () => {
        const text = input.innerText.trim();
        post('commit', text);
    });

    window.editor = {
        setText(text) {
            input.innerText = text || '';
            updatePreview();
        }
    };

    updatePreview();
})();