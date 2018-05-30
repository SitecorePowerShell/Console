jQuery(document).ready(function ($) {
    $("#Copyright").each(function () { // Notice the .each() loop, discussed below
        const currentYear = (new Date()).getFullYear();
        const greetings = "Copyright &copy; 2010-" + currentYear + " Adam Najmanowicz, Michael West. All rights Reserved.\r\n";

        $(this).qtip({
            content: {
                text: greetings,
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


    const controlElements = $("*").filter(function () {
        return $(this).data("group-id") !== undefined;
    });

    const stateControlElements = $("*").filter(function() {
        return $(this).data("parent-group-id") !== undefined;
    });

    $.each(controlElements, function (index, element) {
        $(element).on("change", function () {
            let controlValue;
            if (element.type === "checkbox") {
                controlValue = this.checked ? "1" : "0";
            } else if (element.type === "select-one") {
                controlValue = $(this).find(":selected").val();
            }
            const groupId = element.getAttribute("data-group-id");
            for (let i = 0; i < stateControlElements.length; i++) {
                const e = stateControlElements[i];
                if (e.hasAttribute("data-parent-group-id") && e.getAttribute("data-parent-group-id") === groupId) {
                    const hideOnValue = e.getAttribute("data-hide-on-value");
                    if (controlValue === hideOnValue) {
                        $(e).hide();
                    } else {
                        $(e).show();
                    }
                    //$(e).prop("disabled", function(j, v) { return !v; });
                }
            }
        });

        $(element).trigger("change");
    });
});