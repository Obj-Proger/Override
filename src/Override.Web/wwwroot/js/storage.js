// Local storage for progress. Storage can be disabled or full: then the site keeps working without saving.
window.overrideStorage = {
    get: function (key) {
        try {
            return window.localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    },
    set: function (key, value) {
        try {
            window.localStorage.setItem(key, value);
            return true;
        } catch (e) {
            return false;
        }
    }
};
