using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Cityline.Client.Tests")]

namespace Cityline.Client
{
    public class CitylineClient : EventEmitter, IDisposable
    {
        internal Uri _serverUrl;
        private int errorCount = 0;
        private readonly CancellationTokenSource _internalTokenSource = new CancellationTokenSource();
        private readonly Action<HttpRequestMessage> _messageModifier;
        private readonly HttpClient _httpClient;
        private readonly IDictionary<string, Frame> _frames = new Dictionary<string, Frame>();
        private readonly IDictionary<string, string> _idCache = new Dictionary<string, string>();
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.None };
        
        public CitylineClient(Uri serverUrl, Func<HttpClient> httpClientFactory = null, Action<HttpRequestMessage> messageModifier = null)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
            _messageModifier = messageModifier ?? new Action<HttpRequestMessage>((message) => {});

            var factory = httpClientFactory ?? new Func<HttpClient>(() => new HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite) });
            _httpClient = factory.Invoke();
        }

        public void Dispose()
        {
            _internalTokenSource?.Dispose();
            _httpClient?.Dispose();
        }

        private async Task HandleUnsuccessfullResponse(HttpResponseMessage response) 
        {
             switch(response.StatusCode) {

                case HttpStatusCode.NotFound:
                    throw new CitylineClientException($"Server not found, please ensure that url is correct: {_serverUrl}");

                case HttpStatusCode.InternalServerError:
                    errorCount++;

                    if (errorCount > 3) 
                        throw new CitylineClientException("Aborting, Server returned error more than 3 times");

                    await Task.Delay(RetryOnErrorDelay);                
                    break;
                default:
                    throw new CitylineClientException($"Unhandled status resceived from server: {response.StatusCode}, {response.ReasonPhrase}");
            }
        }

        // var handler = new HttpClientHandler
        // {
        //     ClientCertificateOptions = ClientCertificateOption.Manual,
        //     ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
        // };

        public TimeSpan RetryOnErrorDelay { get; set;} = TimeSpan.FromSeconds(5);
        
        public async Task StartListening(CancellationToken externalToken = default(CancellationToken))
        {
            using (CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_internalTokenSource.Token, externalToken)) 
            {
                while(!linkedToken.IsCancellationRequested) 
                {
                    var buffer = new Buffer();
                    var json = JsonConvert.SerializeObject(new { tickets = _idCache}, settings);
    
                    using (var message = new HttpRequestMessage(HttpMethod.Post, this._serverUrl))
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        _messageModifier.Invoke(message);
                        message.Content = content;
                        using (var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead))
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            if (!response.IsSuccessStatusCode) 
                            {
                                await HandleUnsuccessfullResponse(response);
                                continue;  
                            }

                            this.errorCount = 0;

                            while (!reader.EndOfStream && !linkedToken.IsCancellationRequested)
                            {
                                buffer.Add(reader.ReadLine());     

                                while (buffer.HasTerminator()) 
                                {
                                    var chunk = buffer.Take();
                                    var frame = Frame.Parse(chunk);
                                    AddFrame(frame);
                                }                        
                            }
                        }
                    }
                }
            }
        }

        private void AddFrame(Frame frame) 
        {
            if (frame == null)
                return;
            
            if (string.IsNullOrWhiteSpace(frame.EventName))
                return;

            if (string.IsNullOrWhiteSpace(frame.Id))
                return;

            _idCache[frame.EventName] = frame.Id;
            _frames[frame.EventName] = frame;
            Emit(frame.EventName, frame);
        }
    }    
}
