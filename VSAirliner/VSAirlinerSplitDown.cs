using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSAirliner
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class VSAirlinerSplitDown
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4132;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5b360608-e092-40e3-8e5c-a0eb27e2c8c9");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private static DTE2 _dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSAirlinerSplitDown"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private VSAirlinerSplitDown(AsyncPackage package, OleMenuCommandService commandService)
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
        public static VSAirlinerSplitDown Instance
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
            // Switch to the main thread - the call to AddCommand in VSAirlinerSplitDown's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new VSAirlinerSplitDown(package, commandService);

            _dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(_dte);
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

            _dte.ExecuteCommand("Edit.ScrollLineTop");
            var curLine = (_dte.ActiveDocument.Selection as TextSelection).TopLine;
            var curCol = (_dte.ActiveDocument.Selection as TextSelection).CurrentColumn;
            _dte.ExecuteCommand("Window.NewWindow");
            _dte.ExecuteCommand("Window.NewHorizontalTabGroup");
            (_dte.ActiveDocument.Selection as TextSelection).MoveTo(curLine, curCol);
            _dte.ExecuteCommand("Edit.ScrollLineCenter");

            // Failed attempt...
            //var model = ProjectHelpers.GetComponentModel();
            //var editor = (IEditorOperationsFactoryService)model.GetService<IEditorOperationsFactoryService>();
            //var adaptor = model.GetService<IVsEditorAdaptersFactoryService>();
            //IWpfTextView TextView = ProjectHelpers.GetCurentTextView();
            //var editorOperations = editor.GetEditorOperations(TextView);
            //editorOperations.ScrollLineCenter();

        }
    }
}
