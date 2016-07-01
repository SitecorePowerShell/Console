jQuery(document).ready(function($) {
    $("#Copyright").each(function() { // Notice the .each() loop, discussed below
        $(this).qtip({
            content: {
                text: "Copyright &copy; 2010-2016 Adam Najmanowicz - Cognifide, Michael West. All rights Reserved.\r\n",
                title: "Sitecore PowerShell Extensions"
            },
            position: {
                my: "bottom left",
                at: "top center"
            },
            style: {
                width: 355,
                "max-width": 355
            },
            hide: {
                event: false,
                inactive: 3000
            }
        });
    });
});