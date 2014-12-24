jQuery(document).ready(function ($) {

    $('#Copyright').each(function() { // Notice the .each() loop, discussed below
        $(this).qtip({
            content: {
                text: "Copyright (c) 2010-2014 <a href='http://www.cognifide.com' target='_blank'>Cognifide Limited</a> &amp; <a href='http://blog.najmanowicz.com/' target='_blank'>Adam Najmanowicz</a>.",
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
    });
});
