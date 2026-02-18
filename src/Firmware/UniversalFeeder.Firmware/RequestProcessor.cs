using System;
using System.Collections;

namespace UniversalFeeder.Firmware
{
    public static class RequestProcessor
    {
        public static Hashtable ParseQueryString(string url)
        {
            var queryParams = new Hashtable();
            int queryStart = url.IndexOf('?');
            if (queryStart == -1) return queryParams;

            string query = url.Substring(queryStart + 1);
            string[] pairs = query.Split('&');

            foreach (string pair in pairs)
            {
                string[] kvp = pair.Split('=');
                if (kvp.Length == 2)
                {
                    queryParams.Add(kvp[0], kvp[1]);
                }
            }

            return queryParams;
        }
    }
}
