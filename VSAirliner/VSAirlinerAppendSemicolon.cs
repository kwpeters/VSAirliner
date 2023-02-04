using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace VSAirliner
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class VSAirlinerAppendSemicolon
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4131;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5b360608-e092-40e3-8e5c-a0eb27e2c8c9");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSAirlinerAppendSemicolon"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private VSAirlinerAppendSemicolon(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static VSAirlinerAppendSemicolon Instance
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
            // Switch to the main thread - the call to AddCommand in VSAirlinerAppendSemicolon's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new VSAirlinerAppendSemicolon(package, commandService);
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
            using (var edit = docInfo.Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(docInfo.CurLine.End.Position, ";");
                edit.Apply();
            }
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
