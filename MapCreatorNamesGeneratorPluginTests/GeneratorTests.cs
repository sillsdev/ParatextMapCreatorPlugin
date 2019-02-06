using System.Linq;
using NUnit.Framework;
using SIL.ParatextMapCreatorPlugin;

namespace MapCreatorNamesGeneratorPlugin.Tests
{
	[TestFixture]
    public class GeneratorTests
	{
		[Test]
		public void Constructor_NoError()
		{
			var generator = new Generator();
			Assert.IsNull(generator.Error);
			Assert.That(generator.CanGenerate);
		}

		[Test]
		public void Generate_AllSourceStringsResolveToSingleTarget_AllTargetsSet()
		{
			var generator = new Generator();
			generator.Generate(source => new[] { "XYZ-" + source });
			Assert.That(generator.GenerationComplete);
			Assert.IsTrue(generator.AllTranslations.All(t => "XYZ-" + t.Item1 == t.Item2));
		}

		[Test]
		public void Generate_AllSourceStringsResolveToMultipleTargets_NoTargetsSet()
		{
			var generator = new Generator();
			generator.Generate(source => new[] { "XYZ", source });
			var origList = generator.AllTranslations.ToList();

			Assert.That(generator.GenerationComplete);
			Assert.IsTrue(origList.SequenceEqual(generator.AllTranslations));
		}

		[Test]
		public void Generate_SomeSourceStringsResolveToNull_NoTargetsSet()
		{
			var generator = new Generator();
			generator.Generate(source => source.Length < 7 ? null : new[] { "XYZ-" + source });

			Assert.That(generator.GenerationComplete);
			Assert.IsTrue(generator.AllTranslations.Any(t => "XYZ-" + t.Item1 == t.Item2));
		}
	}
}
