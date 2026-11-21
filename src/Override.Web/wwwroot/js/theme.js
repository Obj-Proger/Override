// Sets the theme before the page is drawn, so it never flashes in the wrong colors.
// The choice is kept in this browser only; without a choice the site follows the system.
(function () {
    var key = 'override.theme';
    var media = window.matchMedia('(prefers-color-scheme: dark)');

    function stored() {
        try {
            return window.localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    }

    function apply() {
        document.documentElement.dataset.theme = stored() || (media.matches ? 'dark' : 'light');
    }

    window.overrideTheme = {
        toggle: function () {
            var next = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
            try {
                window.localStorage.setItem(key, next);
            } catch (e) {
                // Storage can be disabled; the theme still changes for this visit.
                document.documentElement.dataset.theme = next;
                return;
            }
            apply();
        }
    };

    media.addEventListener('change', apply);
    apply();
})();
