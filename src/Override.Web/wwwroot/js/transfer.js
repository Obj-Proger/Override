// Carries progress between devices in a link. The data is compressed into the part of the address after #,
// which browsers never send to a server: it goes only from one browser to another through the person.
window.overrideTransfer = {
    pack: async function (text) {
        const stream = new Blob([text]).stream().pipeThrough(new CompressionStream('deflate-raw'));
        const bytes = new Uint8Array(await new Response(stream).arrayBuffer());
        let binary = '';
        for (let i = 0; i < bytes.length; i += 0x8000) {
            binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
        }
        return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    },

    unpack: async function (code) {
        const padded = code.replace(/-/g, '+').replace(/_/g, '/') + '='.repeat((4 - code.length % 4) % 4);
        const binary = atob(padded);
        const bytes = Uint8Array.from(binary, function (c) { return c.charCodeAt(0); });
        const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('deflate-raw'));
        return await new Response(stream).text();
    },

    // The code from an opened link, removed from the address so that a reload does not offer it again.
    takeFromAddress: function () {
        const match = location.hash.match(/^#import=(.+)$/);
        if (!match) {
            return null;
        }
        history.replaceState(null, '', location.pathname + location.search);
        return match[1];
    },

    canShare: function () {
        return typeof navigator.share === 'function';
    },

    share: async function (url) {
        try {
            await navigator.share({ title: 'Прогресс Override', url: url });
            return true;
        } catch {
            return false;
        }
    }
};
