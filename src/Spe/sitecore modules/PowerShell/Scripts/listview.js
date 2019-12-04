(function($, window, spe, undefined) {

    spe.updateStatusBarCounters = function (itemCount, currentPage, pageCount) {
        $("#ItemCount").text(itemCount);
        $("#CurrentPage").text(currentPage);
        $("#PageCount").text(pageCount);
    }

    $(function () {
        window.parent.focus();
        window.focus();
        $("#Input_Filter").removeAttr('onchange').removeAttr('onkeydown');
        $("#Input_Filter").keypress(function(event) {
            if (event.which === 13) {
                event.preventDefault();
                window.scForm.postEvent(this, event, "pslv:filter");
            };
        });
    });

}(jQuery, window, window.spe = window.spe || {}));
