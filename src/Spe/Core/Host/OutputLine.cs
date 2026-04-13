using System;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Spe.Core.Host
{
    public class OutputLine
    {
        public const string FormatResponseText = "text";
        public const string FormatResponseHtml = "html";
        public const string FormatResponseJsterm = "jsterm";

        public OutputLine(OutputLineType outputLineType, string value, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, bool terminated)
        {
            LineType = outputLineType;
            Text = value;
            ForegroundColor = foregroundColor;
            BackgroundColor = backgroundColor;
            Terminated = terminated;
        }

        public OutputLineType LineType { get; internal set; }
        public string Text { get; internal set; }
        public ConsoleColor ForegroundColor { get; internal set; }
        public ConsoleColor BackgroundColor { get; internal set; }
        public bool Terminated { get; internal set; }

        public void GetHtmlLine(StringBuilder output, bool enableGuidLink = true)
        {
            var outString = Terminated ? Text.TrimEnd() : Text;

            outString = HttpUtility.HtmlEncode(outString);
            if (enableGuidLink && outString.Contains("{"))
            {
                outString = Regex.Replace(outString,
                    @"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
                    "<a onclick=\"javascript:return scForm.postEvent(this,event,'item:load(id={$0})')\" href=\"#\">$0</a>",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            else if(outString.Contains("http:") || outString.Contains("https:"))
            {
                var urlRegex = new Regex(@"(\w+:\/\/[\w@][\w.:@]+\/?[\w\.?=%&=\-@/$,]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                outString = urlRegex.Replace(outString, "<a target='_blank' href='$1'>$1</a>");
            }
            output.AppendFormat(
                Terminated
                    ? "<span style='background-color:{0};color:{1};'>{2}</span>\r\n"
                    : "<span style='background-color:{0};color:{1};'>{2}</span>",
                ProcessHtmlColor(BackgroundColor),
                ProcessHtmlColor(ForegroundColor),
                outString);
        }

        public string ToHtmlString()
        {
            var outString = Terminated ? Text.TrimEnd() : Text;
            outString = HttpUtility.HtmlEncode(outString);
            if (outString.Contains("{"))
            {
                outString = Regex.Replace(outString,
                    @"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
                    "<a onclick=\"javascript:return scForm.postEvent(this,event,'item:load(id={$0})')\" href=\"#\">$0</a>",
                    RegexOptions.IgnoreCase);
            }
            return String.Format(
                Terminated
                    ? "<span style='background-color:{0};color:{1};'>{2}</span>\r\n"
                    : "<span style='background-color:{0};color:{1};'>{2}</span>",
                ProcessHtmlColor(BackgroundColor),
                ProcessHtmlColor(ForegroundColor),
                outString);
        }

        public static string ProcessHtmlColor(ConsoleColor color)
        {
            switch (color)
            {
                case (ConsoleColor.DarkBlue):
                    return "#012456";
                case (ConsoleColor.Green):
                    return "Lime";
                default:
                    return color.ToString();
            }
        }

        public static Color ProcessTerminalColor(ConsoleColor color, bool isBackground = false)
        {
            Color c;
            switch (color)
            {
                case ConsoleColor.DarkBlue:
                    c = Color.FromArgb(1, 0x24, 0x56);
                    break;
                case ConsoleColor.Green:
                    c = Color.LimeGreen;
                    break;
                default:
                    c = Color.FromName(color.ToString());
                    break;
            }

            // Darken non-default background colors so text remains legible
            // even when foreground and background use the same ConsoleColor.
            // DarkBlue is the default console background and stays as-is.
            if (isBackground && color != ConsoleColor.DarkBlue)
            {
                return Color.FromArgb(
                    (int)(c.R * 0.65),
                    (int)(c.G * 0.65),
                    (int)(c.B * 0.65));
            }
            return c;
        }

        public void GetTerminalLine(StringBuilder output)
        {
            var outString = Terminated ? Text.TrimEnd() : Text;

            // Error lines use a fixed legible color with no background
            // (terminal default). Other lines use the mapped ConsoleColors.
            string fgHex, bgHex;
            if (LineType == OutputLineType.Error)
            {
                fgHex = "#FF9494";
                bgHex = "";
            }
            else
            {
                var fg = ProcessTerminalColor(ForegroundColor, false);
                var bg = ProcessTerminalColor(BackgroundColor, true);
                fgHex = $"#{fg.R:X2}{fg.G:X2}{fg.B:X2}";
                bgHex = $"#{bg.R:X2}{bg.G:X2}{bg.B:X2}";
            }

            // Split on \n as a safety net - Write(ConsoleColor,...) splits at
            // storage time but Write(string) may still append newlines to text.
            // Each segment gets a trailing space to prevent empty format blocks,
            // trailing-backslash escaping issues, and blank-line collapsing.
            var segments = (outString ?? "").Split('\n');
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i].TrimEnd('\r');
                var escaped = HttpUtility.HtmlEncode(seg + " ")
                    .Replace("[", "&#91;")
                    .Replace("]", "&#93;");
                output.AppendFormat("[[;{0};{1}]{2}]", fgHex, bgHex, escaped);
                if (i < segments.Length - 1) output.Append("\n");
            }
            if (Terminated) output.Append("\r\n");
        }

        public void GetPlainTextLine(StringBuilder output)
        {
            if (Terminated)
            {
                output.AppendLine(Text);
            }
            else
            {
                output.Append(Text);
            }
            
        }

        public void GetLine(StringBuilder temp, string stringFormat)
        {
            switch (stringFormat)
            {
                case (FormatResponseHtml):
                    GetHtmlLine(temp);
                    break;
                case (FormatResponseJsterm):
                    GetTerminalLine(temp);
                    break;
                //case (FormatResponseText):
                default:
                    GetPlainTextLine(temp);
                    break;
            }
        }
    }
}