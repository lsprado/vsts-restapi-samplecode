﻿using System;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.Common;
using System.Collections.Generic;

namespace VstsClientLibrariesSamples.WorkItemTracking
{
    public class Sample
    {
        readonly IConfiguration _configuration;
        private VssBasicCredential _credentials;
        private Uri _uri;
        
        public Sample(IConfiguration configuration)
        {
            _configuration = configuration;
            _credentials = new VssBasicCredential("", _configuration.PersonalAccessToken);
            _uri = new Uri(_configuration.UriString);
        }

        public string CreateBug()
        {
            string project = _configuration.Project;
            
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            //add fields to your patch document
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = "Authorization Errors"
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.TCM.ReproSteps",
                    Value = "Our authorization logic needs to allow for users with Microsoft accounts (formerly Live Ids) - http://msdn.microsoft.com/en-us/library/live/hh826547.aspx"
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.Priority",
                    Value = "1"
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.Severity",
                    Value = "2 - High"
                }
            );

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(_uri, _credentials))
            {
                //create the bug
                WorkItem result = workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, project, "Bug").Result;
            }

            patchDocument = null;

            return "success";
        }

        public string UpdateBug()
        {
            var id = _configuration.WorkItemId;

            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.History",
                    Value = "Tracking that we changed the priority and severity of this bug to high"
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.Priority",
                    Value = "1"
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.Severity",
                    Value = "1 - Critical"
                }
            );

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(_uri, _credentials))
            {
                WorkItem result = workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, id).Result;
            }

            patchDocument = null;

            return "success";
        }
        
        public string AddCommentsToBug()
        {
            var id = _configuration.WorkItemId;

            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.History",
                    Value = "Adding 'hello world' comment to this bug"
                }
            );
            
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(_uri, _credentials))
            {
                WorkItem result = workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, id).Result;
            }

            patchDocument = null;

            return "success";
        }

        public string QueryWorkItems_Query()
        {
            string project = _configuration.Project;
            string query = _configuration.Query;

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(_uri, _credentials))
            {
                QueryHierarchyItem queryItem;

                try
                {
                    //get the query object based on the query name and project
                    queryItem = workItemTrackingHttpClient.GetQueryAsync(project, query).Result;                    
                }                
                catch (Exception ex)
                {
                    return ex.InnerException.Message;
                }             
               
                //now we have the query id, so lets execute the query and get the results
                WorkItemQueryResult workItemQueryResult = workItemTrackingHttpClient.QueryByIdAsync(queryItem.Id).Result;
                
                //some error handling                
                if (workItemQueryResult == null)
                {
                    return "failure";
                }                  
                else if (workItemQueryResult.WorkItems.Count() == 0)
                {
                    return "no records found for query '" + query + "'";
                } 
                else 
                {
                    //need to get the list of our work item id's and put them into an array
                    List<int> list = new List<int>();
                    foreach (var item in workItemQueryResult.WorkItems)
                    {
                        list.Add(item.Id);
                    }
                    int[] arr = list.ToArray();

                    //build a list of the fields we want to see
                    string[] fields = new string[3];
                    fields[0] = "System.Id";
                    fields[1] = "System.Title";
                    fields[2] = "System.State";

                    var workItems = workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf).Result;

                    return "success";
                }                              
            }
        }

        public string QueryWorkItems_Wiql()
        {
            string project = _configuration.Project;

            //create a query to get your list of work items needed
            Wiql wiql = new Wiql()
            {
                Query = "Select [State], [Title] " +
                        "From WorkItems " +
                        "Where [Work Item Type] = 'Bug' " +
                        "And [System.TeamProject] = '" + project + "' " +
                        "And [System.State] = 'New' " +
                        "Order By [State] Asc, [Changed Date] Desc"
            };

            //create instance of work item tracking http client
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(_uri, _credentials))
            {
                //execute the query
                WorkItemQueryResult workItemQueryResult = workItemTrackingHttpClient.QueryByWiqlAsync(wiql).Result;

                //check to make sure we have some results
                if (workItemQueryResult == null || workItemQueryResult.WorkItems.Count() == 0)
                {
                    return "Wiql '" + wiql.Query + "' did not find any results";
                }
                else
                {
                    //need to get the list of our work item id's and put them into an array
                    List<int> list = new List<int>();
                    foreach (var item in workItemQueryResult.WorkItems)
                    {
                        list.Add(item.Id);
                    }
                    int[] arr = list.ToArray();

                    //build a list of the fields we want to see
                    string[] fields = new string[3];
                    fields[0] = "System.Id";
                    fields[1] = "System.Title";
                    fields[2] = "System.State";

                    var workItems = workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf).Result;
                    return "success";
                }
            }
        }

        public string QueryAndUpdateWorkItems()
        {
            string project = _configuration.Project;

            //create a query to get your list of work items needed
            Wiql wiql = new Wiql()
            {
                Query = "Select [State], [Title] " +
                        "From WorkItems " +
                        "Where [Work Item Type] = 'Bug' " +
                        "And [System.TeamProject] = '" + project + "' " +
                        "And [System.State] = 'New' " +
                        "Order By [State] Asc, [Changed Date] Desc"
            };


            //create a patchDocument that is used to update the work items
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            //change the backlog priority
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.BacklogPriority",
                    Value = "2",
                    From = _configuration.Identity
                }
            );

            //move the state to active
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.State",
                    Value = "Active",
                    From = _configuration.Identity
                }
            );

            //add some comments
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.History",
                    Value = "comment from client lib sample code",
                    From = _configuration.Identity
                }
            );

            //create instance of work item tracking http client
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(_uri, _credentials))
            {
                //execute the query
                WorkItemQueryResult workItemQueryResult = workItemTrackingHttpClient.QueryByWiqlAsync(wiql).Result;

                //check to make sure we have some results
                if (workItemQueryResult == null || workItemQueryResult.WorkItems.Count() == 0)
                {                    
                    throw new NullReferenceException("Wiql '" + wiql.Query + "' did not find any results");                   
                }

                //loop thru the work item results and update each work item
                foreach (WorkItemReference workItemReference in workItemQueryResult.WorkItems)
                {
                    WorkItem result = workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, workItemReference.Id).Result;
                }
            }

            wiql = null;
            patchDocument = null;

            return "success";           
        }
    }
}