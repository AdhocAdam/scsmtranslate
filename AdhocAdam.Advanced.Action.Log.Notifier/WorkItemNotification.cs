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
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Workflow.Common;
using Microsoft.EnterpriseManagement.Notifications.Workflows;
using System.Collections.Generic;
using System.Globalization;

namespace Advanced.Action.Log.Notifier
{
    //building a notification
    //https://techcommunity.microsoft.com/t5/System-Center-Blog/Custom-Notification-workflow-on-activity-assignment-or/ba-p/341513

    //building service request notifications
    //https://social.technet.microsoft.com/Forums/lync/en-US/b7b7e6d2-53aa-43af-849f-4385d58118cc/service-request-activities-comments-and-notifications-for-service-desk?forum=systemcenterservicemanager

    //SCSM 2012: Notify the analyst when an end-user comment is added to an incident
    //https://social.technet.microsoft.com/Forums/ie/en-US/ef5a80e6-af12-4cba-9d9f-3798944035f1/scsm-2012-notify-the-analyst-when-an-enduser-comment-is-added-to-an-incident?forum=systemcenterservicemanager

    //SLA management, "How we Built the Solution"
    //https://techcommunity.microsoft.com/t5/System-Center-Blog/Incident-SLA-Management-in-Service-Manager/ba-p/341691

    //building Service Request Notifications
    //http://www.scsm.se/?p=875

    //this is triggered when an a Comment is left on a Service Request, we'll figure out if its a User or Analyst
    public partial class WorkItemNotification : SendNotificationsActivity
    {
        //define the Workflow Parameters coming in from the Management Pack
        public static DependencyProperty SubscriptionIdProperty = DependencyProperty.Register("SubscriptionId", typeof(Guid), typeof(WorkItemNotification));
        public static DependencyProperty DataItemsProperty = DependencyProperty.Register("DataItems", typeof(string[]), typeof(WorkItemNotification));
        public static DependencyProperty InstanceIdsProperty = DependencyProperty.Register("InstanceIds", typeof(string[]), typeof(WorkItemNotification));

        [DescriptionAttribute("The Subscription ID's GUID")]
        [CategoryAttribute("Test category")]
        [BrowsableAttribute(true)]
        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
        public Guid SubscriptionId
        {
            get
            {
                return ((Guid)(base.GetValue(WorkItemNotification.SubscriptionIdProperty)));
            }
            set
            {
                base.SetValue(WorkItemNotification.SubscriptionIdProperty, value);
            }
        }

        [DescriptionAttribute("The Data Items")]
        [CategoryAttribute("Test category")]
        [BrowsableAttribute(true)]
        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
        public string[] DataItems
        {
            get
            {
                return ((string[])(base.GetValue(WorkItemNotification.DataItemsProperty)));
            }
            set
            {
                base.SetValue(WorkItemNotification.DataItemsProperty, value);
            }
        }

        [DescriptionAttribute("The GUID of the comment object as a string defined per the MP.")]
        [CategoryAttribute("Test category")]
        [BrowsableAttribute(true)]
        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
        public string[] InstanceIds
        {
            get
            {
                return ((string[])(base.GetValue(WorkItemNotification.InstanceIdsProperty)));
            }
            set
            {
                base.SetValue(WorkItemNotification.InstanceIdsProperty, value);
            }
        }

        //execute the Workflow to Notify the Assigned/Affected User
        protected override ActivityExecutionStatus Execute(ActivityExecutionContext WIExecutionContext)
        {
            //since workflows only run on the workflow server, we can safely
            //connect and execute against the localhost
            EnterpriseManagementGroup emg = new EnterpriseManagementGroup("localhost");

            //Get the classes we need to work with
            /*
                get-scsmclass -name "System.WorkItem.TroubleTicket.AnalystCommentLog" | select name, id
                get-scsmclass -name "System.WorkItem.TroubleTicket.UserCommentLog" | select name, id
                get-scsmrelationshipclass -name "System.WorkItem.TroubleTicketHasAnalystComment" | select name, id
              https://github.com/SMLets/SMLets/blob/55f1bac3bc7a7011a461b24f6d7787ba89fe2624/SMLets.Shared/Code/EntityTypes.cs#L120
              https://github.com/SMLets/SMLets/blob/55f1bac3bc7a7011a461b24f6d7787ba89fe2624/SMLets.Shared/Code/EntityTypes.cs#L161

                 Name                                            Id
                 ----                                            --
                 System.WorkItem.TroubleTicket.AnalystCommentLog f14b70f4-878c-c0e1-b5c1-06ca22d05d40
                 System.WorkItem.TroubleTicket.UserCommentLog    a3d4e16f-5e8a-18ba-9198-d9815194c986
             */
            ManagementPackClass AnalystCommentClass = emg.EntityTypes.GetClass(new Guid("f14b70f4-878c-c0e1-b5c1-06ca22d05d40"));
            ManagementPackClass UserCommentClass = emg.EntityTypes.GetClass(new Guid("a3d4e16f-5e8a-18ba-9198-d9815194c986"));
            ManagementPackClass UserClass = emg.EntityTypes.GetClass(new Guid("943d298f-d79a-7a29-a335-8833e582d252"));
            ManagementPackClass LocalizationClass = emg.EntityTypes.GetClass(new Guid("efa8bbd3-3fa4-2f37-d0d5-7a6bf53be7c8"));
            ManagementPackRelationship WorkItemHasCommentLogRelClass = emg.EntityTypes.GetRelationshipClass(new Guid("79d27435-5917-b0a1-7911-fb2b678f32a6"));
            ManagementPackRelationship AssignedUserRelClass = emg.EntityTypes.GetRelationshipClass(new Guid("15e577a3-6bf9-6713-4eac-ba5a5b7c4722"));
            ManagementPackRelationship AffectedUserRelClass = emg.EntityTypes.GetRelationshipClass(new Guid("dff9be66-38b0-b6d6-6144-a412a3ebd4ce"));
            ManagementPackRelationship UserHasPreferenceRelClass = emg.EntityTypes.GetRelationshipClass(new Guid("649e37ab-bf89-8617-94f6-d4d041a05171"));

            //Get the Template GUID from the Translate MP
            EnterpriseManagementObject emoAdminSetting = emg.EntityObjects.GetObject<EnterpriseManagementObject>(new Guid("49a053e7-6080-e211-fd79-ca3607eecce7"), ObjectQueryOptions.Default);
            string templateID = emoAdminSetting[null, "WorkItemCommentTemplate"].Value.ToString();
            bool isAzureTranslateEnabled;
            try { isAzureTranslateEnabled = Boolean.Parse(emoAdminSetting[null, "EnableAzureTranslate"].ToString()); }
            catch { isAzureTranslateEnabled = false; }

            //get the objects we'll need to work with starting with...
            //Comment object, the string/guid is an incoming parameter from the management pack
            //$commentObject = Get-SCSMObject -id "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            Guid CommentGuid = new Guid(this.InstanceIds[0].ToString());
            EnterpriseManagementObject CommentObject = emg.EntityObjects.GetObject<EnterpriseManagementObject>(CommentGuid, ObjectQueryOptions.Default);

            //verify the comment came from a Service Request
            //Comment and Work Item relationship object
            //Using SMLets' Get-SCSMRelationshipObject -ByTarget parameter: https://github.com/SMLets/SMLets/blob/55f1bac3bc7a7011a461b24f6d7787ba89fe2624/SMLets.Shared/Code/EntityObjects.cs#L1853
            //$WorkItemActionLogRelObject = Get-SCSMRelationshipObject -ByTarget $commentObject | ?{$_.relationshipid -eq "835a64cd-7d41-10eb-e5e4-365ea2efc2ea"}
            IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> CommentWorkItemRelObjects = emg.EntityObjects.GetRelationshipObjectsWhereTarget<EnterpriseManagementObject>(CommentObject.Id, ObjectQueryOptions.Default);
            EnterpriseManagementRelationshipObject<EnterpriseManagementObject> WorkItemActionLogRelObject = null;
            foreach (EnterpriseManagementRelationshipObject<EnterpriseManagementObject> relObject in CommentWorkItemRelObjects)
            {
                //if the relationship object has an ID that is the WorkItemHasCommentLogRelClass, then update the variable above with it
                if (relObject.RelationshipId.ToString() == WorkItemHasCommentLogRelClass.Id.ToString())
                {
                    //we now have the relationship object that contains the Comment and the Work Item
                    WorkItemActionLogRelObject = relObject;
                    break;
                }
            }
            //Work Item object
            //Get-SCSMObject -id $WorkItemActionLogRelObject.SourceObject.Id
            EnterpriseManagementObject WorkItem = emg.EntityObjects.GetObject<EnterpriseManagementObject>(WorkItemActionLogRelObject.SourceObject.Id, ObjectQueryOptions.Default);

            //as long as the Work Item's Class ID is a Service Request, we can go through with this notification
            if (WorkItem.LeastDerivedNonAbstractManagementPackClassId.ToString() == "04b69835-6343-4de2-4b19-6be08c612989")
            {
                //figure out if the Comment is a Analyst or User Comment
                if (CommentObject.GetLeastDerivedNonAbstractClass().Name == AnalystCommentClass.Name)
                {
                    //Analyst Comment, notify Affected

                    //make sure it isn't a Private comment
                    bool isCommentPrivate = Boolean.Parse(CommentObject[(ManagementPackType)AnalystCommentClass, "IsPrivate"].Value.ToString());
                    if (isCommentPrivate)
                    {
                        //it's a private comment, exit the workflow
                        return base.Execute(WIExecutionContext);
                    }

                    //Affected User relationship object
                    IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> WorkItemRelUsers = emg.EntityObjects.GetRelationshipObjectsWhereSource<EnterpriseManagementObject>(WorkItem.Id, TraversalDepth.Recursive, ObjectQueryOptions.Default);
                    EnterpriseManagementObject AffectedUser = null;
                    foreach (EnterpriseManagementRelationshipObject<EnterpriseManagementObject> relObject in WorkItemRelUsers)
                    {
                        //if the relationship object has an ID that is the Affected User and it isn't an old (isDeleted) user, then update the variable above with it
                        if ((relObject.RelationshipId == AffectedUserRelClass.Id) && (relObject.IsDeleted == false))
                        {
                            //we now have the relationship object that contains the Affected User
                            AffectedUser = relObject.TargetObject;
                            break;
                        }
                    }

                    //if the Affected User exists, try and retrieve their Locale
                    if (AffectedUser != null)
                    {
                        //Try to retrieve the Affected User's Locale
                        //Affected User's Preference relationship objects
                        //https://github.com/SMLets/SMLets/blob/55f1bac3bc7a7011a461b24f6d7787ba89fe2624/SMLets.Shared/Code/EntityObjects.cs#L2775
                        //Get-SCSMRelatedObject -SMObject AffectedUser
                        EnterpriseManagementObject AffectedUserLocale = null;

                        //get the Affected User's locale
                        IList<EnterpriseManagementObject> AffectedUserPreferences = emg.EntityObjects.GetRelatedObjects<EnterpriseManagementObject>(AffectedUser.Id, TraversalDepth.OneLevel, ObjectQueryOptions.Default);
                        foreach (EnterpriseManagementObject preference in AffectedUserPreferences)
                        {
                            if (preference.GetClasses()[0].Name == "System.UserPreference.Localization")
                            {
                                AffectedUserLocale = preference;
                                break;
                            }
                        }

                        //check if the Affected User has a defined Locale, if they do and Azure Translate is enabled we can go ahead with a comparison.
                        if ((AffectedUserLocale != null) && (isAzureTranslateEnabled))
                        {
                            //get the Affected User's locale
                            string AffectedUserLocaleCode = AffectedUserLocale[(ManagementPackType)LocalizationClass, "LocaleID"].Value.ToString();

                            //get the Affected User's language code
                            //PowerShell equivalent:
                            //[System.Globalization.Cultureinfo]::GetCultureInfo(LocaleCode).TwoLetterISOLanguageName
                            string AffectedUserLangCode = System.Globalization.CultureInfo.GetCultureInfo(int.Parse(AffectedUserLocaleCode)).TwoLetterISOLanguageName.ToString().ToLower();

                            //now we need determine the language of the comment that was left
                            //first, grab the Comment object and it's actual comment that was left
                            //second, grab the person who entered the comment
                            string CommentText = CommentObject[(ManagementPackType)AnalystCommentClass, "Comment"].Value.ToString();
                            string EnteredBy = CommentObject[(ManagementPackType)AnalystCommentClass, "EnteredBy"].Value.ToString();
                            int CommentFirstSentencePeriod = CommentText.IndexOf(".");

                            //use a period to try to find the end of the first sentence to save on Azure spend
                            //second, make the call to the Azure Translate class in this project to Detect the language code of the comment
                            AzureCognitiveServicesTranslate languageManipulation = new AzureCognitiveServicesTranslate();
                            string CommentLangCode = null;
                            string CommentLangDetectSample = null;
                            int CommentLangDetectSamplePeriodIndex = CommentText.IndexOf(".");
                            try
                            {
                                CommentLangDetectSample = CommentText.Substring(0, CommentLangDetectSamplePeriodIndex);
                            }
                            catch
                            {
                                CommentLangDetectSample = CommentText;
                            }
                            if (CommentLangDetectSample.Length > 1)
                            {
                                CommentLangCode = languageManipulation.LanguageDetect(emg, CommentLangDetectSample);
                            }
                            else
                            {
                                CommentLangCode = languageManipulation.LanguageDetect(emg, CommentText);
                            }

                            //if the detected Comment Language is different than that of the Affected User then...
                            //translate, leave the translated comment on the Action Log, notify on the new translated entry
                            //this if statement would compare as seen below:
                            //if ("en" != "es")
                            if (CommentLangCode != AffectedUserLangCode)
                            {
                                //different languages
                                //translate with Azure
                                string translatedComment = languageManipulation.LanguageTranslate(emg, CommentLangCode, AffectedUserLangCode, CommentText);

                                //create a New translated Analyst Comment via projection/function from Rob Ford on TechNet
                                //https://social.technet.microsoft.com/Forums/WINDOWS/en-US/1f06e71c-00f4-4cf7-9f7e-a9a78b4b907c/creating-action-log-entry-for-incident-via-sdk-in-c?forum=customization
                                AddActionLogEntry newComment = new AddActionLogEntry();
                                string newCommentGuid = newComment.AddToActionLog(emg, WorkItem, translatedComment, EnteredBy, "AnalystCommentLog");

                                //notify on the new comment using the recently created comment's guid instead of the one that came in from the workflow parameter
                                NotifyUser notification = new NotifyUser();
                                notification.SendNotification(this.SubscriptionId, this.DataItems, newCommentGuid, new String[] { templateID }, new String[] { AffectedUser.Id.ToString() }, WIExecutionContext);
                            }
                            else
                            {
                                //same language
                                //notify on the original comment
                                NotifyUser notification = new NotifyUser();
                                notification.SendNotification(this.SubscriptionId, this.DataItems, this.InstanceIds[0].ToString(), new String[] { templateID }, new String[] { AffectedUser.Id.ToString() }, WIExecutionContext);
                            }
                        }
                        else
                        {
                            //the Affected user doesn't have a locale or Azure Translate isn't enabled, just notify them
                            //notify the User
                            NotifyUser notification = new NotifyUser();
                            notification.SendNotification(this.SubscriptionId, this.DataItems, this.InstanceIds[0].ToString(), new String[] { templateID }, new String[] { AffectedUser.Id.ToString() }, WIExecutionContext);
                        }
                    }
                    else
                    {
                        //the affected user doesn't exist, so there isn't anyone to notify
                    }
                }
                else if (CommentObject.GetLeastDerivedNonAbstractClass().Name == UserCommentClass.Name)
                {
                    //User Comment, notify Assigned

                    //Assigned User relationship object
                    IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> WorkItemRelUsers = emg.EntityObjects.GetRelationshipObjectsWhereSource<EnterpriseManagementObject>(WorkItem.Id, TraversalDepth.Recursive, ObjectQueryOptions.Default);
                    EnterpriseManagementObject AssignedUser = null;
                    foreach (EnterpriseManagementRelationshipObject<EnterpriseManagementObject> relObject in WorkItemRelUsers)
                    {
                        //if the relationship object has an ID that is the Assigned User and it isn't an old (isDeleted) user, then update the variable above with it
                        if ((relObject.RelationshipId == AssignedUserRelClass.Id) && (relObject.IsDeleted == false))
                        {
                            //we now have the relationship object that contains the Assigned User
                            AssignedUser = relObject.TargetObject;
                            break;
                        }
                    }

                    //if the Assigned User exists, try and retrieve their Locale
                    if (AssignedUser != null)
                    {
                        //Try to retrieve the Assigned User's Locale
                        //Affected User's Preference relationship objects
                        //https://github.com/SMLets/SMLets/blob/55f1bac3bc7a7011a461b24f6d7787ba89fe2624/SMLets.Shared/Code/EntityObjects.cs#L2775
                        //Get-SCSMRelatedObject -SMObject AffectedUser
                        EnterpriseManagementObject AssignedUserLocale = null;

                        //get the Assigned User's locale
                        IList<EnterpriseManagementObject> AssignedUserPreferences = emg.EntityObjects.GetRelatedObjects<EnterpriseManagementObject>(AssignedUser.Id, TraversalDepth.OneLevel, ObjectQueryOptions.Default);
                        foreach (EnterpriseManagementObject preference in AssignedUserPreferences)
                        {
                            if (preference.GetClasses()[0].Name == "System.UserPreference.Localization")
                            {
                                AssignedUserLocale = preference;
                                break;
                            }
                        }

                        //check if the Assigned User has a defined Locale, if they do and Azure Translate is enabled we can go ahead with a comparison.
                        if ((AssignedUserLocale != null) && (isAzureTranslateEnabled))
                        {
                            //get the Assigned User's locale
                            string AssignedUserLocaleCode = AssignedUserLocale[(ManagementPackType)LocalizationClass, "LocaleID"].Value.ToString();

                            //get the Assigned User's language code
                            //PowerShell equivalent:
                            //[System.Globalization.Cultureinfo]::GetCultureInfo(LocaleCode).TwoLetterISOLanguageName
                            string AssignedUserLangCode = System.Globalization.CultureInfo.GetCultureInfo(int.Parse(AssignedUserLocaleCode)).TwoLetterISOLanguageName.ToString().ToLower();

                            //now we need determine the language of the comment that was left
                            //first, grab the Comment object and it's actual comment that was left
                            //second, grab the person who entered the comment
                            string CommentText = CommentObject[(ManagementPackType)UserCommentClass, "Comment"].Value.ToString();
                            string EnteredBy = CommentObject[(ManagementPackType)UserCommentClass, "EnteredBy"].Value.ToString();
                            int CommentFirstSentencePeriod = CommentText.IndexOf(".");

                            //use a period to try to find the end of the first sentence to save on Azure spend
                            //second, make the call to the Azure Translate class in this project to Detect the language code of the comment
                            AzureCognitiveServicesTranslate languageManipulation = new AzureCognitiveServicesTranslate();
                            string CommentLangCode = null;
                            string CommentLangDetectSample = null;
                            int CommentLangDetectSamplePeriodIndex = CommentText.IndexOf(".");
                            try
                            {
                                CommentLangDetectSample = CommentText.Substring(0, CommentLangDetectSamplePeriodIndex);
                            }
                            catch
                            {
                                CommentLangDetectSample = CommentText;
                            }
                            if (CommentLangDetectSample.Length > 1)
                            {
                                CommentLangCode = languageManipulation.LanguageDetect(emg, CommentLangDetectSample);
                            }
                            else
                            {
                                CommentLangCode = languageManipulation.LanguageDetect(emg, CommentText);
                            }

                            //if the detected Comment Language is different than that of the Assigned User then...
                            //translate, leave the translated comment on the Action Log, notify on the new translated entry
                            //this if statement would compare as seen below:
                            //if ("en" != "es")
                            if (CommentLangCode != AssignedUserLangCode)
                            {
                                //different languages
                                //translate with Azure
                                string translatedComment = languageManipulation.LanguageTranslate(emg, CommentLangCode, AssignedUserLangCode, CommentText);

                                //create a New translated User Comment via projection/function from Rob Ford on TechNet
                                //https://social.technet.microsoft.com/Forums/WINDOWS/en-US/1f06e71c-00f4-4cf7-9f7e-a9a78b4b907c/creating-action-log-entry-for-incident-via-sdk-in-c?forum=customization
                                AddActionLogEntry newComment = new AddActionLogEntry();
                                string newCommentGuid = newComment.AddToActionLog(emg, WorkItem, translatedComment, EnteredBy, "UserCommentLog");

                                //notify on the new comment using the recently created comment's guid instead of the one that came in from the workflow parameter
                                NotifyUser notification = new NotifyUser();
                                notification.SendNotification(this.SubscriptionId, this.DataItems, newCommentGuid, new String[] { templateID }, new String[] { AssignedUser.Id.ToString() }, WIExecutionContext);
                            }
                            else
                            {
                                //same language
                                //notify on the original comment
                                NotifyUser notification = new NotifyUser();
                                notification.SendNotification(this.SubscriptionId, this.DataItems, this.InstanceIds[0].ToString(), new String[] { templateID }, new String[] { AssignedUser.Id.ToString() }, WIExecutionContext);
                            }
                        }
                        else
                        {
                            //the Affected user doesn't have a locale or Azure Translate isn't enabled, just notify them
                            //notify the User
                            NotifyUser notification = new NotifyUser();
                            notification.SendNotification(this.SubscriptionId, this.DataItems, this.InstanceIds[0].ToString(), new String[] { templateID }, new String[] { AssignedUser.Id.ToString() }, WIExecutionContext);
                        }
                    }
                    else
                    {
                        //the assigned user doesn't exist, so there isn't anyone to notify
                    }
                }
                else
                {
                    //this isn't a User comment or an Analyst comment
                }     
            }

            return base.Execute(WIExecutionContext);
        }
    }
}