using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSAirliner
{
    internal struct DocInfo
    {
        public readonly IWpfTextView View;
        public readonly ITextSnapshot Snapshot;
        public ITextSelection Selection;
        public int AnchorPos;
        public int ActivePos;
        public readonly ITextSnapshotLine CurLine;

        public DocInfo(IWpfTextView view, ITextSnapshot snapshot, ITextSelection selection, ITextSnapshotLine curLine)
        {
            this.View = view;
            this.Snapshot = snapshot;
            this.Selection = selection;
            this.AnchorPos = selection.AnchorPoint.Position.Position;
            this.ActivePos = selection.ActivePoint.Position.Position;
            CurLine = curLine;
        }
    }
}
