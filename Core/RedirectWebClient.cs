using System;
using System.Net;

namespace AssimilationSoftware.TodoSort.Core
{
    public class RedirectWebClient : WebClient
    {
        public Uri ResponseUri { get; private set; }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;
            ResponseUri = null;
            try
            {
                response = base.GetWebResponse(request);
                ResponseUri = response?.ResponseUri;
            }
            catch
            {
                // ignored
            }
            return response;
        }
    }
}
