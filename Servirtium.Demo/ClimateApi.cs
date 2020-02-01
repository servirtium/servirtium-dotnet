using Servirtium.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Servirtium.Demo
{ 
    internal class ClimateApi
    {
        internal static readonly Uri DEFAULT_SITE = new Uri("http://climatedataapi.worldbank.org");

        private readonly Uri _site;
        public ClimateApi(): this(DEFAULT_SITE) { }
        public ClimateApi(Uri site)
        {
            _site = site;
        }

        public async Task<double> getAveAnnualRainfall(int fromCCYY, int toCCYY, params string[] countryIsos)
        {
            HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            IEnumerable<Task<double>> avgTempPerCountry = countryIsos.Select(async countryIso =>
            {
                var requestUri = new Uri(_site, $"/climateweb/rest/v1/country/annualavg/pr/{fromCCYY}/{toCCYY}/{countryIso}.xml");
                var response = await httpClient.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    var rawXml = await response.Content.ReadAsStringAsync();
                    if (rawXml.Contains("Invalid country code. Three letters are required"))
                    {
                        throw new Exception($"{countryIso} not recognized by climateweb");
                    }
                    var doc = XDocument.Parse(rawXml);
                    var result = doc.Descendants(XNamespace.None + "annualData")
                        .Descendants(XNamespace.None + "double")
                        .Select(xe => Double.Parse(xe.Value));
                    if (!result.Any())
                    {
                        throw new Exception($"date range {fromCCYY}-{toCCYY} not supported");
                    }
                    return result.Average();
                }
                else throw new HttpRequestException($"GET Request to {requestUri} failed, status {response.StatusCode}, Content: {Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
            });
            //'Average of averages' logic replicates https://github.com/servirtium/demo-java-climate-data-tck/blob/master/src/main/java/com/paulhammant/climatedata/ClimateApi.java
            List<double> averages = new List<double>();
            foreach(var calculateAverage in avgTempPerCountry)
            {
                averages.Add(await calculateAverage);
            }
            return averages.Average();

        }
    }
}