using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Reflection;
using System.Workflow.ComponentModel.Compiler;
using System.Workflow.ComponentModel.Serialization;
using System.Workflow.ComponentModel;
using System.Workflow.ComponentModel.Design;
using System.Workflow.Runtime;
using System.Workflow.Activities;
using System.Workflow.Activities.Rules;

namespace Advanced.Action.Log.Notifier
{
    public partial class TroubleTicketEndUserNotification
    {
        #region Activity Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("", "")]
        private void InitializeComponent()
        {
            // 
            // TroubleTicketAnalystNotification
            // 
            this.Name = "TroubleTicketEndUserNotification";
            this.BlindCarbonCopyUserList = null;
            this.CarbonCopyUserList = null;
            this.DataItem = null;
            this.DataItems = null;
            this.Description = "Send Email Notification";
            this.InstanceId = null;
            this.InstanceIds = null;
            this.Name = "SendUserEmailNotification";
            this.PrimaryUserList = null;
            this.PrimaryUserRelationships = null;
            this.SubscriptionId = new System.Guid("00000000-0000-0000-0000-000000000000");
            this.TemplateIds = null;
        }

        #endregion
    }
}
