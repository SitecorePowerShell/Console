jQuery(document).ready(function ($) {
    $('#Copyright').each(function() { // Notice the .each() loop, discussed below
        $(this).qtip({
            content: {
                text: "Copyright (c) 2010-2013 <a href='http://www.cognifide.com' target='_blank'>Cognifide Limited</a> &amp; <a href='http://blog.najmanowicz.com/' target='_blank'>Adam Najmanowicz</a>.",
                title: 'Sitecore PowerShell Extensions'
            },
            position: {
                my: 'bottom left',
                at: 'top center'
            },
            style: {
                width: 355,
                "max-width" : 355
            },
            hide: {
                event: false,
                inactive: 3000
            }
        });
        var controlWidth = ($(window).width() - 24) + "px";
        $(".treePicker").css({ width: controlWidth });
        $(".textEdit").css({ width: controlWidth });        
        if ($(".scUserPickerButton").length > 0) {
            controlWidth = ($(window).width() - $(".scUserPickerButton").outerWidth() - 30) + "px";
            $(".scUserPickerEdit").css({ width: controlWidth });
        }
    });
    $(window).resize(function() {
        var controlWidth = ($(window).width() - 24) + "px";
        $(".treePicker").css({ width: controlWidth });
        $(".textEdit").css({ width: controlWidth });
        if ($(".scUserPickerButton").length > 0) {
            controlWidth = ($(window).width() - $(".scUserPickerButton").outerWidth() - 30) + "px";
            $(".scUserPickerEdit").css({ width: controlWidth });
        }
    });
});

function RefreshPickerSize() {
    if ($ise(".scUserPickerButton").length > 0) {
        controlWidth = ($ise(window).width() - $ise(".scUserPickerButton")[0].offsetWidth - 32) + "px";
        $ise(".scUserPickerEdit").css({ width: controlWidth });
    }
}
