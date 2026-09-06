using NUnit.Framework;
using SnowMeltingCalculator.Services.Reports.Calculation;

namespace SnowMeltingCalculator.Tests.Services.Reports.Calculation
{
    /// <summary>
    /// Пины форматирования чисел отчёта (В6): запятая — десятичный разделитель,
    /// пробел — разделитель тысяч; каноническая культура AppCulture (ru-RU).
    /// </summary>
    [TestFixture]
    public class ReportNumberTests
    {
        [Test]
        public void Format_DecimalSeparator_IsComma()
        {
            Assert.That(ReportNumber.Format(15.6), Is.EqualTo("15,60"));
        }

        [Test]
        public void Format_ThousandsSeparator_IsSpace()
        {
            // ru-RU использует неразрывный пробел (U+00A0) как разделитель групп.
            Assert.That(ReportNumber.Format(29199.0, "N0"), Is.EqualTo("29 199"));
            Assert.That(ReportNumber.Format(1234567.89), Is.EqualTo("1 234 567,89"));
            Assert.That(ReportNumber.Format(29199.0, "N0"), Does.Not.Contain(","));
        }

        [Test]
        public void Format_WithDecimals_UsesRequestedPrecision()
        {
            Assert.That(ReportNumber.Format(0.0575, 4), Is.EqualTo("0,0575"));
            Assert.That(ReportNumber.Format(9.0833, 2), Is.EqualTo("9,08"));
        }

        [Test]
        public void Format_NegativeNumber_UsesCommaAndMinus()
        {
            Assert.That(ReportNumber.Format(-15.5, "N1"), Is.EqualTo("-15,5"));
        }
    }
}
