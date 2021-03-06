using System.Collections.Generic;
using System.Diagnostics;
using Composite.Core.ResourceSystem;
using Composite.Core.Types;


namespace Composite.Core.WebClient.Services.TreeServiceObjects
{
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
    [DebuggerDisplay("ClientElement: '{Label}'")]
    public sealed class ClientElement
    {
        /// <exclude />
        public string ElementKey { get; set; }  // CORE

        /// <exclude />
        public string ProviderName { get; set; }

        /// <exclude />
        public string EntityToken { get; set; }

        /// <exclude />
        public string Piggybag { get; set; }

        /// <exclude />
        public string PiggybagHash { get; set; }

        /// <exclude />
        public string Label { get; set; }  // CORE

        /// <exclude />
        public string ToolTip { get; set; }  // CORE

        /// <exclude />
        public bool HasChildren { get; set; }  // CORE

        /// <exclude />
        public bool IsDisabled { get; set; }  // CORE

        /// <exclude />
        public ResourceHandle Icon { get; set; }  // CORE

        /// <exclude />
        public ResourceHandle OpenedIcon { get; set; }   // CORE       

        /// <exclude />
        public List<ClientAction> Actions { get; set; }

        /// <exclude />
        public List<string> ActionKeys { get; set; }

        /// <exclude />
        public List<KeyValuePair> PropertyBag { get; set; }  // CORE

        /// <exclude />
        public List<string> DropTypeAccept { get; set; }
        
        /// <exclude />
        public bool DetailedDropSupported { get; set; }

        /// <exclude />
        public string DragType { get; set; }

        /// <exclude />
        public string TagValue { get; set; } // CORE

        /// <exclude />
        public bool ContainsTaggedActions { get; set; } // CORE

        /// <exclude />
        public bool TreeLockEnabled { get; set; }
    }
}
