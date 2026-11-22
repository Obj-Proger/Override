// Copies text to the clipboard. Returns false when the browser does not allow it.
window.overrideClipboard = {
    copy: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (e) {
            return false;
        }
    }
};
