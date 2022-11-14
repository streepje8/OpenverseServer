using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using JetBrains.Annotations;
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
        public bool isActive;
        public OpenverseServerInfoResponse info;
        private FileHashes hashes;
        private string url = "http://localhost:8080";

        private OpenverseSettings openverseSettings;
        public void StartServ(OpenverseSettings settings)
        {
            openverseSettings = settings;
            url = "http://localhost:" + settings.webServerPort + "/";
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            info.OpenverseServerPort = settings.serverPort;
            info.OpenverseServerName = settings.serverName;
            info.ProtocolVersion = GlobalData.ProtocolVersion;
            info.IconURL = settings.iconURL;
            info.Description = settings.serverDescription;
            RefreshHashes();
            Debug.Log("(WEBSERVER) Webserver Started On Port: " + settings.webServerPort);
            HandleIncomingConnectionsAsync().
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

        private string GetHashFromManifest(string path)
        {
            foreach (string str in File.ReadAllLines(path))
            {
                string cropped = Regex.Replace(str,@"\s+","");
                if (cropped.Substring(0, 5).Equals("Hash:", StringComparison.OrdinalIgnoreCase))
                {
                    return cropped.Substring(5);
                }
            }

            return "NULL";
        }

        public async Task HandleIncomingConnectionsAsync()
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
                    await RespondJsonAsync(resp, this.info);
                    hasResponded = true;
                }
                
                if (req.Url.AbsolutePath.Equals("/hashes", StringComparison.OrdinalIgnoreCase))
                {
                    await RespondJsonAsync(resp, hashes);
                    hasResponded = true;
                }

                if (req.Url.AbsolutePath.ToLower().StartsWith("/file"))
                {
                    string file = req.Url.AbsolutePath.Replace("/file/", "").ToUpper();
                    string path = Directory.GetCurrentDirectory();
                    #if UNITY_EDITOR
                    path += "/Assets";
                    #endif
                    path += "/OpenverseBuilds";
                    switch (file)
                    {
                        case "CLIENTASSETS":
                            RespondFile(resp, path + "/clientassets");
                            hasResponded = true;
                            break;
                        case "CLIENTSCENE":
                            RespondFile(resp, path + "/clientscene");
                            hasResponded = true;
                            break;
                        case "OPENVERSEBUILDS":
                            RespondFile(resp, path + "/openversebuilds");
                            hasResponded = true;
                            break;
                    }
                }

                if (!hasResponded) await RespondStringAsync(resp, "Invalid Request", "text/html");
                resp.Close();
            }

            listener.Close();
            listener = null;
        }

        //Borrowed from https://stackoverflow.com/questions/13385633/serving-large-files-with-c-sharp-httplistener might have to be asynced later
        private void RespondFile(HttpListenerResponse resp, string testpath)
        {
            using (FileStream fs = File.OpenRead(testpath))
            {
                string filename = Path.GetFileName(testpath);
                resp.ContentLength64 = fs.Length;
                resp.SendChunked = false;
                resp.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                resp.AddHeader("Content-disposition", "attachment; filename=" + filename);

                byte[] buffer = new byte[64 * 1024];
                int read;
                using (BinaryWriter bw = new BinaryWriter(resp.OutputStream))
                {
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, read);
                        bw.Flush(); //seems to have no effect
                    }

                    bw.Close();
                }

                resp.StatusCode = (int)HttpStatusCode.OK;
                resp.StatusDescription = "OK";
            }
        }

        private async Task RespondJsonAsync(HttpListenerResponse resp,System.Object reply)
        {
            await RespondStringAsync(resp,JsonConvert.SerializeObject(reply,Formatting.Indented), "text/json");
        }

        private async Task RespondStringAsync(HttpListenerResponse resp, string reply, string ContentType)
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