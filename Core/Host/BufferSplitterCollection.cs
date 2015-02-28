using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Host;

namespace Cognifide.PowerShell.Core.Host
{
    public class BufferSplitterCollection : IEnumerable<OutputLine>
    {
        private string value;
        private readonly ConsoleColor backgroundColor;
        private readonly ConsoleColor foregroundColor;
        private readonly bool terminated;
        private readonly OutputLineType type;
        private readonly int width;

        public BufferSplitterCollection(OutputLineType type, string value, PSHostRawUserInterface host, bool terminated)
        {
            this.type = type;
            this.value = value;
            this.terminated = terminated;
            width = host.BufferSize.Width;
            foregroundColor = host.ForegroundColor;
            backgroundColor = host.BackgroundColor;
        }

        public BufferSplitterCollection(OutputLineType type, string value, int width, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, bool terminated)
        {
            this.type = type;
            this.value = value;
            this.width = width;
            this.terminated = terminated;
            this.foregroundColor = foregroundColor;
            this.backgroundColor = backgroundColor;
        }

        public IEnumerator<OutputLine> GetEnumerator()
        {
            while (value.Length > width)
            {
                yield return new OutputLine(type, value.Substring(0, width), foregroundColor, backgroundColor, true);
                value = value.Substring(width);
            }
            yield return new OutputLine(type, value, foregroundColor, backgroundColor, terminated);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}