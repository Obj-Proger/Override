// Shows what went wrong in the error panel, so that a failure can be described without developer tools,
// for example on a phone. The text comes from the browser console: Blazor writes its errors there.
(function () {
    const lines = [];

    function text(value) {
        if (value instanceof Error) {
            return value.stack || value.message;
        }
        if (typeof value === 'string') {
            return value;
        }
        try {
            return JSON.stringify(value);
        } catch (e) {
            return String(value);
        }
    }

    function remember(message) {
        lines.push(message);
        if (lines.length > 5) {
            lines.shift();
        }
        const details = document.getElementById('error-details');
        if (details) {
            details.textContent = lines.join('\n\n');
        }
    }

    const original = console.error;
    console.error = function () {
        remember(Array.prototype.map.call(arguments, text).join(' '));
        return original.apply(console, arguments);
    };

    window.addEventListener('error', function (event) {
        remember(event.error ? text(event.error) : event.message + ' (' + event.filename + ':' + event.lineno + ')');
    });

    window.addEventListener('unhandledrejection', function (event) {
        remember(text(event.reason));
    });
})();
