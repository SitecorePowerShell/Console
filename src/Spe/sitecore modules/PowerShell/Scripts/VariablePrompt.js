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

    const controlElements = $("[data-group-id]");
    const stateControlElements = $("[data-parent-group-id]");

    function normalizeCheckboxValue(value) {
        if (value === null || value === undefined) return value;
        var lower = value.toLowerCase();
        if (lower === "true" || lower === "yes") return "1";
        if (lower === "false" || lower === "no") return "0";
        return value;
    }

    function applyGroupVisibility(element) {
        let controlValue;
        const isCheckbox = element.type === "checkbox";
        if (isCheckbox) {
            controlValue = element.checked ? "1" : "0";
        } else if (element.type === "select-one") {
            controlValue = $(element).find(":selected").val();
        } else {
            const checkedRadio = $(element).find("input[type='radio']:checked");
            if (checkedRadio.length) {
                controlValue = checkedRadio.val();
            }
        }
        const groupId = element.getAttribute("data-group-id");
        for (let i = 0; i < stateControlElements.length; i++) {
            const e = stateControlElements[i];
            if (e.hasAttribute("data-parent-group-id") && e.getAttribute("data-parent-group-id") === groupId) {
                const hideOnValue = e.getAttribute("data-hide-on-value");
                if (hideOnValue) {
                    const normalizedHide = isCheckbox ? normalizeCheckboxValue(hideOnValue) : hideOnValue;
                    if (controlValue === normalizedHide) {
                        $(e).hide();
                    } else {
                        $(e).show();
                    }
                } else {
                    const showOnValue = e.getAttribute("data-show-on-value");
                    if (showOnValue) {
                        const normalizedShow = isCheckbox ? normalizeCheckboxValue(showOnValue) : showOnValue;
                        if (controlValue === normalizedShow) {
                            $(e).show();
                        } else {
                            $(e).hide();
                        }
                    }
                }

            }
        }
    }

    $.each(controlElements, function (index, element) {
        $(element).on("change", function () {
            applyGroupVisibility(element);
        });

        // Radio buttons: delegate change from inner inputs to the container
        $(element).find("input[type='radio']").on("change", function () {
            applyGroupVisibility(element);
        });

        applyGroupVisibility(element);
    });

    $('[data-maxlength]').each(function () {
        var $el = $(this);
        var max = parseInt($el.attr('data-maxlength'), 10);
        if (isNaN(max) || max <= 0) return;

        var $counter = $('<span class="varCharCounter">' + $el.val().length + ' / ' + max + '</span>');
        $el.after($counter);

        $el.on('input keyup', function () {
            var len = $el.val().length;
            $counter.text(len + ' / ' + max);
            $counter.toggleClass('varCharCounterOver', len > max);
        });
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