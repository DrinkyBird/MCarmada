using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MCarmada.Server
{
    public partial class Server
    {
        private uint lastHeartbeat = 0;
        private void SendHeartbeat()
        {
            if (!Program.Instance.Settings.Broadcast)
            {
                return;
            }

            if (CurrentTick - lastHeartbeat < 45 * 20 && lastHeartbeat != 0)
            {
                return;
            }

            lastHeartbeat = CurrentTick;

            string html = string.Empty;
            string url = @"https://classicube.net/server/heartbeat/?";
            url += "name=" + Uri.EscapeDataString(ServerName) + "&";
            url += "port=" + port + "&";
            url += "users=" + GetOnlinePlayers() + "&";
            url += "max=" + players.Length + "&";
            url += "public=" + Program.Instance.Settings.Public + "&";
            url += "salt=" + Salt + "&";
            url += "software=" + Uri.EscapeDataString(Program.FullName) + "&";

            logger.Info("Sending heartbeat: " + url);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        html = reader.ReadToEnd();
                        logger.Error("Failed to heartbeat: " + html);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Failed to heartbeat: " + e);
                logger.Error("Will retry in 45 seconds.");
            }
        }
    }
}
