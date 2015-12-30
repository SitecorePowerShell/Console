(function($, window, cognifide, undefined) {

    cognifide.powershell.updateStatusBarCounters = function (itemCount, currentPage, pageCount) {
        $("#ItemCount").text(itemCount);
        $("#CurrentPage").text(currentPage);
        $("#PageCount").text(pageCount);
    }

    $(function () {
        window.parent.focus();
        window.focus();

        $("#Input_Filter").keypress(function(event) {
            if (event.which === 13) {
                event.preventDefault();
                window.scForm.postRequest("", "", "", "pslv:filter");
            };
        });
    });

}(jQuery, window, window.cognifide = window.cognifide || {}));