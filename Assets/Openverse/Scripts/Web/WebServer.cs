using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Openverse.Data;

namespace Openverse.Web
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Text;
    using ScriptableObjects;
    using UnityEngine;

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct OpenverseServerInfoResponse
    {
        public string OpenverseServerName;
        public ushort OpenverseServerPort;
        public string ProtocolVersion;
        public string IconURL;
        public string Description;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct FileHashes
    {
        public string ClientAssets;
        public string ClientScene;
        public string ServerScene;
        public string OpenverseBuilds;
    }

    public class WebServer : MonoBehaviour
    {
        public HttpListener listener { get; private set; }
        public bool isActive = false;
        public OpenverseServerInfoResponse response = new OpenverseServerInfoResponse();
        private FileHashes hashes = new FileHashes();
        private string url = "http://localhost:8080";

        private OpenverseSettings openverseSettings;
        public void StartServ(OpenverseSettings settings)
        {
            openverseSettings = settings;
            url = "http://localhost:" + settings.webServerPort + "/";
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            response.OpenverseServerPort = settings.serverPort;
            response.OpenverseServerName = settings.serverName;
            response.ProtocolVersion = GlobalData.ProtocolVersion;
            response.IconURL = settings.iconURL;
            response.Description = settings.serverDescription;
            RefreshHashes();
            Debug.Log("(WEBSERVER) Webserver Started On Port: " + settings.webServerPort);
            HandleIncomingConnections().
                ContinueWith(t => Debug.LogException(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        public void RefreshHashes()
        {
            string path = Directory.GetCurrentDirectory();
            #if UNITY_EDITOR
            path += "/Assets";
            #endif
            path += "/OpenverseBuilds";
            hashes.ClientAssets = GetHashFromManifest(path + "/clientassets.manifest");
            hashes.ClientScene = GetHashFromManifest(path + "/clientscene.manifest");
            hashes.OpenverseBuilds = GetHashFromManifest(path + "/openversebuilds.manifest");
            hashes.ServerScene = GetHashFromManifest(path + "/serverscene.manifest");
        }

        public string GetHashFromManifest(string path)
        {
            foreach (string str in File.ReadAllLines(path))
            {
                string cropped = Regex.Replace(str,@"\s+","");
                if (cropped.Substring(0, 5).Equals("Hash:", StringComparison.OrdinalIgnoreCase))
                {
                    return cropped.Substring(5);
                }
            }

            return null;
        }

            public async Task HandleIncomingConnections()
        {
            isActive = true;

            while (isActive)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                
                //Temp for debugging
                Debug.Log(req.Url.ToString());
                Debug.Log(req.HttpMethod);
                Debug.Log(req.UserHostName);
                Debug.Log(req.UserAgent);

                bool hasResponded = false;

                if (req.Url.AbsolutePath.Equals("/", StringComparison.OrdinalIgnoreCase))
                {
                    await RespondJson(resp, response);
                    hasResponded = true;
                }
                if (req.Url.AbsolutePath.Equals("/hashes", StringComparison.OrdinalIgnoreCase))
                {
                    await RespondJson(resp, hashes);
                    hasResponded = true;
                }

                if (!hasResponded)
                {
                    await RespondString(resp, "Invalid Request", "text/html");
                }
                resp.Close();
            }

            listener.Close();
            listener = null;
        }

        private async Task RespondJson(HttpListenerResponse resp,System.Object reply)
        {
            await RespondString(resp,JsonConvert.SerializeObject(reply,Formatting.Indented), "text/json");
        }

        private async Task RespondString(HttpListenerResponse resp, string reply, string ContentType)
        {
            byte[] data = Encoding.UTF8.GetBytes(reply);
            resp.ContentType = ContentType;
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;

            await resp.OutputStream.WriteAsync(data, 0, data.Length);
        }

            public void StopServ()
        {
            isActive = false;
        }

        private void OnApplicationQuit() => StopServ();
    }
}