using System;
using System.Drawing;
using System.Text;
using System.Web;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public class OutputLine
    {
        public const string FormatResponseText = "text";
        public const string FormatResponseHtml = "html";
        public const string FormatResponseJsterm = "jsterm";

        public OutputLine(OutputLineType outputLineType, string value, ConsoleColor foregroundColor,
                          ConsoleColor backgroundColor)
        {
            LineType = outputLineType;
            Text = value;
            ForegroundColor = foregroundColor;
            BackgroundColor = backgroundColor;
        }

        public OutputLineType LineType { get; internal set; }
        public string Text { get; internal set; }
        public ConsoleColor ForegroundColor { get; internal set; }
        public ConsoleColor BackgroundColor { get; internal set; }

        public void GetHtmlLine(StringBuilder output)
        {
            output.AppendFormat(
                "<span style='background-color:{0};color:{1};'>{2}</span>\r\n",
                BackgroundColor.ToString().Replace("DarkBlue", "#012456"),
                ForegroundColor.ToString().Replace("DarkBlue", "#012456"),
                HttpUtility.HtmlEncode(Text.TrimEnd()));
        }

        public string ToHtmlString()
        {
            return String.Format(
                "<span style='background-color:{0};color:{1};'>{2}</span>\r\n",
                BackgroundColor.ToString().Replace("DarkBlue", "#012456"),
                ForegroundColor.ToString().Replace("DarkBlue", "#012456"),
                HttpUtility.HtmlEncode(Text.TrimEnd()));
        }

        public void GetTerminalLine(StringBuilder output)
            //, ConsoleColor HostBackgroundColor, ConsoleColor HostForegroundColor)
        {
            Color htmlBackgroundColor = BackgroundColor == ConsoleColor.DarkBlue
                                            ? Color.FromArgb(1, 0x24, 0x56)
                                            : Color.FromName(BackgroundColor.ToString());
            Color htmlForegroundColor = Color.FromName(ForegroundColor.ToString());
            output.AppendFormat(
                "[[;#{0}{1}{2};#{3}{4}{5}]{6}] \r\n",
                htmlForegroundColor.R.ToString("X2"),
                htmlForegroundColor.G.ToString("X2"),
                htmlForegroundColor.B.ToString("X2"),
                htmlBackgroundColor.R.ToString("X2"),
                htmlBackgroundColor.G.ToString("X2"),
                htmlBackgroundColor.B.ToString("X2"),
                HttpUtility.HtmlEncode(Text.TrimEnd()).Replace("[", "%((%").Replace("]", "%))%"));
        }

        public void GetPlainTextLine(StringBuilder output)
        {
            output.Append(Text);
            output.Append("\n");
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