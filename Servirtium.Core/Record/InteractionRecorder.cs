using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Record
{
    public class InteractionRecorder : IInteractionMonitor
    {
        private readonly IServiceInteroperation _service;
        private readonly IScriptWriter _scriptWriter;
        private readonly Uri _redirectHost;
        private readonly IDictionary<int, IInteraction> _allInteractions;
        private readonly string _targetFile;

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter) : this(redirectHost, targetFile, scriptWriter, new ServiceInteropViaSystemNetHttp()) { }

        public InteractionRecorder(Uri redirectHost, string targetFile, IScriptWriter scriptWriter, IServiceInteroperation service, IDictionary<int, IInteraction>? interactions = null)
        {
            _redirectHost = redirectHost;
            _service = service;
            _scriptWriter = scriptWriter;
            _allInteractions = interactions ?? new Dictionary<int, IInteraction> { };
            _targetFile = targetFile;
        }

        public async Task<ServiceResponse> GetServiceResponseForRequest(Uri host, IInteraction interaction, bool lowerCaseHeaders)
        {
            var response = await _service.InvokeServiceEndpoint(
                interaction.Method, null, null,
                new Uri($"{_redirectHost.GetLeftPart(UriPartial.Authority)}{interaction.Path}"),
                interaction.RequestHeaders);
            var builder = new ImmutableInteraction.Builder()
                .From(interaction)
                .ResponseHeaders(response.Headers)
                .StatusCode(response.StatusCode);

            if (response.Body != null && response.ContentType != null)
            {
                builder.ResponseBody(response.Body.ToString(), response.ContentType);
            }
            else
            {
                builder.RemoveResponseBody();
            }
            var interactionToRecord = builder.Build();
            _allInteractions[interactionToRecord.Number] = interactionToRecord;
            return response;
        }

        public void NoteCompletedInteraction(IInteraction requestInteraction, ServiceResponse responseFromService) 
        {
            var builder = new ImmutableInteraction.Builder()
             .From(requestInteraction)
             .ResponseHeaders(responseFromService.Headers)
             .StatusCode(responseFromService.StatusCode);

            if (responseFromService.Body != null && responseFromService.ContentType != null)
            {
                builder.ResponseBody(responseFromService.Body.ToString(), responseFromService.ContentType);
            }
            else
            {
                builder.RemoveResponseBody();
            }
            var interactionToRecord = builder.Build();
            _allInteractions[interactionToRecord.Number] = interactionToRecord;
        }

        public void FinishedScript(int interactionNum, bool failed)
        {
      
            using (var fs = File.CreateText(_targetFile))
            {
                _scriptWriter.Write(fs, _allInteractions);
            }
        }
    }
}
