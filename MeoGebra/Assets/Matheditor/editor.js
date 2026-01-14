(function () {
    const mathfield = document.getElementById('mf');
    const errorLine = document.getElementById('math-error');
    let isSetting = false;

    function post(type, text) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ type, text });
        }
    }

    function getLatex() {
        if (!mathfield) {
            return '';
        }
        if (typeof mathfield.getValue === 'function') {
            return mathfield.getValue() || '';
        }
        return mathfield.value || '';
    }

    function setLatex(latex) {
        if (!mathfield) {
            return;
        }
        isSetting = true;
        if (typeof mathfield.setValue === 'function') {
            mathfield.setValue(latex || '', { suppressChangeNotifications: true });
        } else {
            mathfield.value = latex || '';
        }
        isSetting = false;
    }

    function onInput() {
        if (isSetting) {
            return;
        }
        const latex = getLatex().trim();
        errorLine.textContent = '';
        post('change', latex);
    }

    if (!mathfield) {
        if (errorLine) {
            errorLine.textContent = 'MathLive failed to initialize.';
        }
        return;
    }

    mathfield.addEventListener('input', onInput);
    mathfield.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' && event.ctrlKey) {
            event.preventDefault();
            const latex = getLatex().trim();
            post('commit', latex);
        }
    });

    window.editor = {
        setLatex(latex) {
            setLatex(latex || '');
        }
    };
})();
