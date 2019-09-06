using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Linq;
using System.Workflow.ComponentModel.Compiler;
using System.Workflow.ComponentModel.Serialization;
using System.Workflow.ComponentModel;
using System.Workflow.ComponentModel.Design;
using System.Workflow.Runtime;
using System.Workflow.Activities;
using System.Workflow.Activities.Rules;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Workflow.Common;
using Microsoft.EnterpriseManagement.Notifications.Workflows;
using System.Collections.Generic;

namespace Advanced.Action.Log.Notifier
{
    public class NotifyUser : SendNotificationsActivity
    {
        public void SendNotification(Guid SubscriptionId, string[] DataItems, string InstanceId, string[] TemplateIds, string[] PrimaryUserGuids, ActivityExecutionContext executionContext)
        {
            NotifyUser notice = new NotifyUser();
            notice.SubscriptionId = SubscriptionId;
            notice.DataItems = DataItems;
            notice.InstanceId = InstanceId;
            notice.TemplateIds = TemplateIds;
            notice.PrimaryUserList = PrimaryUserGuids;

            notice.Execute(executionContext);
        }
    }
}