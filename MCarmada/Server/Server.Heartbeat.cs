using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MCarmada.Server
{
    partial class Server
    {
        private uint lastHeartbeat = 0;
        private void SendHeartbeat()
        {
            if (CurrentTick - lastHeartbeat < 45 * 20 && lastHeartbeat != 0)
            {
                return;
            }

            lastHeartbeat = CurrentTick;

            string html = string.Empty;
            string url = @"https://classicube.net/server/heartbeat/?";
            url += "name=MCarmada+Test&";
            url += "port=25565&";
            url += "users=" + GetOnlinePlayers() + "&";
            url += "max=" + players.Length + "&";
            url += "public=true&";
            url += "salt=" + Salt + "&";
            url += "public=true&";

            logger.Info("Sending heartbeat: " + url);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            Console.WriteLine(html);
        }
    }
}
