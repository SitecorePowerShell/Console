function OnTabSelect() {
    if (window.Flexie) Flexie.updateInstance();
}

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

    const stateControlElements = $("*").filter(function () {
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
                    if (hideOnValue) {
                        if (controlValue === hideOnValue) {
                            $(e).hide();
                        } else {
                            $(e).show();
                        }
                    } else {
                        const showOnValue = e.getAttribute("data-show-on-value");
                        if (showOnValue) {
                            if (controlValue === showOnValue) {
                                $(e).show();
                            } else {
                                $(e).hide();
                            }
                        }
                    }

                }
            }
        });

        $(element).trigger("change");
    });

    document.observe("keypress", function (event) {
        if (event.keyCode == 13) {
            var ctl = event.target;
            if (ctl != null) {
                if (ctl.tagName == "TEXTAREA") {
                    event.stopPropagation();
                }
                if (ctl.tagName == "INPUT") {
                    if (ctl.onsubmit) {
                        if (ctl.onsubmit.toString().indexOf("return false;") >= 0) {
                            return;
                        }
                    }
                }
            }

            var ok = $("OKButton");

            if (ok != null) {
                ok.click();
            }
        }

        if (event.keyCode == 27) {
            var ok = $("CancelButton");

            if (ok != null) {
                ok.click();
            }
        }
    });
});