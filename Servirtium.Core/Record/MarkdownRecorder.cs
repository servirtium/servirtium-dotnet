using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Servirtium.Core.Record
{
    public class MarkdownRecorder : IInteractionMonitor
    {
        private readonly IServiceInteroperation _service;
        private readonly IScriptWriter _scriptWriter;
        private readonly Uri _redirectHost;
        private readonly IDictionary<int, IInteraction> _allInteractions;
        private readonly string _targetFile;
        private int _currentInteraction = -1;

        public MarkdownRecorder(Uri redirectHost, string targetFile) : this(redirectHost, targetFile, new ServiceInteropViaSystemNetHttp(), new MarkdownScriptWriter()) { }

        public MarkdownRecorder(Uri redirectHost, string targetFile, IServiceInteroperation service, IScriptWriter scriptWriter, IDictionary<int, IInteraction>? interactions = null)
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
            var builder = new MarkdownInteraction.Builder()
                .From(interaction)
                .ResponseHeaders(response.Headers);
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
            _currentInteraction = interactionToRecord.Number;
            return response;
        }

        public IInteraction NewInteraction(int interactionNum, string context, string method, string path, string url)
        {
            throw new NotImplementedException();
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
