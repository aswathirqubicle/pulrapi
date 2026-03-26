using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Core.Application.Constants.Http;

namespace Core.Application.Interfaces
{
    public interface IHttpClientService
    {
        Task<HttpResponseMessage> CreateRequest(HttpMethod httpMethod,
                                                             string url,
                                                             object requestBody,
                                                             Dictionary<string, string> formContent,
                                                             string bearerToken = null,
                                                             string contentType = ContentTypes.JSON,
                                                             List<KeyValuePair<string, string>> headers = null,
                                                             X509Certificate2 cert = null);
    }
}
