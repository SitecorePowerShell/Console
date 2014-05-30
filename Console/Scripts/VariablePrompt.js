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
	ResizeDialogControls();
    });
    $(window).resize(function() {
	ResizeDialogControls();
    });
});

function ResizeDialogControls() {
    var chromeHeight = $ise(".scWizardHeader").height() +$ise(".scWizardButtons").height() + 20;
    var windowW = $ise(window).width();
    var windowH = $ise(window).height();
    var tabsWidth = (windowW - 26) + "px";
    var tabsPage = (windowW - 60) + "px";
    var controlWidth = (windowW - 34 - tabsOffset) + "px";
    var windowWidth = (windowW - 14) + "px";
    var tabsHeight = (windowH - chromeHeight) + "px";
    console.log("windowW:"+windowW + "; windowH:"+windowH+"; controlWidth:"+controlWidth+"; windowWidth:"+windowWidth+"; tabsHeight:"+tabsHeight+ "; chromeHeight:"+chromeHeight);

    $ise("#ValuePanel").css({ width: windowWidth });
    $ise("#Tabstrip").css({ height: tabsHeight, width: tabsWidth});
    $ise(".treePicker").css({ width: controlWidth });
    $ise(".scContentControlChecklist").css({ width: (windowW - 58 - tabsOffset) + "px" });
    $ise(".scRadioGroup").css({ width: (windowW - 58 - tabsOffset) + "px" });
    $ise(".textEdit").css({ width: controlWidth });        
    if ($ise(".scUserPickerButton").length > 0) {
        controlWidth = (windowW - $ise(".scUserPickerButton").outerWidth() - 40 - tabsOffset) + "px";
        $ise(".scUserPickerEdit").css({ width: controlWidth });
    }
}
