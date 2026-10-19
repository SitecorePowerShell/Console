(function($, window, spe, undefined) {

    // SpeJobRecovery.js gave up reviving the poll of a running action script.
    // Reloading would throw away the list, so only tell the user; the job is
    // re-attached in a Runner by the next shell page that loads.
    window.speJobWatchdogLost = function () {
        alert("Lost contact with the running script. It will reopen in a script runner the next time you load a Sitecore page.");
    };

    spe.updateStatusBarCounters = function (itemCount, currentPage, pageCount) {
        $("#ItemCount").text(itemCount);
        $("#CurrentPage").text(currentPage);
        $("#PageCount").text(pageCount);
    }

    // The ribbon renders the filter box with inline onchange/onkeydown handlers
    // that post pslv:filter through scForm.postEvent without cancelling the
    // key's default action. The filter box is the only text field on the page,
    // so Enter also triggers the browser's implicit form submit, which reloads
    // the page in its initial state (ribbon reset, filter text gone).
    // Sheer re-renders the ribbon after every selection change, so stripping
    // the inline handlers once at DOM ready is not enough: handle Enter here,
    // in the capture phase, so it runs before the inline handlers and no matter
    // how often the ribbon is re-rendered.
    document.addEventListener("keydown", function (event) {
        if (event.target.id !== "Input_Filter" || event.key !== "Enter") {
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        window.scForm.postEvent(event.target, event, "pslv:filter");
    }, true);

    // Keep the inline onchange from posting the filter when the box loses focus.
    document.addEventListener("change", function (event) {
        if (event.target.id === "Input_Filter") {
            event.stopPropagation();
        }
    }, true);

    $(function () {
        window.parent.focus();
        window.focus();
    });

}(jQuery, window, window.spe = window.spe || {}));
