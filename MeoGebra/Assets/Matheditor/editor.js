(function () {
    const input = document.getElementById('latexInput');
    const preview = document.getElementById('preview');
    const errorLine = document.getElementById('math-error');

    function post(type, text) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ type, text });
        }
    }

    function renderPreview(latex) {
        if (!window.katex) {
            preview.textContent = latex || '';
            errorLine.textContent = 'KaTeX failed to load.';
            return;
        }

        errorLine.textContent = '';
        const safeLatex = latex || '\\quad';
        try {
            window.katex.render(safeLatex, preview, {
                throwOnError: true,
                displayMode: true
            });
        } catch (error) {
            errorLine.textContent = error && error.message ? error.message : 'Unable to render LaTeX.';
            window.katex.render(safeLatex, preview, {
                throwOnError: false,
                displayMode: true
            });
        }
    }

    function updatePreview() {
        const latex = input.value.trim();
        renderPreview(latex);
        post('change', latex);
    }

    input.addEventListener('input', () => {
        updatePreview();
    });

    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' && event.ctrlKey) {
            event.preventDefault();
            const latex = input.value.trim();
            post('commit', latex);
        }
    });

    window.editor = {
        setLatex(latex) {
            input.value = latex || '';
            renderPreview(input.value.trim());
        }
    };

    renderPreview(input.value.trim());
})();