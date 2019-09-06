using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Advanced.Action.Log.Notifier
{
    public class AzureCognitiveServicesTranslate
    {
        //Detect a Language using Azure Translate
        //https://docs.microsoft.com/en-us/azure/cognitive-services/translator/tutorial-wpf-translation-csharp#detect-language-of-source-text
        public String LanguageDetect(EnterpriseManagementGroup emg, string text)
        {
            //Get the API key for Azure Translate from the Admin Settings MP
            EnterpriseManagementObject emoAdminSetting = emg.EntityObjects.GetObject<EnterpriseManagementObject>(new Guid("49a053e7-6080-e211-fd79-ca3607eecce7"), ObjectQueryOptions.Default);
            string ACSTranslateKey = emoAdminSetting[null, "ACSAPIKey"].Value.ToString();

            //site
            string ACSTranslateSite = "https://api.cognitive.microsofttranslator.com/detect?api-version=3.0";

            try
            {
                // Create request to Detect languages with Translator Text
                HttpWebRequest detectLanguageWebRequest = (HttpWebRequest)WebRequest.Create(ACSTranslateSite);
                detectLanguageWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", ACSTranslateKey);
                detectLanguageWebRequest.ContentType = "application/json; charset=utf-8";
                detectLanguageWebRequest.Method = "POST";

                // Send request
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                string jsonText = serializer.Serialize(text);

                string body = "[{ \"Text\": " + jsonText + " }]";
                byte[] data = Encoding.UTF8.GetBytes(body);

                detectLanguageWebRequest.ContentLength = data.Length;

                using (var requestStream = detectLanguageWebRequest.GetRequestStream())
                    requestStream.Write(data, 0, data.Length);

                HttpWebResponse response = (HttpWebResponse)detectLanguageWebRequest.GetResponse();

                // Read and parse JSON response
                var responseStream = response.GetResponseStream();
                var jsonString = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
                dynamic jsonResponse = serializer.DeserializeObject(jsonString);

                // Fish out the detected language code
                var languageInfo = jsonResponse[0];
                if (languageInfo["score"] > (decimal)0.5)
                {
                    return languageInfo["language"];
                }
                else
                {
                    return "Unable to confidently detect input language.";
                }
            }
            catch
            {
                return "Unable to contact Azure.";
            }
            
        }

        //Translate a Language
        //https://docs.microsoft.com/en-us/azure/cognitive-services/translator/quickstart-csharp-translate
        //modified to follow suit from above request
        public String LanguageTranslate(EnterpriseManagementGroup emg, string SourceLanguageCode, string TargetLanguageCode, string TextToTranslate)
        {
            //Get the API key for Azure Translate from the Admin Settings MP
            EnterpriseManagementObject emoAdminSetting = emg.EntityObjects.GetObject<EnterpriseManagementObject>(new Guid("49a053e7-6080-e211-fd79-ca3607eecce7"), ObjectQueryOptions.Default);
            string ACSTranslateKey = emoAdminSetting[null, "ACSAPIKey"].Value.ToString();

            //site
            //https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=$($SourceLanguage)&to=$($TargetLanguage)
            string ACSTranslateSite = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0";

            //prepare the text
            TextToTranslate = TextToTranslate.Trim();

            // send HTTP request to perform the translation
            string uri = string.Format(ACSTranslateSite + "&from={0}&to={1}", SourceLanguageCode, TargetLanguageCode);

            // Send request
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonText = serializer.Serialize(TextToTranslate);

            string body = "[{ \"Text\": " + jsonText + " }]";
            byte[] data = Encoding.UTF8.GetBytes(body);

            try
            {
                // Create request to Detect languages with Translator Text
                HttpWebRequest translateLanguageWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                translateLanguageWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", ACSTranslateKey);
                translateLanguageWebRequest.ContentType = "application/json; charset=utf-8";
                translateLanguageWebRequest.Method = "POST";

                translateLanguageWebRequest.ContentLength = data.Length;

                using (var requestStream = translateLanguageWebRequest.GetRequestStream())
                    requestStream.Write(data, 0, data.Length);

                HttpWebResponse response = (HttpWebResponse)translateLanguageWebRequest.GetResponse();

                // Read and parse JSON response
                var responseStream = response.GetResponseStream();
                var jsonString = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
                dynamic jsonResponse = serializer.DeserializeObject(jsonString);

                //Return the translated text
                var translation = jsonResponse[0];
                return translation["translations"][0]["text"];
            }
            catch
            {
                return "Unable to contact Azure.";
            }
        }
    }
}
