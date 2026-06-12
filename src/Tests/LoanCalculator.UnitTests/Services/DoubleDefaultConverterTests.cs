using System.Text.Json;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Services
{
    [TestFixture]
    public class DoubleDefaultConverterTests
    {
        private JsonSerializerOptions _options;

        [SetUp]
        public void Setup()
        {
            _options = new JsonSerializerOptions
            {
                Converters = { new DoubleDefaultConverter() }
            };
        }

        // Helper to round-trip a double through JSON serialization
        private double RoundTrip(double value)
        {
            var json = JsonSerializer.Serialize(value, _options);
            return JsonSerializer.Deserialize<double>(json, _options);
        }

        // ── Read — special string tokens ─────────────────────────────────────

        [Test]
        public void Read_InfinityString_ReturnsPositiveInfinity()
        {
            var result = JsonSerializer.Deserialize<double>("\"Infinity\"", _options);
            Assert.That(result, Is.EqualTo(double.PositiveInfinity));
        }

        [Test]
        public void Read_NegativeInfinityString_ReturnsNegativeInfinity()
        {
            var result = JsonSerializer.Deserialize<double>("\"-Infinity\"", _options);
            Assert.That(result, Is.EqualTo(double.NegativeInfinity));
        }

        [Test]
        public void Read_NaNString_ReturnsNaN()
        {
            var result = JsonSerializer.Deserialize<double>("\"NaN\"", _options);
            Assert.That(result, Is.NaN);
        }

        [Test]
        public void Read_UnknownString_ReturnsZero()
        {
            var result = JsonSerializer.Deserialize<double>("\"invalid\"", _options);
            Assert.That(result, Is.EqualTo(0.0));
        }

        [Test]
        public void Read_EmptyString_ReturnsZero()
        {
            var result = JsonSerializer.Deserialize<double>("\"\"", _options);
            Assert.That(result, Is.EqualTo(0.0));
        }

        // ── Read — numeric tokens ────────────────────────────────────────────

        [Test]
        public void Read_NormalNumber_ReturnsValue()
        {
            var result = JsonSerializer.Deserialize<double>("3.14", _options);
            Assert.That(result, Is.EqualTo(3.14).Within(0.0001));
        }

        [Test]
        public void Read_Zero_ReturnsZero()
        {
            var result = JsonSerializer.Deserialize<double>("0", _options);
            Assert.That(result, Is.EqualTo(0.0));
        }

        [Test]
        public void Read_NegativeNumber_ReturnsNegativeValue()
        {
            var result = JsonSerializer.Deserialize<double>("-99.5", _options);
            Assert.That(result, Is.EqualTo(-99.5).Within(0.0001));
        }

        // ── Write ────────────────────────────────────────────────────────────

        [Test]
        public void Write_PositiveInfinity_WritesInfinityString()
        {
            var json = JsonSerializer.Serialize(double.PositiveInfinity, _options);
            Assert.That(json, Is.EqualTo("\"Infinity\""));
        }

        [Test]
        public void Write_NegativeInfinity_WritesNegativeInfinityString()
        {
            var json = JsonSerializer.Serialize(double.NegativeInfinity, _options);
            Assert.That(json, Is.EqualTo("\"-Infinity\""));
        }

        [Test]
        public void Write_NaN_WritesNaNString()
        {
            var json = JsonSerializer.Serialize(double.NaN, _options);
            Assert.That(json, Is.EqualTo("\"NaN\""));
        }

        [Test]
        public void Write_NormalNumber_WritesNumericToken()
        {
            var json = JsonSerializer.Serialize(42.5, _options);
            Assert.That(json, Is.EqualTo("42.5"));
        }

        [Test]
        public void Write_Zero_WritesZero()
        {
            var json = JsonSerializer.Serialize(0.0, _options);
            Assert.That(json, Is.EqualTo("0"));
        }

        // ── Round-trip ───────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_PositiveInfinity_Preserves()
            => Assert.That(RoundTrip(double.PositiveInfinity), Is.EqualTo(double.PositiveInfinity));

        [Test]
        public void RoundTrip_NegativeInfinity_Preserves()
            => Assert.That(RoundTrip(double.NegativeInfinity), Is.EqualTo(double.NegativeInfinity));

        [Test]
        public void RoundTrip_NaN_Preserves()
            => Assert.That(RoundTrip(double.NaN), Is.NaN);

        [Test]
        public void RoundTrip_NormalValue_Preserves()
            => Assert.That(RoundTrip(12345.678), Is.EqualTo(12345.678).Within(0.001));

        [Test]
        public void RoundTrip_NegativeValue_Preserves()
            => Assert.That(RoundTrip(-500000.0), Is.EqualTo(-500000.0).Within(0.01));

        // ── POCO round-trip with DoubleDefaultConverter ───────────────────────

        private record LoanRecord(double Principal, double Rate);

        [Test]
        public void RoundTrip_POCO_NormalValues_PreservesFields()
        {
            var loan = new LoanRecord(500000.0, 5.75);
            var json = JsonSerializer.Serialize(loan, _options);
            var back = JsonSerializer.Deserialize<LoanRecord>(json, _options);
            Assert.That(back!.Principal, Is.EqualTo(500000.0).Within(0.01));
            Assert.That(back.Rate, Is.EqualTo(5.75).Within(0.001));
        }
    }
}
