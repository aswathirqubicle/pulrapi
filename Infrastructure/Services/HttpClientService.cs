using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Core.Application.Constants.Http;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Infrastructure.Services;

namespace Core.Infrastructure.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly ILogger<HttpClientService> _logger;

        public HttpClientService(ILogger<HttpClientService> logger)
        {
            _logger = logger;
        }

        public async Task<HttpResponseMessage> CreateRequest(HttpMethod httpMethod,
                                                             string url,
                                                             object requestBody,
                                                             Dictionary<string, string> formContent,
                                                             string bearerToken = null,
                                                             string contentType = ContentTypes.JSON,
                                                             List<KeyValuePair<string, string>> headers = null,
                                                             X509Certificate2 cert = null)
        {

            try
            {

                using (var client = new HttpClient(GetHttpClientHandlerForCertificate(cert)))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                    if (!string.IsNullOrEmpty(bearerToken))
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    HttpRequestMessage request = new HttpRequestMessage()
                    {
                        Method = httpMethod,
                        RequestUri = new Uri(url),
                    };

                    if (requestBody != null || formContent != null)
                    {
                        request.Content = CreateRequestBodyByContentType(contentType, requestBody, formContent);
                    }

                    if (headers != null && headers.Count > 0)
                    {
                        foreach (var header in headers)
                        {
                            request.Headers.Add(header.Key, header.Value);
                        }
                    }

                    //TODO timeout increase (to be removed)
                    client.Timeout = TimeSpan.FromMinutes(10);

                    var response = await client.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var x = await response.Content.ReadAsStringAsync();

                        throw new NotFoundException("Status code: " + response.StatusCode.ToString() + " , Message:" + x);
                    }

                    return response;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new Exception("Http Client Service: An error occurred while creating the request.", e);
            }

        }

        private HttpContent CreateRequestBodyByContentType(string contentType, object requestBody, Dictionary<string, string> formContent)
        {
            if (contentType == ContentTypes.JSON)
            {
                var requestBodySerialized = JsonConvert.SerializeObject(requestBody);
                return new StringContent(requestBodySerialized, Encoding.UTF8, contentType);
            }

            if (contentType == ContentTypes.XML)
            {
                var requestBodySerialized = XmlSerializeHelper.SerializeObject(requestBody);
                return new StringContent(requestBodySerialized, Encoding.UTF8, contentType);
            }

            if (contentType == ContentTypes.FormUrlEncoded)
            {
                return new FormUrlEncodedContent(formContent);
            }

            throw new NotFoundException($"Http Client Service: Content type '{contentType}' doesn't exist.");
        }

        private HttpClientHandler GetHttpClientHandlerForCertificate(X509Certificate2 cert)
        {
            var handler = new HttpClientHandler();
            if (cert != null)
            {
                handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);
            }

            return handler;
        }

    }
}
