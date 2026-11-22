// Keyboard shortcuts of the site: Ctrl+K (or Cmd+K) and "/" open the search, Escape closes dialogs and the drawer.
window.overrideShortcuts = {
    register: function (target) {
        document.addEventListener('keydown', function (event) {
            var typing = event.target instanceof HTMLElement &&
                (event.target.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(event.target.tagName));
            var search = ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') ||
                (event.key === '/' && !typing);

            if (search) {
                event.preventDefault();
                target.invokeMethodAsync('OpenSearch');
            } else if (event.key === 'Escape') {
                target.invokeMethodAsync('CloseOverlays');
            }
        });
    },
    focus: function (element) {
        if (element) {
            element.focus();
        }
    },
    // Moves the focus without scrolling and without the focus ring: the page has just been scrolled to the top.
    focusQuietly: function (element) {
        if (element && !element.contains(document.activeElement)) {
            element.focus({ preventScroll: true });
        }
    }
};
