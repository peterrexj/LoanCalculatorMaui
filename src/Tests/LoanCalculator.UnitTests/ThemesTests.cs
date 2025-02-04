using Pj.Library;
using System.Xml.Linq;

namespace LoanCalculator.UnitTests
{
    public class ThemesTests
    {
        private string _rootFolderForThemes;

        [SetUp]
        public void Setup()
        {
            // Set the relative path to the themes folder
            _rootFolderForThemes = Path.Combine(PjUtility.Runtime.ExecutingRepositoryRootFolder, "src", "LoanCalculator", "Themes");
        }

        [Test]
        public void ThemeFiles_ShouldHave_EqualKeys()
        {
            string[] files = { "LightTheme.xaml", "DarkTheme.xaml" };

            var allKeys = new Dictionary<string, HashSet<string>>();

            foreach (var file in files)
            {
                var filePath = Path.Combine(_rootFolderForThemes, file);
                Assert.That(File.Exists(filePath), $"File {file} does not exist at path {filePath}");

                var keys = GetKeysFromThemeFile(filePath);
                allKeys[file] = keys;
            }

            // Compare keys between all theme files
            var firstFileKeys = allKeys[files[0]];
            foreach (var file in files.Skip(1))
            {
                var currentFileKeys = allKeys[file];
                Assert.That(currentFileKeys, Is.EquivalentTo(firstFileKeys), $"Keys in {file} do not match keys in {files[0]}");
            }
        }

        [Test]
        public void ThemeFiles_ShouldHave_NonEmptyValues()
        {
            string[] files = { "LightTheme.xaml", "DarkTheme.xaml" };

            foreach (var file in files)
            {
                var filePath = Path.Combine(_rootFolderForThemes, file);
                Assert.That(File.Exists(filePath), $"File {file} does not exist at path {filePath}");

                var keysWithValues = GetKeysWithNonEmptyValues(filePath);
                Assert.That(keysWithValues.Count, Is.GreaterThan(0), $"File {file} contains keys with empty values.");
            }
        }

        [Test]
        public void ThemeFiles_ShouldPrint_SameValuesForSameKeys()
        {
            string[] files = { "LightTheme.xaml", "DarkTheme.xaml" };

            var allKeysWithValues = new Dictionary<string, Dictionary<string, string>>();

            foreach (var file in files)
            {
                var filePath = Path.Combine(_rootFolderForThemes, file);
                Assert.That(File.Exists(filePath), $"File {file} does not exist at path {filePath}");

                var keysWithValues = GetKeysWithNonEmptyValues(filePath);
                allKeysWithValues[file] = keysWithValues;
            }

            var firstFileKeysWithValues = allKeysWithValues[files[0]];
            foreach (var key in firstFileKeysWithValues.Keys)
            {
                bool allValuesSame = true;
                string firstValue = firstFileKeysWithValues[key];

                foreach (var file in files.Skip(1))
                {
                    var currentFileKeysWithValues = allKeysWithValues[file];
                    if (currentFileKeysWithValues.ContainsKey(key) && currentFileKeysWithValues[key] != firstValue)
                    {
                        allValuesSame = false;
                        break;
                    }
                }

                if (allValuesSame)
                {
                    TestContext.WriteLine($"Key: {key}, Value: {firstValue}");
                }
            }
        }

        private HashSet<string> GetKeysFromThemeFile(string filePath)
        {
            var keys = new HashSet<string>();
            var xdoc = XDocument.Load(filePath);

            XNamespace xNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";
            var elementsWithKeys = xdoc.Descendants().Where(e => e.Attribute(xNamespace + "Key") != null);
            foreach (var element in elementsWithKeys)
            {
                var key = element.Attribute(xNamespace + "Key")?.Value;
                if (!string.IsNullOrEmpty(key))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        private Dictionary<string, string> GetKeysWithNonEmptyValues(string filePath)
        {
            var keysWithValues = new Dictionary<string, string>();
            var xdoc = XDocument.Load(filePath);

            XNamespace xNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";
            var elementsWithKeys = xdoc.Descendants().Where(e => e.Attribute(xNamespace + "Key") != null);
            foreach (var element in elementsWithKeys)
            {
                var key = element.Attribute(xNamespace + "Key")?.Value;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(element.Value))
                {
                    keysWithValues[key] = element.Value;
                }
            }

            return keysWithValues;
        }
    }
}

