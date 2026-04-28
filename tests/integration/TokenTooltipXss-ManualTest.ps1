# TokenTooltipXss-ManualTest.ps1
# Issue #1441 - Token tooltip innerHTML XSS via unencoded variable/property names
# Validates that PowerShellWebService.GetVariableValue HTML-encodes user-controlled
# strings before composing the tooltip HTML returned to token_tooltip.js.
#
# Run mode: manual. Open the ISE in a Sitecore CM that has the patched Spe.dll
# deployed, paste each block, click Execute, then HOVER the variable name in the
# script editor to trigger the tooltip.
#
# Pass criteria: every payload below renders as literal text. No formatting,
# no alert dialogs, no network requests, no element side effects.

# ----------------------------------------------------------------------------
# Test 1 - Property-name HTML rendering (PowerShellWebService.asmx.cs lines 264, 273)
# Hover $test1, expand the object preview.
# Before fix: the second key renders as bold text (<b> tag interpreted).
# After fix: literal text "<b>BOLD-RENDERED-AS-HTML</b>".
# ----------------------------------------------------------------------------
$test1 = [PSCustomObject]@{
    "NormalProp" = "hello"
    "<b>BOLD-RENDERED-AS-HTML</b>" = "check 1"
}
$test1

# ----------------------------------------------------------------------------
# Test 2 - Active XSS payload in property name (lines 264, 273)
# Hover $test2.
# Before fix: alert("XSS-1441-child") fires on tooltip render.
# After fix: literal <img ...> text, no alert.
# ----------------------------------------------------------------------------
$test2 = [PSCustomObject]@{
    "<img src=x onerror=alert('XSS-1441-child')>" = ""
}
$test2

# ----------------------------------------------------------------------------
# Test 3 - Nested property name (line 279)
# Hover $test3, the tooltip expands Outer and renders sub-children.
# Before fix: alert("XSS-1441-sub") fires.
# After fix: no alert, literal text.
# ----------------------------------------------------------------------------
$test3 = [PSCustomObject]@{
    Outer = [PSCustomObject]@{
        "<img src=x onerror=alert('XSS-1441-sub')>" = "nested"
    }
}
$test3

# ----------------------------------------------------------------------------
# Test 4 - Variable-name from request (lines 210, 245)
# The ISE editor parses identifier characters before sending the variable name,
# so the hostile name has to bypass the editor. Use the in-page helper
# spe.variableValue(name) - it closes over the live session GUID and returns
# the same HTML string the hover tooltip would render. With ISE open and a
# session running, paste this into the browser devtools console:
#
#   (function () {
#       var payload = "<img src=x onerror=alert('XSS-1441-name')>";
#       var html = spe.variableValue(payload);
#       var d = document.createElement('div');
#       d.innerHTML = html;
#       document.body.appendChild(d);
#       console.log('[1441] returned HTML:', html);
#   })();
#
# Before fix: alert("XSS-1441-name") fires when the appended div is parsed.
# After fix: no alert; the div shows escaped text and the console log shows
# the raw <span class='varName'> wrapping the &lt;img...&gt; entity sequence.
# ----------------------------------------------------------------------------

# ----------------------------------------------------------------------------
# Test 5 - Negative regression (must render identically before and after the fix)
# Hover each of the variables below. Property names, values, and expansion
# behavior should be unchanged. Generic types render with `1[[...]] notation
# (backtick-digit is HTML-safe; encoding is a no-op for these).
# ----------------------------------------------------------------------------
$normal = [PSCustomObject]@{
    Id    = 42
    Name  = "Hello"
    Items = @("a", "b", "c")
}
$normal

$item = Get-Item master:/sitecore/content
$item

$generic = New-Object 'System.Collections.Generic.List[string]'
$generic.Add("one")
$generic
