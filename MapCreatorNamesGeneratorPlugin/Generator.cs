using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.ParatextMapCreatorPlugin
{
	public class Generator
	{
		public const string tempHardcodedFilename = @"Map Creator\MapCreator.xml";
		private readonly XDocument doc;
		private readonly XElement dictionary;
		private readonly IEnumerable<XElement> translations;
		private readonly XAttribute targetLanguageAttribute;
		public string TargetLanguage => targetLanguageAttribute?.Value;
		public string Filename => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), tempHardcodedFilename);

		public Generator()
		{
			try
			{
				doc = XDocument.Load(Filename);
				dictionary = doc.Root?.Elements().SingleOrDefault(e => e.Name.LocalName == "Dictionary");
				string sourceLanguage = dictionary?.Attribute("SourceLanguage")?.Value;
				if (sourceLanguage != "en_US")
				{
					Error = "Source language must be English.";
					dictionary = null;
				}
				else
				{
					targetLanguageAttribute = dictionary.Attribute("TargetLanguage");
					translations = dictionary.Elements().Where(e => e.Name.LocalName == "Translation");
					if (!translations.Any())
					{
						Error = "No translation elements found.";
						translations = null;
					}
				}
			}
			catch (Exception e)
			{
				Error = e.Message;
			}
		}

		public string Error { get; }
		public bool CanGenerate => TargetLanguage != null && translations != null;
		public bool GenerationComplete { get; private set; }
		public int RenderingsSet { get; private set; }
		internal IEnumerable<Tuple<string, string>> AllTranslations => translations.Select(t =>
			new Tuple<string, string>(t.Attribute("Source")?.Value, t.Attribute("Target")?.Value));

		public void Generate(Func<string, IList<string>> getRenderings, string setLanguageId = null)
		{
			if (setLanguageId != null)
				targetLanguageAttribute.SetValue(setLanguageId);

			foreach (var translation in translations)
			{
				var source = translation.Attribute("Source")?.Value;
				var target = translation.Attribute("Target");

				if (source != null && target != null)
				{
					var renderings = getRenderings(source);
					if (renderings?.Count == 1)
					{
						target.Value = renderings.First();
						RenderingsSet++;
					}
				}
			}
			GenerationComplete = true;
		}

		public void Save()
		{
			doc.Save(Filename);
		}
	}
}
