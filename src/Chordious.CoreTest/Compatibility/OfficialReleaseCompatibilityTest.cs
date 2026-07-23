// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

using Chordious.Core;
using Chordious.Core.Legacy;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.CoreTest
{
    [TestClass]
    public class OfficialReleaseCompatibilityTest
    {
        [TestInitialize]
        public void Initialize()
        {
            AppInfo.Assembly = typeof(OfficialReleaseCompatibilityTest).Assembly;
        }

        [TestMethod]
        public void Official280Corpus_HasExpectedProvenanceAndIntegrity()
        {
            using JsonDocument manifest = JsonDocument.Parse(
                GetResourceBytes(ManifestResourceName));
            JsonElement root = manifest.RootElement;

            Assert.AreEqual("v2.8.0",
                root.GetProperty("release").GetProperty("tag").GetString());
            Assert.AreEqual(
                "bbcbcf6ec34846f4ed297fc4a06fa6dce2bd37fb",
                root.GetProperty("release").GetProperty("commit").GetString());
            Assert.IsTrue(
                root.GetProperty("fixtures")
                    .GetProperty("Chordious.Config.2.8.0.xml")
                    .GetProperty("sanitized")
                    .GetBoolean());

            AssertFixtureHash(
                root,
                "Chordious.Config.2.8.0.xml",
                ConfigResourceName);
            AssertFixtureHash(
                root,
                "Classic.ChordLine.txt",
                ChordLineResourceName);

            XDocument document = XDocument.Parse(
                GetResourceText(ConfigResourceName));
            Assert.AreEqual(
                "Chordious.WPF 2.8.0",
                document.Root?.Attribute("version")?.Value);
            CollectionAssert.AreEqual(
                ExpectedSections,
                document.Root?.Elements()
                    .Select(element => element.Name.LocalName)
                    .ToArray());
        }

        [TestMethod]
        public void Official280Config_LoadsEveryConfigurationArea()
        {
            ConfigFile config = LoadOfficialConfig();

            Assert.AreEqual(
                @"C:\Users\Chordious\Documents",
                config.ChordiousSettings.Get("app.lastpath", false));
            Assert.AreEqual(
                "#1F4E79",
                config.DiagramStyle.Get("diagram.bordercolor", false));

            Instrument instrument = config.Instruments.Get("Compatibility Lute");
            Assert.AreEqual(5, instrument.NumStrings);
            Assert.AreEqual(
                "Open C",
                instrument.Tunings.Single(tuning => tuning.Name == "Open C").Name);

            ChordQuality chordQuality = config.ChordQualities
                .OfType<ChordQuality>()
                .Single(quality => quality.Name == "Compatibility Major Seventh");
            Assert.AreEqual("maj7", chordQuality.Abbreviation);
            CollectionAssert.AreEqual(
                new[] { 0, 4, 7, 11 },
                chordQuality.Intervals);

            Scale scale = config.Scales
                .OfType<Scale>()
                .Single(item => item.Name == "Compatibility Dorian");
            CollectionAssert.AreEqual(
                new[] { 0, 2, 3, 5, 7, 9, 10, 12 },
                scale.Intervals);

            DiagramCollection collection = config.DiagramLibrary
                .Get("Compatibility 2.8.0");
            Assert.AreEqual(2, collection.Count);

            Diagram diagram = collection.DiagramAt(0);
            Assert.AreEqual("CΔ7 ♯", diagram.Title);
            Assert.AreEqual(6, diagram.NumStrings);
            Assert.AreEqual(5, diagram.NumFrets);
            Assert.AreEqual(3, diagram.Marks.Count());
            Assert.AreEqual(1, diagram.Barres.Count());
            Assert.AreEqual(1, diagram.FretLabels.Count());
            Assert.IsTrue(diagram.Marks.Any(mark =>
                mark.Type == DiagramMarkType.Open));
            Assert.IsTrue(diagram.Marks.Any(mark =>
                mark.Type == DiagramMarkType.Muted));
            StringAssert.Contains(
                diagram.ToImageMarkup(ImageMarkupType.SVG),
                "<svg");
        }

        [TestMethod]
        public void Official280Config_RoundTripsWithoutSemanticDataLoss()
        {
            byte[] fixture = GetResourceBytes(ConfigResourceName);
            ConfigFile config = LoadOfficialConfig();

            byte[] firstRoundTrip = Save(config, ConfigParts.All);
            ConfigFile reloaded = LoadConfig(firstRoundTrip);
            byte[] secondRoundTrip = Save(reloaded, ConfigParts.All);

            Assert.AreEqual(
                CanonicalizeConfig(fixture),
                CanonicalizeConfig(firstRoundTrip));
            Assert.AreEqual(
                CanonicalizeConfig(firstRoundTrip),
                CanonicalizeConfig(secondRoundTrip));
        }

        [TestMethod]
        public void Official280Config_ExportsAndReloadsEachConfigurationArea()
        {
            (ConfigParts Part, string Section)[] cases =
            {
                (ConfigParts.Settings, "settings"),
                (ConfigParts.Styles, "styles"),
                (ConfigParts.Instruments, "instruments"),
                (ConfigParts.Qualities, "qualities"),
                (ConfigParts.Scales, "scales"),
                (ConfigParts.Library, "library")
            };

            foreach ((ConfigParts part, string section) in cases)
            {
                ConfigFile partial = new(
                    ConfigFile.DefaultConfig,
                    ConfigFile.UserLevelKey);
                using (Stream input = OpenResource(ConfigResourceName))
                {
                    partial.LoadFile(input, part);
                }

                byte[] exported = Save(partial, part);
                using MemoryStream exportedStream = new(exported);
                XDocument document = XDocument.Load(exportedStream);
                XElement[] sections = document.Root?.Elements().ToArray()
                    ?? Array.Empty<XElement>();

                Assert.AreEqual(
                    1,
                    sections.Length,
                    $"Expected one section for {part}.");
                Assert.AreEqual(section, sections[0].Name.LocalName);

                ConfigFile reloaded = new(
                    ConfigFile.DefaultConfig,
                    ConfigFile.UserLevelKey);
                using MemoryStream stream = new(exported);
                reloaded.LoadFile(stream, part);
            }
        }

        [TestMethod]
        public void ClassicChordLineCorpus_ImportsAndRoundTripsThroughLibrary()
        {
            ConfigFile config = new(
                ConfigFile.DefaultConfig,
                ConfigFile.UserLevelKey);
            DiagramCollection imported;
            using (Stream stream = OpenResource(ChordLineResourceName))
            {
                imported = ChordDocument.Load(
                    config.DiagramLibrary.Style,
                    stream);
            }

            Assert.AreEqual(3, imported.Count);
            Assert.AreEqual("C", imported.DiagramAt(0).Title);
            Assert.AreEqual(6, imported.DiagramAt(0).Marks.Count());
            Assert.IsTrue(imported.DiagramAt(0).Marks.Any(mark =>
                mark.Type == DiagramMarkType.Open));
            Assert.IsTrue(imported.DiagramAt(0).Marks.Any(mark =>
                mark.Type == DiagramMarkType.Muted));
            Assert.AreEqual("Fmaj7", imported.DiagramAt(1).Title);
            Assert.AreEqual(1, imported.DiagramAt(1).FretLabels.Count());
            Assert.AreEqual("Am / G", imported.DiagramAt(2).Title);

            config.DiagramLibrary
                .Add("Imported Classic")
                .Add(imported);
            ConfigFile reloaded = LoadConfig(Save(config, ConfigParts.All));
            DiagramCollection restored = reloaded.DiagramLibrary
                .Get("Imported Classic");

            Assert.AreEqual(3, restored.Count);
            CollectionAssert.AreEqual(
                new[] { "C", "Fmaj7", "Am / G" },
                restored.Select(diagram => diagram.Title).ToArray());
        }

        private static ConfigFile LoadOfficialConfig()
        {
            using Stream stream = OpenResource(ConfigResourceName);
            return new ConfigFile(
                ConfigFile.DefaultConfig,
                stream,
                ConfigFile.UserLevelKey);
        }

        private static ConfigFile LoadConfig(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            return new ConfigFile(
                ConfigFile.DefaultConfig,
                stream,
                ConfigFile.UserLevelKey);
        }

        private static byte[] Save(ConfigFile config, ConfigParts parts)
        {
            using MemoryStream stream = new();
            config.SaveFile(stream, parts);
            return stream.ToArray();
        }

        private static string CanonicalizeConfig(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            XDocument document = XDocument.Load(stream);
            document.Root?.Attribute("version")?.Remove();
            document.Root?.Attribute("date")?.Remove();
            return document.ToString(SaveOptions.DisableFormatting);
        }

        private static void AssertFixtureHash(
            JsonElement manifest,
            string fixtureName,
            string resourceName)
        {
            string expected = manifest
                .GetProperty("fixtures")
                .GetProperty(fixtureName)
                .GetProperty("sha256")
                .GetString();
            string actual = Convert.ToHexString(
                    SHA256.HashData(GetResourceBytes(resourceName)))
                .ToLowerInvariant();
            Assert.AreEqual(expected, actual);
        }

        private static string GetResourceText(string resourceName)
        {
            using Stream stream = OpenResource(resourceName);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private static byte[] GetResourceBytes(string resourceName)
        {
            using Stream stream = OpenResource(resourceName);
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }

        private static Stream OpenResource(string resourceName)
        {
            return typeof(OfficialReleaseCompatibilityTest)
                .Assembly
                .GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Missing embedded test resource: {resourceName}");
        }

        private static readonly string[] ExpectedSections =
        {
            "settings",
            "styles",
            "instruments",
            "qualities",
            "scales",
            "library"
        };

        private const string ResourcePrefix =
            "Chordious.CoreTest.Compatibility.TestCases.";
        private const string ConfigResourceName =
            ResourcePrefix + "Chordious.Config.2.8.0.xml";
        private const string ChordLineResourceName =
            ResourcePrefix + "Classic.ChordLine.txt";
        private const string ManifestResourceName =
            ResourcePrefix + "manifest.json";
    }
}
