using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advanced.Action.Log.Notifier
{
    public class AddActionLogEntry
    {
        //Modified from Anton Gritsenko/Rob Ford on TechNet
        //https://social.technet.microsoft.com/Forums/WINDOWS/en-US/1f06e71c-00f4-4cf7-9f7e-a9a78b4b907c/creating-action-log-entry-for-incident-via-sdk-in-c?forum=customization

        //Follows similiar SMLets Exchange Connector logic
        //https://github.com/AdhocAdam/smletsexchangeconnector/blob/5e488ed24ff9467e0a8691df56cc993eabfb9c7c/smletsExchangeConnector.ps1#L1876

        public String AddToActionLog(EnterpriseManagementGroup emg, EnterpriseManagementObject WorkItem, string Comment, string User, string CommentType)
        {
            try
            {
                //Get the System.WorkItem.Library mp
                ManagementPack mpWorkItemLibrary = emg.ManagementPacks.GetManagementPack(new Guid("405D5590-B45F-1C97-024F-24338290453E"));

                //Get the Action Log class, only 1 of 2 could be incoming - User Comments or Analyst Comments
                /*
                    ManagementPackClass CommentLogType = emg.EntityTypes.GetClass("System.WorkItem.TroubleTicket.AnalystCommentLog", mpWorkItemLibrary); //UserCommentLog
                    ManagementPackClass UserCommentLog = emg.EntityTypes.GetClass("System.WorkItem.TroubleTicket.UserCommentLog", mpWorkItemLibrary); //AnalystComments
                */
                string commentClassName = "System.WorkItem.TroubleTicket." + CommentType;
                ManagementPackClass CommentLogType = emg.EntityTypes.GetClass(commentClassName, mpWorkItemLibrary);

                //Create a new action log entry
                CreatableEnterpriseManagementObject objectActionLog = new CreatableEnterpriseManagementObject(emg, CommentLogType);

                //Check description
                Comment += "\n";
                if (Comment.Length > 4000) Comment = Comment.Substring(0, 4000);

                //Setup the action log entry
                Guid NewCommentGuid = Guid.NewGuid();
                objectActionLog[CommentLogType, "Id"].Value = NewCommentGuid.ToString();
                objectActionLog[CommentLogType, "DisplayName"].Value = NewCommentGuid.ToString();
                objectActionLog[CommentLogType, "Comment"].Value = Comment;
                objectActionLog[CommentLogType, "EnteredBy"].Value = "Azure Translate/" + User;
                objectActionLog[CommentLogType, "EnteredDate"].Value = DateTime.Now.ToUniversalTime();

                //Get the enumeration and relationship
                ManagementPackRelationship wiActionLogRel = emg.EntityTypes.GetRelationshipClass("System.WorkItemHasCommentLog", mpWorkItemLibrary);
                ManagementPackRelationship userActionLogRel = emg.EntityTypes.GetRelationshipClass("System.WorkItem.TroubleTicketHasUserComment", mpWorkItemLibrary);
                ManagementPackRelationship analystActionLogRel = emg.EntityTypes.GetRelationshipClass("System.WorkItem.TroubleTicketHasAnalystComment", mpWorkItemLibrary);

                //Get the projection for the incident
                EnterpriseManagementObjectProjection emopWorkItem = new EnterpriseManagementObjectProjection(WorkItem);

                //Add the comment and save
                ManagementPackRelationship WorkItemActionLogRelationship = null;
                switch (CommentType)
                {
                    case "UserCommentLog":
                        {WorkItemActionLogRelationship = userActionLogRel;}
                        break;
                    case "AnalystCommentLog":
                        {WorkItemActionLogRelationship = analystActionLogRel;
                        objectActionLog[CommentLogType, "IsPrivate"].Value = false;}
                        break;
                }

                //change the relationship if the work item is a Service Request
                if (WorkItem.LeastDerivedNonAbstractManagementPackClassId.ToString() == "04b69835-6343-4de2-4b19-6be08c612989")
                {
                    WorkItemActionLogRelationship = wiActionLogRel;
                }

                //write the new comment into the Action Log
                emopWorkItem.Add(objectActionLog, WorkItemActionLogRelationship.Target);
                emopWorkItem.Commit();

                //return the new Comment Guid to notify on
                return objectActionLog.Id.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}