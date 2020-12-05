using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Moq;
using Servirtium.Core.Interactions;
using Xunit;

namespace Servirtium.Core.Tests.Interactions
{
    public class FindAndReplaceScriptWriterTest
    {
        private static readonly IInteraction BASE_INTERACTION = new ImmutableInteraction.Builder()
            .Number(666)
            .Method(HttpMethod.Put)
            .Path("/the/request/path")
            .RequestHeaders(new[] {("request-n-apples", "request-v-apples"),("request-n-bananas", "request-v-bananas")})
            .RequestBody("I like to eat, eat, eat apples and bananas", MediaTypeHeaderValue.Parse("application/json"))
            .StatusCode(HttpStatusCode.Created)
            .ResponseHeaders(new[] {("response-n-apples", "response-v-apples"),("response-n-bananas", "response-v-bananas")})
            .ResponseBody("You like to eat, eat, eat bananas and apples", MediaTypeHeaderValue.Parse("application/xml"))
            .Notes(new []
            {
                new IInteraction.Note(IInteraction.Note.NoteType.Text, "text note", "text content"),
                new IInteraction.Note(IInteraction.Note.NoteType.Code, "code note", "code content"),
            })
            .Build();
        
        private readonly Mock<IScriptWriter> _mockScriptWriter = new Mock<IScriptWriter>();
        
        private readonly Mock<TextWriter> _mockTextWriter = new Mock<TextWriter>();

        private IDictionary<int, IInteraction>? _delegatedInteractions;

        public FindAndReplaceScriptWriterTest()
        {
            _mockScriptWriter.Setup(w => w.Write(It.IsAny<TextWriter>(), It.IsAny<IDictionary<int, IInteraction>>()))
                .Callback<TextWriter, IDictionary<int, IInteraction>>((w, i) => _delegatedInteractions = i);
        }

        private static void AssertInteractionsEqual(IInteraction expected, IInteraction actual)
        {
            Assert.Equal(expected.Number, actual.Number);
            Assert.Equal(expected.Method, actual.Method);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.RequestHeaders.ToArray(), actual.RequestHeaders.ToArray());
            Assert.Equal(expected.RequestBody, actual.RequestBody);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.ResponseHeaders.ToArray(), actual.ResponseHeaders.ToArray());
            Assert.Equal(expected.ResponseBody, actual.ResponseBody);
            Assert.Equal(expected.Notes.Count(), actual.Notes.Count());
            for (var i=0; i<expected.Notes.Count(); i++)
            {
                var expectedNote = expected.Notes.ElementAt(i);
                var actualNote = expected.Notes.ElementAt(i);
                Assert.Equal(expectedNote.Type, actualNote.Type);
                Assert.Equal(expectedNote.Title, actualNote.Title);
                Assert.Equal(expectedNote.Content, actualNote.Content);
            }
            Assert.Equal(expected.Notes.Count(), actual.Notes.Count());
            
        }

        private static IEnumerable<RegexReplacement> ApplesAndBananasReplacements(string eat, string apples, string bananas, 
            ReplacementContext ctx = (ReplacementContext.RequestBody | ReplacementContext.RequestHeader | ReplacementContext.ResponseBody | ReplacementContext.ResponseHeader) ) =>
            new[]
            {
                new RegexReplacement(new Regex("eat"), eat, ctx),
                new RegexReplacement(new Regex("apples"), apples, ctx),
                new RegexReplacement(new Regex("bananas"), bananas, ctx)
            };

        //All the test cases for testing find & replace behaviours on a single interaction
        class SingleInteractionTestCases : TheoryData<IInteraction,
            IEnumerable<RegexReplacement>, IInteraction>
        {
            public SingleInteractionTestCases()
            {
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    new RegexReplacement[0],
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("ate", "aypples", "banaynays"),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .RequestHeaders(new[] {("request-n-aypples", "request-v-aypples"),("request-n-banaynays", "request-v-banaynays")})
                        .RequestBody("I like to ate, ate, ate aypples and banaynays", MediaTypeHeaderValue.Parse("application/json"))
                        .ResponseHeaders(new[] {("response-n-aypples", "response-v-aypples"),("response-n-banaynays", "response-v-banaynays")})
                        .ResponseBody("You like to ate, ate, ate banaynays and aypples", MediaTypeHeaderValue.Parse("application/xml"))
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("eet", "eepples", "baneenees", ReplacementContext.ResponseHeader),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .ResponseHeaders(new[] {("response-n-eepples", "response-v-eepples"),("response-n-baneenees", "response-v-baneenees")})
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("ite", "ipples", "bininis", ReplacementContext.ResponseBody),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .ResponseBody("You like to ite, ite, ite bininis and ipples", MediaTypeHeaderValue.Parse("application/xml"))
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("ote", "opples", "bononos", ReplacementContext.RequestHeader),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .RequestHeaders(new[] {("request-n-opples", "request-v-opples"),("request-n-bononos", "request-v-bononos")})
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("ute", "upples", "bununus", ReplacementContext.RequestBody),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .RequestBody("I like to ute, ute, ute upples and bununus", MediaTypeHeaderValue.Parse("application/json"))
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("ate", "aypples", "banaynays", ReplacementContext.None),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("ute", "upples", "bununus", ReplacementContext.RequestBody | ReplacementContext.ResponseBody),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .RequestBody("I like to ute, ute, ute upples and bununus", MediaTypeHeaderValue.Parse("application/json"))
                        .ResponseBody("You like to ute, ute, ute bununus and upples", MediaTypeHeaderValue.Parse("application/xml"))
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("ute", "upples", "bununus", ReplacementContext.RequestBody)
                        .Concat(ApplesAndBananasReplacements("eet", "eepples", "baneenees", ReplacementContext.ResponseBody)),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .RequestBody("I like to ute, ute, ute upples and bununus", MediaTypeHeaderValue.Parse("application/json"))
                        .ResponseBody("You like to eet, eet, eet baneenees and eepples", MediaTypeHeaderValue.Parse("application/xml"))
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    new [] { new RegexReplacement(new Regex(@"apples(\s*):(\s*)re"), "grapefruits: roo")},
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .RequestHeaders(new[] {("request-n-grapefruits", "rooquest-v-apples"),("request-n-bananas", "request-v-bananas")})
                        .ResponseHeaders(new[] {("response-n-grapefruits", "roosponse-v-apples"),("response-n-bananas", "response-v-bananas")})
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("eet", "eepples", "baneenees", ReplacementContext.ResponseBody)
                        .Concat(ApplesAndBananasReplacements("ite", "ipples", "bininis", ReplacementContext.ResponseBody)),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .ResponseBody("You like to eet, eet, eet baneenees and eepples", MediaTypeHeaderValue.Parse("application/xml"))
                        .Build());
                Add(new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .Build(),
                    ApplesAndBananasReplacements("eet", "eepples", "baneenees", ReplacementContext.ResponseBody)
                        .Append(new RegexReplacement(new Regex("eet"), "ite", ReplacementContext.ResponseBody)),
                    new ImmutableInteraction.Builder().From(BASE_INTERACTION)
                        .ResponseBody("You like to ite, ite, ite baneenees and eepples", MediaTypeHeaderValue.Parse("application/xml"))
                        .Build());
            }
        }

        [ClassData(typeof(SingleInteractionTestCases))]
        [Theory]
        public void Write_SingleInteraction_DelegatesToScriptWriterWithUpdatedInteraction(IInteraction input, IEnumerable<RegexReplacement> replacements, IInteraction output)
        {
            new FindAndReplaceScriptWriter(replacements, _mockScriptWriter.Object).Write(_mockTextWriter.Object, 
                new Dictionary<int, IInteraction>{{666, input}});
            _mockScriptWriter.Verify(sw=>sw.Write(
                _mockTextWriter.Object, 
                It.Is<IDictionary<int,IInteraction>>(d=>d.ContainsKey(666))));
            AssertInteractionsEqual(output, _delegatedInteractions![666]);
        }

        [Fact]
        public void Write_MultipleInteractions_AppliesAllReplaceLogicTooAllInteractions()
        {
            new FindAndReplaceScriptWriter(
                new []
                {
                    new RegexReplacement(new Regex("banana"), "spider", ReplacementContext.RequestBody),
                    new RegexReplacement(new Regex("tv show"), "movie", ReplacementContext.RequestBody),
                    new RegexReplacement(new Regex("apples"), "cheese", ReplacementContext.RequestBody),
                }, _mockScriptWriter.Object).Write(_mockTextWriter.Object, 
                new Dictionary<int, IInteraction>
                {
                    {666, BASE_INTERACTION},
                    {1337, new ImmutableInteraction.Builder()
                        .From(BASE_INTERACTION)
                        .RequestBody("my favourite tv show is bananaman", MediaTypeHeaderValue.Parse("text/plain"))
                        .Build()
                }
                });
            _mockScriptWriter.Verify(sw=>sw.Write(
                _mockTextWriter.Object, 
                It.Is<IDictionary<int,IInteraction>>(d=>d.ContainsKey(666) && d.ContainsKey(1337))));
            AssertInteractionsEqual(
                new ImmutableInteraction.Builder()
                    .From(BASE_INTERACTION)
                    .RequestBody("I like to eat, eat, eat cheese and spiders", MediaTypeHeaderValue.Parse("application/json"))
                    .Build(),
                _delegatedInteractions![666]);            AssertInteractionsEqual(
                new ImmutableInteraction.Builder()
                    .From(BASE_INTERACTION)
                    .RequestBody("my favourite movie is spiderman", MediaTypeHeaderValue.Parse("text/plain"))
                    .Build(),
                _delegatedInteractions![1337]);
        }
    }
}