
jQuery(document).ready(function($) {
    window.parent.focus();
    window.focus();

    $('#CodeEditor').on('keyup mousedown', function() {
        var position = codeeditor.getCursorPosition();
        posx.text(position.column);
        posy.text((position.row + 1));
    });

    $('#Input_Filter').keypress(function(event) {
        if (event.which === 13) {
            event.preventDefault();
            scForm.postRequest('', '', '', 'pslv:filter');
        }
        ;
    });
    function barWidth() {
        var barWidth = $('.progressBar').width();
        $('.progressFillText').css('width',barWidth);
    }

    barWidth();
    window.onresize = function() {
        barWidth();
    }
});

function updateStatusBarCounters(itemCount, currentPage, pageCount) {
    $ise('#ItemCount').text(itemCount);
    $ise('#CurrentPage').text(currentPage);
    $ise('#PageCount').text(pageCount);
}

