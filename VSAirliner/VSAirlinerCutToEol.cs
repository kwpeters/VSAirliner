using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace VSAirliner
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class VSAirlinerCutToEol
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4130;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5b360608-e092-40e3-8e5c-a0eb27e2c8c9");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// A timer that determines whether the user is killing text in rapid
        /// succession.  When this happens, the killed text is appended to the
        /// current clipboard contents.
        /// </summary>
        private readonly System.Timers.Timer accrueTimer;

        /// <summary>
        /// A regular expression that matches whitespace followed by
        /// non-whitespace.
        /// </summary>
        private readonly Regex textWithLeadingWhitespace = new Regex(@"^(?<leadingWhitespace>\s+)\S+");

        /// <summary>
        /// Initializes a new instance of the <see cref="VSAirlinerCutToEol"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private VSAirlinerCutToEol(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            this.accrueTimer = new System.Timers.Timer();
            accrueTimer.Interval = 2500;
            accrueTimer.AutoReset = false;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static VSAirlinerCutToEol Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in VSAirlinerCutToEol's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new VSAirlinerCutToEol(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get some information about the current document.
            var docInfoRes = this.GetDocInfo();
            if (!docInfoRes.HasValue)
            {
                return;
            }
            var docInfo = docInfoRes.Value;

            //
            // If there is currently a selection, just cut that and be done.
            //
            if (docInfo.AnchorPos != docInfo.ActivePos)
            {
                int start = Math.Min(docInfo.AnchorPos, docInfo.ActivePos);
                int end = Math.Max(docInfo.AnchorPos, docInfo.ActivePos);

                string selectedText = docInfo.Snapshot.GetText(start, end - start);
                Clipboard.SetText(selectedText);
                using (var edit = docInfo.Snapshot.TextBuffer.CreateEdit())
                {
                    edit.Delete(start, end - start);
                    edit.Apply();
                }

                return;
            }

            // If we are accruing copied text (due to the timeout timer), then
            // the text we need to copy should start with the current clipboard
            // contents.
            string textToCopy = this.accrueTimer.Enabled ? this.GetClipboardText() : "";

            // Get the text that follows the caret to the EOL.
            int eolPos = docInfo.CurLine.End.Position;
            Span toEolSpan = new Span(docInfo.ActivePos, eolPos - docInfo.ActivePos);
            string toEolText = docInfo.Snapshot.GetText(toEolSpan);

            if (toEolText.Length > 0)
            {
                // If the text remaining on the line is whitespace followed by
                // non-whitespace, then kill just the leading whitespace.  By
                // removing just the whitespace, we will make joining two lines
                // easier.
                var match = textWithLeadingWhitespace.Match(toEolText);
                if (match.Success)
                {
                    string leadingWhitespace = match.Groups["leadingWhitespace"].Value;
                    textToCopy += leadingWhitespace;

                    // Remove the whitespace from the document.
                    using (var edit = docInfo.Snapshot.TextBuffer.CreateEdit())
                    {
                        edit.Delete(docInfo.ActivePos, leadingWhitespace.Length);
                        edit.Apply();
                    }
                }
                else
                {
                    textToCopy += toEolText;

                    // Remove the kill text from the document.
                    using (var edit = docInfo.Snapshot.TextBuffer.CreateEdit())
                    {
                        edit.Delete(docInfo.ActivePos, toEolText.Length);
                        edit.Apply();
                    }
                }
            }
            else
            {
                // Delete the \n and \r characters that follow the caret
                // position.
                int numCharsToDelete = 0;
                bool nextCharIsLineEndingChar;
                do
                {
                    string nextChar = docInfo.Snapshot.GetText(docInfo.ActivePos + numCharsToDelete, 1);
                    nextCharIsLineEndingChar = nextChar == "\n" || nextChar == "\r";
                    if (nextCharIsLineEndingChar)
                    {
                        numCharsToDelete += 1;
                    }

                } while (nextCharIsLineEndingChar && numCharsToDelete < 2);

                textToCopy += docInfo.Snapshot.GetText(docInfo.ActivePos, numCharsToDelete);

                using (var edit = docInfo.Snapshot.TextBuffer.CreateEdit())
                {
                    edit.Delete(docInfo.ActivePos, numCharsToDelete);
                    edit.Apply();
                }
            }

            Clipboard.SetText(textToCopy);

            // Restart the accrue timer.
            this.accrueTimer.Stop();
            this.accrueTimer.Start();
        }

        /// <summary>
        /// Convenience helper method for getting useful information about the
        /// document being edited.
        /// </summary>
        /// <returns>A data structure containing useful information.</returns>
        private Nullable<DocInfo> GetDocInfo()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IWpfTextView view = ProjectHelpers.GetCurentTextView();
            if (view == null)
                return null;

            ITextSnapshot snapshot = view.TextSnapshot;
            ITextSelection selection = view.Selection;
            ITextSnapshotLine curLine = snapshot.GetLineFromPosition(selection.ActivePoint.Position.Position);
            return new DocInfo(view, snapshot, selection, curLine);
        }

        /// <summary>
        /// Gets text from the clipboard and accounts for the possibility there
        /// is no text on the clipboard.
        /// </summary>
        /// <returns>Text from the clipboard or an empty string.</returns>
        private string GetClipboardText()
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : "";
        }
    }
}
