using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EnterpriseManagement.UI.WpfWizardFramework;
using System.ComponentModel;
using Microsoft.EnterpriseManagement.UI.DataModel;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.Win32;
using Microsoft.EnterpriseManagement.ConnectorFramework;

namespace SettingsUI
{
    class AdminSettingWizardData : WizardData
    {
        #region Variable Declaration
        //looking to collapse some/all of these quickly? CTRL + MM
        //Yes, that's really two M back to back

        //azure settings
        private Boolean boolEnableAzureTranslate = false;
        private String acsTranslateAPIKey = String.Empty;

        //workflow settings
        private Boolean boolNotifyAssignedWF = false;
        private Boolean boolNotifyAffectedWF = false;
        private Boolean boolNotifyWorkItemComments = false;

        //templates
        ManagementPackObjectTemplate troubleTicketAnalystCommentTemplate;
        ManagementPackObjectTemplate troubleTicketUserCommentTemplate;
        ManagementPackObjectTemplate workItemCommentLogTemplate;

        //management pack guid
        private Guid guidEnterpriseManagementObjectID = Guid.Empty;
        #endregion

        //notify on property changed
        //https://docs.microsoft.com/en-us/dotnet/framework/wpf/data/how-to-implement-property-change-notification
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propchanger)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propchanger));
            }
        }

        #region variables get/set
        //azure settings
        public Boolean IsAzureTranslateEnabled
        {
            get
            {
                return this.boolEnableAzureTranslate;
            }
            set
            {
                if (this.boolEnableAzureTranslate != value)
                {
                    this.boolEnableAzureTranslate = value;
                }
            }
        }
        public String AzureCognitiveServicesTranslateAPIKey
        {
            get
            {
                return this.acsTranslateAPIKey;
            }
            set
            {
                if (this.acsTranslateAPIKey != value)
                {
                    this.acsTranslateAPIKey = value;
                }
            }
        }

        //workflow settings
        public Boolean IsNotifyAssignedUserEnabled
        {
            get
            {
                return this.boolNotifyAssignedWF;
            }
            set
            {
                if (this.boolNotifyAssignedWF != value)
                {
                    this.boolNotifyAssignedWF = value;
                }
            }
        }
        public Boolean IsNotifyAffecteddUserEnabled
        {
            get
            {
                return this.boolNotifyAffectedWF;
            }
            set
            {
                if (this.boolNotifyAffectedWF != value)
                {
                    this.boolNotifyAffectedWF = value;
                }
            }
        }
        public Boolean IsNotifyWorkItemCommentsEnabled
        {
            get
            {
                return this.boolNotifyWorkItemComments;
            }
            set
            {
                if (this.boolNotifyWorkItemComments != value)
                {
                    this.boolNotifyWorkItemComments = value;
                }
            }
        }

        //templates
        public ManagementPackObjectTemplate TroubleTicketAnalystCommentTemplate
        {
            get
            {
                return troubleTicketAnalystCommentTemplate;
            }
            set
            {
                if (this.troubleTicketAnalystCommentTemplate != value)
                {
                    troubleTicketAnalystCommentTemplate = value;
                    NotifyPropertyChanged("troubleTicketAnalystCommentTemplate");
                }
            }
        }
        public IList<ManagementPackObjectTemplate> TroubleTicketAnalystCommentTemplates { get; set; }
        public ManagementPackObjectTemplate TroubleTicketUserCommentTemplate
        {
            get
            {
                return troubleTicketUserCommentTemplate;
            }
            set
            {
                if (this.troubleTicketUserCommentTemplate != value)
                {
                    troubleTicketUserCommentTemplate = value;
                    NotifyPropertyChanged("troubleTicketUserCommentTemplate");
                }
            }
        }
        public IList<ManagementPackObjectTemplate> TroubleTicketUserCommentTemplates { get; set; }
        public ManagementPackObjectTemplate WorkItemCommentLogTemplate
        {
            get
            {
                return workItemCommentLogTemplate;
            }
            set
            {
                if (this.workItemCommentLogTemplate != value)
                {
                    workItemCommentLogTemplate = value;
                    NotifyPropertyChanged("workItemCommentLogTemplate");
                }
            }
        }
        public IList<ManagementPackObjectTemplate> WorkItemCommentLogTemplates { get; set; }

        //management pack guid
        public Guid EnterpriseManagementObjectID
        {
            get
            {
                return this.guidEnterpriseManagementObjectID;
            }
            set
            {
                if (this.guidEnterpriseManagementObjectID != value)
                {
                    this.guidEnterpriseManagementObjectID = value;
                }
            }
        }
        #endregion

        //This Loads the settings that have been set in the management pack. In some cases for user protection, they load values from the function below
        //so on Save some kind of value is committed. This provides the means to suggest a value once, but ignore it afterwards.
        internal AdminSettingWizardData(EnterpriseManagementObject emoAdminSetting)
        {
            //Get the server name to connect to and then connect 
            String strServerName = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\System Center\\2010\\Service Manager\\Console\\User Settings", "SDKServiceMachine", "localhost").ToString();
            EnterpriseManagementGroup emg = new EnterpriseManagementGroup(strServerName);

            /*Get the Advanced Action Notifier class by GUID
            ##PowerShell SMlets equivalent:
                Get-SCSMClass -name "AdhocAdam.Advanced"
                Get-SCSMClass -id "49a053e7-6080-e211-fd79-ca3607eecce7"
            */
            ManagementPackClass adhocAdamAALNClass = emg.EntityTypes.GetClass(new Guid("49a053e7-6080-e211-fd79-ca3607eecce7"));

            //Azure Settings
            try { this.IsAzureTranslateEnabled = Boolean.Parse(emoAdminSetting[adhocAdamAALNClass, "EnableAzureTranslate"].ToString()); }
            catch { this.IsAzureTranslateEnabled = false; }
            this.AzureCognitiveServicesTranslateAPIKey = emoAdminSetting[adhocAdamAALNClass, "ACSAPIKey"].ToString();

            //Retrieve the unsealed workflow management pack that contains the workflows, to control whether or not the workflows are enabled/disabled
            //https://marcelzehner.ch/2013/07/04/a-look-at-scsm-workflows-and-notifications-and-how-to-manage-them-by-using-scripts/
            ManagementPack adhocAdamAALWFClass = emg.ManagementPacks.GetManagementPack(new Guid("4db18489-38fa-895a-7149-902b675dc752"));
            ManagementPackRule assignedNotificationWF = adhocAdamAALWFClass.GetRule("AdhocAdam.Advanced.Action.Log.Notify.TroubleTicketAnalystCommentNotification");
            ManagementPackRule affectedNotificationWF = adhocAdamAALWFClass.GetRule("AdhocAdam.Advanced.Action.Log.Notify.TroubleTicketUserCommentNotification");
            ManagementPackRule workItemCommentNotificationWF = adhocAdamAALWFClass.GetRule("AdhocAdam.Advanced.Action.Log.Notify.WorkItemUserCommentNotification");
            
            //Assigned User notification
            if (assignedNotificationWF.Enabled == ManagementPackMonitoringLevel.@false)
            {
                this.IsNotifyAssignedUserEnabled = false;
            }
            else
            {
                this.IsNotifyAssignedUserEnabled = true;
            }

            //Affected User notification
            if (affectedNotificationWF.Enabled == ManagementPackMonitoringLevel.@false)
            {
                this.IsNotifyAffecteddUserEnabled = false;
            }
            else
            {
                this.IsNotifyAffecteddUserEnabled = true;
            }

            //Work Item (Service Request) notification
            if (workItemCommentNotificationWF.Enabled == ManagementPackMonitoringLevel.@false)
            {
                this.IsNotifyWorkItemCommentsEnabled = false;
            }
            else
            {
                this.IsNotifyWorkItemCommentsEnabled = true;
            }

            //Templates
            //First, we'll pull all of the Email Templates from the Admin Settings pane
            //Get-SCSMClass -id "0814d9a7-8332-a5df-2ec8-34d07f3d40db"
            //Get-SCSMClass -name "System.Notification.Template.SMTP"
            ManagementPackObjectTemplateCriteria mpotcTTATemplates = new ManagementPackObjectTemplateCriteria("TypeID = '0814d9a7-8332-a5df-2ec8-34d07f3d40db'");
            this.TroubleTicketAnalystCommentTemplates = emg.Templates.GetObjectTemplates(mpotcTTATemplates);
            this.TroubleTicketAnalystCommentTemplates = this.TroubleTicketAnalystCommentTemplates.OrderBy(template => template.DisplayName).ToList();
            try
            {
                Guid ttacTemplate = (Guid)emoAdminSetting[null, "TroubleTicketAnalystCommentTemplate"].Value;
                this.TroubleTicketAnalystCommentTemplate = emg.Templates.GetObjectTemplate(ttacTemplate);
            }
            catch { }

            ManagementPackObjectTemplateCriteria mpotcTTUTemplates = new ManagementPackObjectTemplateCriteria("TypeID = '0814d9a7-8332-a5df-2ec8-34d07f3d40db'");
            this.TroubleTicketUserCommentTemplates = emg.Templates.GetObjectTemplates(mpotcTTUTemplates);
            this.TroubleTicketUserCommentTemplates = this.TroubleTicketUserCommentTemplates.OrderBy(template => template.DisplayName).ToList();
            try
            {
                Guid ttucTemplate = (Guid)emoAdminSetting[null, "TroubleTicketUserCommentTemplate"].Value;
                this.TroubleTicketUserCommentTemplate = emg.Templates.GetObjectTemplate(ttucTemplate);
            }
            catch { }

            ManagementPackObjectTemplateCriteria mpotcWICLTemplates = new ManagementPackObjectTemplateCriteria("TypeID = '0814d9a7-8332-a5df-2ec8-34d07f3d40db'");
            this.WorkItemCommentLogTemplates = emg.Templates.GetObjectTemplates(mpotcWICLTemplates);
            this.WorkItemCommentLogTemplates = this.WorkItemCommentLogTemplates.OrderBy(template => template.DisplayName).ToList();
            try
            {
                Guid wiclTemplate = (Guid)emoAdminSetting[null, "WorkItemCommentTemplate"].Value;
                this.WorkItemCommentLogTemplate = emg.Templates.GetObjectTemplate(wiclTemplate);
            }
            catch { }

            //load the MP
            this.EnterpriseManagementObjectID = emoAdminSetting.Id;
        }

        //This Saves the values that have been set throughout the Wizard Pages back into the management pack so the
        //Advanced Action Log Notifier can read them via ((Get-SCSMObject -Class (Get-SCSMClass -Name "AdhocAdam.Advanced.Action.Log.Notify.AdminSettings$")))
        public override void AcceptChanges(WizardMode wizardMode)
        {
            //Get the server name to connect to and then connect 
            String strServerName = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\System Center\\2010\\Service Manager\\Console\\User Settings", "SDKServiceMachine", "localhost").ToString();
            EnterpriseManagementGroup emg = new EnterpriseManagementGroup(strServerName);

            //Get the Advanced Action Notifier class by GUID
            ManagementPackClass adhocAdamAALNClass = emg.EntityTypes.GetClass(new Guid("49a053e7-6080-e211-fd79-ca3607eecce7"));

            //Get the SMLets Exchange Connector Settings object using the object GUID 
            EnterpriseManagementObject emoAdminSetting = emg.EntityObjects.GetObject<EnterpriseManagementObject>(this.EnterpriseManagementObjectID, ObjectQueryOptions.Default);

            //Workflow Status
            //Retrieve the unsealed workflow management pack that contains the workflows, to control whether or not the workflows are enabled/disabled
            //https://marcelzehner.ch/2013/07/04/a-look-at-scsm-workflows-and-notifications-and-how-to-manage-them-by-using-scripts/
            ManagementPack adhocAdamAALWFClass = emg.ManagementPacks.GetManagementPack(new Guid("4db18489-38fa-895a-7149-902b675dc752"));
            ManagementPackRule assignedNotificationWF = adhocAdamAALWFClass.GetRule("AdhocAdam.Advanced.Action.Log.Notify.TroubleTicketAnalystCommentNotification");
            ManagementPackRule affectedNotificationWF = adhocAdamAALWFClass.GetRule("AdhocAdam.Advanced.Action.Log.Notify.TroubleTicketUserCommentNotification");
            ManagementPackRule workItemCommentNotificationWF = adhocAdamAALWFClass.GetRule("AdhocAdam.Advanced.Action.Log.Notify.WorkItemUserCommentNotification");
            assignedNotificationWF.Status = ManagementPackElementStatus.PendingUpdate;
            affectedNotificationWF.Status = ManagementPackElementStatus.PendingUpdate;
            workItemCommentNotificationWF.Status = ManagementPackElementStatus.PendingUpdate;

            //Notify Assigned To
            if (this.IsNotifyAssignedUserEnabled == true)
            {
                assignedNotificationWF.Enabled = ManagementPackMonitoringLevel.@true;
            }
            else
            {
                assignedNotificationWF.Enabled = ManagementPackMonitoringLevel.@false;
            }

            //Notify Affected User
            if (this.IsNotifyAffecteddUserEnabled == true)
            {
                affectedNotificationWF.Enabled = ManagementPackMonitoringLevel.@true;
            }
            else
            {
                affectedNotificationWF.Enabled = ManagementPackMonitoringLevel.@false;
            }

            //Service Request Comments
            if (this.IsNotifyWorkItemCommentsEnabled == true)
            {
                workItemCommentNotificationWF.Enabled = ManagementPackMonitoringLevel.@true;
            }
            else
            {
                workItemCommentNotificationWF.Enabled = ManagementPackMonitoringLevel.@false;
            }
            
            //save the changes back to the unsealed mp
            adhocAdamAALWFClass.AcceptChanges();

            //Azure Settings
            emoAdminSetting[adhocAdamAALNClass, "EnableAzureTranslate"].Value = this.IsAzureTranslateEnabled;
            emoAdminSetting[adhocAdamAALNClass, "ACSAPIKey"].Value = this.AzureCognitiveServicesTranslateAPIKey;

            //Templates
            try { emoAdminSetting[adhocAdamAALNClass, "TroubleTicketAnalystCommentTemplate"].Value = this.TroubleTicketAnalystCommentTemplate.Id; }
            catch { }
            try { emoAdminSetting[adhocAdamAALNClass, "TroubleTicketUserCommentTemplate"].Value = this.TroubleTicketUserCommentTemplate.Id; }
            catch { }
            try { emoAdminSetting[adhocAdamAALNClass, "WorkItemCommentTemplate"].Value = this.WorkItemCommentLogTemplate.Id; }
            catch { }

            //Update the MP
            emoAdminSetting.Commit();
            this.WizardResult = WizardResult.Success;
        }
    }
}
