using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spire.Doc;
using Document = Spire.Doc.Document;
using Spire.Doc.Documents;
using Section = Spire.Doc.Section;
using Spire.Doc.Fields;

namespace FontTest1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("pdf")]
        public Stream GeneratePDF()
        {
            var stream = System.IO.File.Open("CertificateTemplates/Docs/template.docx", FileMode.Open);
            return GenerateCertificatePdf(stream, new Dictionary<string, object> { ["{{Title}}"] = "まはしく" });
        }

        private Stream GenerateCertificatePdf(Stream template, IDictionary<string, object> fieldValuePairs)
        {
            using (var document = new Document(template))
            {
                bool isDisplayAlternate = fieldValuePairs.Where(kvp => kvp.Key.Contains("isDisplayAlternate")).Any(key => (string)key.Value == "1");
                if (!isDisplayAlternate)
                {
                    TextSelection selection = document.FindString("AlternateName", true, true);
                    if (selection != null)
                    {
                        Paragraph paragraph = selection.GetAsOneRange().OwnerParagraph;
                        Section section = paragraph.OwnerTextBody.Owner as Section;
                        section.Paragraphs.Remove(paragraph);
                    }
                }
                foreach (var keyValuePair in fieldValuePairs)
                {
                    switch (keyValuePair.Value)
                    {
                        case string newValue:
                            if (keyValuePair.Key.Contains("AlternateName"))
                            {
                                TextSelection selection = document.FindString("AlternateName", false, true);
                                if (selection != null)
                                {
                                    TextRange r = selection.GetAsOneRange();
                                    Paragraph paragraph = r.OwnerParagraph;

                                    paragraph.ChildObjects.Clear();

                                    TextRange newTextRange = new TextRange(document);
                                    newTextRange.Text = newValue;
                                    document.EmbedFontsInFile = true;
                                    string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CertificateTemplates/Fonts/ARIALUNI.TTF");
                                    if (System.IO.File.Exists(fontPath))
                                    {
                                        document.PrivateFontList.Add(new PrivateFontPath("Arial Unicode MS", fontPath));
                                        newTextRange.CharacterFormat.FontName = "Arial Unicode MS";
                                    }
                                    paragraph.ChildObjects.Add(newTextRange);



                                }
                            }
                            else
                            {
                                document.Replace(keyValuePair.Key, newValue, caseSensitive: false, wholeWord: true);
                            }
                            break;

                        default:
                            throw new NotImplementedException($"{keyValuePair.Value.GetType().FullName} = {keyValuePair.Value}");
                    }
                }

                var output = new MemoryStream();

                document.JPEGQuality = 100;
                document.SaveToStream(output, Spire.Doc.FileFormat.PDF);

                output.Seek(0, SeekOrigin.Begin);

                return output;
            }
        }
    }
}
