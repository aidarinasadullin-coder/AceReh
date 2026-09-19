using System;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.RefactorBaseline
{
    /// <summary>
    /// Относительный допуск baseline-сверки (решение владельца D6, ADR-016):
    /// |a−b| ≤ 1e-9 · max(|a|, |b|, 1). NUnit `.Within` — абсолютный, поэтому
    /// компаратор свой. Пин остаётся change-detector'ом: допуск покрывает
    /// только ULP-шум, а не реальный сдвиг значений.
    /// </summary>
    internal static class BaselineAssertions
    {
        private const double RelativeTolerance = 1e-9;

        public static void AssertCloseTo(double actual, double expected, string name)
        {
            var tolerance = RelativeTolerance * Math.Max(Math.Max(Math.Abs(actual), Math.Abs(expected)), 1.0);
            Assert.That(
                Math.Abs(actual - expected),
                Is.LessThanOrEqualTo(tolerance),
                $"{name}: ожидалось {expected:R}, получено {actual:R} (предел {tolerance:R})");
        }
    }
}
