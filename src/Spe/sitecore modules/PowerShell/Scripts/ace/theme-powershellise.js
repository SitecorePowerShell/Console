ace.define(
  "ace/theme/powershellise",
  ["require", "exports", "module", "ace/lib/dom"],
  function (require, exports, module) {
    exports.isDark = false;
    exports.cssText =
      ".ace-powershell-ise .ace_keyword {\
color: #00008B;\
}\
.ace-powershell-ise .ace_identifier {\
color: #8a2be2;\
}\
.ace-powershell-ise .ace_attribute {\
color: #00BFFF;\
}\
.ace-powershell-ise .ace_comment {\
color: #006400;\
}\
.ace-powershell-ise .ace_comment.ace_doc.ace_tag {\
color: #006400;\
font-weight: bold;\
}\
.ace-powershell-ise .ace_constant.ace_numeric {\
color: #800080;\
}\
.ace-powershell-ise .ace_constant.ace_language.ace_boolean {\
color: #0000FF;\
}\
.ace-powershell-ise .ace_support.ace_function {\
color: #0000FF;\
}\
.ace-powershell-ise .ace_support.ace_parameter {\
color: #000080;\
}\
.ace-powershell-ise .ace_type {\
color: #008080;\
}\
.ace-powershell-ise .ace_operator {\
color: #A9A9A9;\
}\
.ace-powershell-ise .ace_parameter {\
color: #000080;\
}\
.ace-powershell-ise .ace_member {\
color: #000000;\
}\
.ace-powershell-ise .ace_string {\
color: #8B0000;\
}\
.ace-powershell-ise .ace_variable.ace_instance, .ace-powershell-ise .ace_constant.ace_language {\
color: #FF4500;\
}\
.ace-powershell-ise .ace_variable.ace_braced {\
color: #FF4500;\
}\
.ace-powershell-ise .ace_constant.ace_language.ace_escape {\
color: #EE82EE;\
}\
.ace-powershell-ise .ace_punctuation {\
color: #808080;\
}\
.ace-powershell-ise .ace_paren {\
color: #808080;\
}\
.ace-powershell-ise {\
background-color: #FFFFFF;\
}\
.ace-powershell-ise .ace_cursor {\
border-left: 2px solid #000000;\
}\
.ace-powershell-ise .ace_overwrite-cursors .ace_cursor {\
border-left: 0px;\
border-bottom: 1px solid #000000;\
}\
.ace-powershell-ise .ace_gutter {\
background: #f0f0f0;\
color: #2B91AF;\
}\
.ace-powershell-ise .ace_marker-layer .ace_active-line {\
background: #f0f0f0;\
}\
.ace-powershell-ise .ace_marker-layer .ace_selection {\
background: #B5D5FF\
}\
.ace-powershell-ise .ace_marker-layer .ace_bracket {\
margin: -1px 0 0 -1px;\
border: 1px solid #C0C0C0;\
}\
.ace-powershell-ise .ace_indent-guide {\
background: url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAACCAYAAACZgbYnAAAAE0lEQVQImWP4////f4bLly//BwAmVgd1/w11/gAAAABJRU5ErkJggg==) right repeat-y;\
}\
.ace-powershell-ise .ace_fold-widget {\
color: #C0C0C0;\
}";

    exports.cssClass = "ace-powershell-ise";

    var dom = require("../lib/dom");
    dom.importCssString(exports.cssText, exports.cssClass);
  },
);
