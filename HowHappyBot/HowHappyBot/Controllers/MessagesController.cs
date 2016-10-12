using System;
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

namespace HowHappyBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        //_apiKey: Replace this with your own Cognitive Services Emotion API key, please do not use my key. I include it here so you can get up and running quickly but you can get your own key for free at https://www.projectoxford.ai/emotion 
        public const string _emotionApiKey = "1dd1f4e23a5743139399788aa30a7153";

        //_apiUrl: The base URL for the Emotion API. Find out what this is for other APIs via the API documentation
        public const string _emotionApiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient();
                BotData conversationData = await stateClient.BotState.GetConversationDataAsync(activity.ChannelId, activity.From.Id);

                // if there is an attachement, save it/overwrite the saved on and analyse it
                if (activity.Attachments.Count > 0)
                {
                    //get it
                    var sourceImage = await connector.HttpClient.GetStreamAsync(activity.Attachments.FirstOrDefault().ContentUrl);

                    //convert to byte array
                    byte[] imageBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        sourceImage.CopyTo(memoryStream);
                        imageBytes = memoryStream.ToArray();
                    }

                    //save it
                    conversationData.SetProperty<byte[]>("image", imageBytes);
                    await stateClient.BotState.SetConversationDataAsync(activity.ChannelId, activity.From.Id, conversationData);

                    // return our reply to the user
                    Activity replyThanks = activity.CreateReply($"Thanks for the attachment. I've saved it.");
                    await connector.Conversations.ReplyToActivityAsync(replyThanks);
                }

                //check if we have a saved attachement to work with
                if (conversationData.GetProperty<byte[]>("image") != null)
                {
                    //get saved attachment
                    var imageBytes = conversationData.GetProperty<byte[]>("image");

                    //convert saved bytes to a stream
                    var imageStream = new MemoryStream(imageBytes);

                    //call emotion api
                    using (var httpClient = new HttpClient())
                    {
                        //setup HttpClient with content
                        httpClient.BaseAddress = new Uri(_emotionApiUrl);
                        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _emotionApiKey);
                        var content = new StreamContent(imageStream);
                        content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                        //make request
                        var responseMessage = await httpClient.PostAsync(_emotionApiUrl, content);

                        //read response as a json string
                        var responseString = await responseMessage.Content.ReadAsStringAsync();

                        // return our reply to the user
                        Activity replyJson = activity.CreateReply($"JSON! {responseString}");
                        await connector.Conversations.ReplyToActivityAsync(replyJson);
                    }
                }
                else {
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