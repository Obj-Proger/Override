// Keyboard shortcuts of the site: Ctrl+K (or Cmd+K) and "/" open the search.
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
            }
        });
    },
    focus: function (element) {
        if (element) {
            element.focus();
        }
    }
};
