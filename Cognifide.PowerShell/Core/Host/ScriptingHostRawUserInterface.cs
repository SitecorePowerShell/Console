using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Host;
using Cognifide.PowerShell.Core.Settings;

namespace Cognifide.PowerShell.Core.Host
{
    public class ScriptingHostRawUserInterface : PSHostRawUserInterface
    {
        private ConsoleColor backgroundColor;
        private Size bufferSize;
        private Coordinates cursorPosition;
        private int cursorSize;
        private ConsoleColor foregroundColor;
        private Coordinates windowPosition;
        private Size windowSize;

        public ScriptingHostRawUserInterface(ApplicationSettings settings)
        {
            Output = new OutputBuffer();
            backgroundColor = settings.BackgroundColor;
            foregroundColor = settings.ForegroundColor;
            cursorPosition = new Coordinates(0, 0);
            windowPosition = new Coordinates(0, 0);
            cursorSize = 1;
            bufferSize = new Size(settings.HostWidth, Int32.MaxValue);
            windowSize = bufferSize;
        }

        /// <summary>
        ///     A reference to the PSHost implementation.
        /// </summary>
        internal OutputBuffer Output { get; private set; }

        public override ConsoleColor ForegroundColor
        {
            get { return foregroundColor; }
            set { foregroundColor = value; }
        }

        public override ConsoleColor BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        public override Coordinates CursorPosition
        {
            get { return cursorPosition; }
            set { cursorPosition = value; }
        }

        public override Coordinates WindowPosition
        {
            get { return windowPosition; }
            set { windowPosition = value; }
        }

        public override int CursorSize
        {
            get { return cursorSize; }
            set { cursorSize = value; }
        }

        public override Size BufferSize
        {
            get { return bufferSize; }
            set { bufferSize = value; }
        }

        public override Size WindowSize
        {
            get { return windowSize; }
            set { windowSize = value; }
        }

        public override Size MaxWindowSize
        {
            get { return bufferSize; }
        }

        public override Size MaxPhysicalWindowSize
        {
            get { return bufferSize; }
        }

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override bool KeyAvailable
        {
            get { throw new NotImplementedException(); }
        }

        public override string WindowTitle { get; set; }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotImplementedException();
        }

        public override void FlushInputBuffer()
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            Output.Clear();
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip,
            BufferCell fill)
        {
            throw new NotImplementedException();
        }
    }
}