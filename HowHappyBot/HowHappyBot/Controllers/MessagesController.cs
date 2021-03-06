﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http.Headers;
using HowHappyBot.Services;
using HowHappyBot.Models;
using System.Collections.Generic;

namespace HowHappyBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {


        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            try
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    StateClient stateClient = activity.GetStateClient();
                    BotData conversationData = await stateClient.BotState.GetConversationDataAsync(activity.ChannelId, activity.From.Id);

                    // if there is an attachment, save it/overwrite the saved on and analyse it
                    if (activity.Attachments.Count > 0)
                    {
                        //save the attachment
                        await BotStateService.SaveAttachmentToConversation(activity, connector);
                    }

                    //check if we have a saved attachment to work with
                    var imageBytes = await BotStateService.GetByteArrayProperty(activity, connector, "image");

                    if (imageBytes != null)
                    {
                        //get face data
                        List<Face> faces = await EmotionAPIService.GetEmotionData(imageBytes);

                        // return our reply to the user
                        var roundedScore = Math.Round(faces.FirstOrDefault().scores.happiness, 2);
                        var scoreLabel = (roundedScore < 0.01) ?
                            "not" :
                            string.Format("{0:#%}", faces.FirstOrDefault().scores.happiness);
                        Activity replyJson = activity.CreateReply($"This face is {scoreLabel} happy");
                        await connector.Conversations.ReplyToActivityAsync(replyJson);

                        //send image back to users
                        var message = activity.CreateReply("");
                        message.Type = "message";

                        message.Attachments = new List<Attachment>();
                        var image = "data:image/png;base64," + Convert.ToBase64String(imageBytes);
                        message.Attachments.Add(new Attachment { ContentUrl = image, ContentType = "image/png" });
                        await connector.Conversations.ReplyToActivityAsync(message);
                    }
                    else
                    {
                        // return our reply to the user
                        Activity reply = activity.CreateReply($"No attachment. I can't work in these conditions!");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }

                }
                else
                {
                    HandleSystemMessage(activity);
                }
                var response = Request.CreateResponse(HttpStatusCode.OK);
                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                return response;
            }
        }



        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}