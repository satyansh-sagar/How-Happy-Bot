using HowHappyBot.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HowHappyBot.Services
{
    public static class EmotionAPIService
    {
        //_apiKey: Replace this with your own Cognitive Services Emotion API key, please do not use my key. I include it here so you can get up and running quickly but you can get your own key for free at https://www.projectoxford.ai/emotion 
        public const string _emotionApiKey = "1dd1f4e23a5743139399788aa30a7153";

        //_apiUrl: The base URL for the Emotion API. Find out what this is for other APIs via the API documentation
        public const string _emotionApiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        public static async Task<List<Face>> GetEmotionData(byte[] sourceImage)
        {
            //convert saved bytes to a stream
            var imageStream = new MemoryStream(sourceImage);

            //call emotion api
            var responseString = string.Empty;
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
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            //initialise faces
            var faces = new List<Face>();

            //parse json string to object and enumerate
            var responseArray = JArray.Parse(responseString);
            foreach (var faceResponse in responseArray)
            {
                //deserialise json to face
                var face = JsonConvert.DeserializeObject<Face>(faceResponse.ToString());

                //add face to faces list
                faces.Add(face);
            }

            //return
            return faces;
        }
    }
}
