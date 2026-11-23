// iPhone and iPad zoom into a focused field whose text is smaller than 16px, and the code editor uses smaller text.
// maximum-scale stops that zoom; Safari still lets the reader zoom with two fingers. Other browsers would block
// that zoom with it, so it is set only on Apple devices.
(function () {
    var apple = /iPhone|iPad|iPod/.test(navigator.userAgent) ||
        (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    var meta = document.querySelector('meta[name="viewport"]');
    if (apple && meta) {
        meta.setAttribute('content', meta.getAttribute('content') + ', maximum-scale=1');
    }
})();
