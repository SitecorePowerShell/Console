(function ($, window, cognifide, c3, undefined) {
    "use strict";
    $(function () {
        const availabeReportsUrl = "/-/script/v2/master/get-availablereport?depth=5&compress=true";
        $.getJSON(availabeReportsUrl, function (availableData) {
            $.each(availableData.Results,
                function (index, name) {
                    const url = `/-/script/v2/master/${name}?depth=5&compress=true`;
                    $("#chart").append(`<div id="chart-${index}-container"><img class="spinner" src="/sitecore/shell/Themes/Standard/Images/sc-spinner32.gif" /></div>`);
                    $.getJSON(url, function (chartData) {
                        if (chartData.Results.title) {
                            $(`#chart-${index}-container`).append(`<h2 class="title">${chartData.Results.title}</h2>`);
                        }
                        $(`#chart-${index}-container`).append(`<div id="chart-${index}" />`);
                        $(`#chart-${index}-container .spinner`).hide();
                        chartData.Results.bindto = `#chart-${index}`;
                        c3.generate(chartData.Results);
                    });
                });
        }).fail(function () {
            $("#chart").append('<div class="no-data"><span>No report data available.</span><ul><li>Is the remoting service enabled?</li><li>Are the reporting modules installed and enabled?</li></ul></div>');
        });
    });
}(jQuery, window, window.cognifide = window.cognifide || {}, c3));