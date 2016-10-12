using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace HowHappyBot.Services
{
    public static class BotStateService
    {
        public static async Task<byte[]> GetByteArrayProperty(Activity activity, ConnectorClient connector, string name)
        {
            //get ref to conversation data
            StateClient stateClient = activity.GetStateClient();
            BotData conversationData = await stateClient.BotState.GetConversationDataAsync(activity.ChannelId, activity.From.Id);

            if (conversationData.GetProperty<byte[]>(name) != null)
            {
                var bytes = conversationData.GetProperty<byte[]>(name);
                return bytes;
            }
            else
            {
                return null;
            }
        }

        public static async Task<BotData> SaveAttachmentToConversation(Activity activity, ConnectorClient connector)
        {
            //get ref to conversation data
            StateClient stateClient = activity.GetStateClient();
            BotData conversationData = await stateClient.BotState.GetConversationDataAsync(activity.ChannelId, activity.From.Id);

            //get the first attachment
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
            return await stateClient.BotState.SetConversationDataAsync(activity.ChannelId, activity.From.Id, conversationData);
        }
    }
}