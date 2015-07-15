using System;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Cognifide.PowerShell.Core.Host
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

        public void GetHtmlLine(StringBuilder output)
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

        public static Color ProcessTerminalColor(ConsoleColor color)
        {
            switch (color)
            {
                case (ConsoleColor.DarkBlue):
                    return Color.FromArgb(1, 0x24, 0x56);
                case (ConsoleColor.Green):
                    return Color.LimeGreen;
                default:
                    return Color.FromName(color.ToString());
            }
        }

        public void GetTerminalLine(StringBuilder output)
        {
            var outString = Terminated ? Text.TrimEnd() : Text;
            if (outString.EndsWith("\\"))
            {
                outString += " ";
            }
            var htmlBackgroundColor = ProcessTerminalColor(BackgroundColor);
            var htmlForegroundColor = ProcessTerminalColor(ForegroundColor);
            output.AppendFormat(
                Terminated
                    ? "[[;#{0}{1}{2};#{3}{4}{5}]{6}]\r\n"
                    : "[[;#{0}{1}{2};#{3}{4}{5}]{6}]",
                htmlForegroundColor.R.ToString("X2"),
                htmlForegroundColor.G.ToString("X2"),
                htmlForegroundColor.B.ToString("X2"),
                htmlBackgroundColor.R.ToString("X2"),
                htmlBackgroundColor.G.ToString("X2"),
                htmlBackgroundColor.B.ToString("X2"),
                HttpUtility.HtmlEncode(outString).Replace("[", "&#91;").Replace("]", "&#93;"));
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