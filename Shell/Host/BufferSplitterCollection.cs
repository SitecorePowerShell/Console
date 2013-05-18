using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Host;

namespace Cognifide.PowerShell.Shell.Host
{
    public class BufferSplitterCollection : IEnumerable<OutputLine>
    {
        private readonly ConsoleColor backgroundColor;
        private readonly ConsoleColor foregroundColor;
        private readonly OutputLineType type;
        private readonly int width;
        private string value;

        public BufferSplitterCollection(OutputLineType type, string value, PSHostRawUserInterface host)
        {
            this.type = type;
            this.value = value;
            width = host.BufferSize.Width;
            foregroundColor = host.ForegroundColor;
            backgroundColor = host.BackgroundColor;
        }

        public BufferSplitterCollection(OutputLineType type, string value, int width, ConsoleColor foregroundColor,
                              ConsoleColor backgroundColor)
        {
            this.type = type;
            this.value = value;
            this.width = width;
            this.foregroundColor = foregroundColor;
            this.backgroundColor = backgroundColor;
        }

        public IEnumerator<OutputLine> GetEnumerator()
        {
            while (value.Length > width)
            {
                yield return new OutputLine(type, value.Substring(0, width), foregroundColor, backgroundColor);
                value = value.Substring(width);
            }
            yield return new OutputLine(type, value, foregroundColor, backgroundColor);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}