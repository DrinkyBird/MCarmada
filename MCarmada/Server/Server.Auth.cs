using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MCarmada.Server
{
	public partial class Server
	{
	    private string GenerateSalt(int length)
	    {
	        string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

	        string salt = "";
            Random rng = new Random();
            for (int i = 0; i < length; i++)
            {
                int index = rng.Next(0, chars.Length);
                salt += chars[index];
            }

	        return salt;
	    }

	    public bool AuthenticateClient(string playerName, string playerKey)
	    {
	        if (!Program.Instance.Settings.VerifyNames)
	        {
	            return true;
	        }

	        MD5 md5 = MD5.Create();
	        byte[] input = Encoding.ASCII.GetBytes(Salt + playerName);
	        byte[] hash = md5.ComputeHash(input);

	        StringBuilder sb = new StringBuilder();
	        for (int i = 0; i < hash.Length; i++)
	        {
	            sb.Append(hash[i].ToString("x2"));
	        }

	        string key = sb.ToString();

            logger.Info(key + " == " + playerKey);

	        return (key.ToLower().Equals(playerKey.ToLower()));
	    }
	}
}
