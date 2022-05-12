(function ($, window, spe, undefined) {
    function sanitize(string) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#x27;',
            "/": '&#x2F;',
        };
        const reg = /[&<>"'/]/ig;
        return string.replace(reg, (match) => (map[match]));
    }

    hasSketchyText = function (text) {
        var sanitizedText = sanitize(text);
        if (text && text !== sanitizedText) {
            console.log('[SPE] There seems to be an issue with data entered.');
            return true;
        }

        return false;
    };
   
    $(function () {
        $('input[type=text]').each(function (index, element) {
            element.onchange = (function (onchange) {
                return function (evt) {
                    evt = evt || event;

                    if (hasSketchyText(this.value)) {
                        this.value = '';
                        return;
                    }

                    if (onchange) {
                        onchange(evt);
                    }
                }
            })(element.onchange);
        });
    });
}(jQuery, window, window.spe = window.spe || {}));
