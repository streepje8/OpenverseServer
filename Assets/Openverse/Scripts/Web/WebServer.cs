using System;
using System.Net;
using System.Text;
using Openverse.ScriptableObjects;
using UnityEngine;

public class WebServer : MonoBehaviour
{
    public HttpListener listener { get; private set; }
    public bool isActive = false;
    private string url = "http://localhost:8080";
    public static int pageViews = 0;
    public static int requestCount = 0;
    public static string pageData = 
        "<!DOCTYPE>" +
        "<html>" +
        "  <head>" +
        "    <title>HttpListener Example</title>" +
        "  </head>" +
        "  <body>" +
        "    <p>Page Views: {0}</p>" +
        "  </body>" +
        "</html>";
    private OpenverseSettings settings;
    public void StartServ(OpenverseSettings settings)
    {
        this.settings = settings;
        url = "http://localhost:" + settings.webServerPort + "/";
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Debug.Log("Webserver Started On Port: " + settings.webServerPort);
        HandleIncomingConnections();
    }

    public async void HandleIncomingConnections()
    {
        isActive = true;
        
        while (isActive)
        {
            HttpListenerContext ctx = await listener.GetContextAsync();
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;
            
            
            Debug.Log($"Request #: {++requestCount}");
            Debug.Log(req.Url.ToString());
            Debug.Log(req.HttpMethod);
            Debug.Log(req.UserHostName);
            Debug.Log(req.UserAgent);

            

            if (req.Url.AbsolutePath != "/favicon.ico")
                pageViews += 1;
            
            byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews));
            resp.ContentType = "text/html";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;

            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }
        
        listener.Close();
        listener = null;
    }
    
    public void StopServ()
    {
        isActive = false;
    }

    private void OnApplicationQuit() => StopServ();
}
