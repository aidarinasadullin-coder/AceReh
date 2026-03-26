using System.Windows.Controls;
using NUnit.Framework;
using SnowMeltingCalculator.Behaviors;

namespace SnowMeltingCalculator.Tests.Behaviors
{
    /// <summary>
    /// Тесты для DataGridBehavior - attached behavior для автоматического входа в редактирование при клике.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class DataGridBehaviorTests
    {
        #region SingleClickEdit Tests

        [Test]
        public void SingleClickEdit_DefaultValue_IsFalse()
        {
            // Arrange
            var dataGrid = new DataGrid();

            // Act
            var value = DataGridBehavior.GetSingleClickEdit(dataGrid);

            // Assert
            Assert.That(value, Is.False);
        }

        [Test]
        public void SingleClickEdit_CanBeSetToTrue()
        {
            // Arrange
            var dataGrid = new DataGrid();

            // Act
            DataGridBehavior.SetSingleClickEdit(dataGrid, true);
            var value = DataGridBehavior.GetSingleClickEdit(dataGrid);

            // Assert
            Assert.That(value, Is.True);
        }

        [Test]
        public void SingleClickEdit_CanBeSetToFalse()
        {
            // Arrange
            var dataGrid = new DataGrid();
            DataGridBehavior.SetSingleClickEdit(dataGrid, true);

            // Act
            DataGridBehavior.SetSingleClickEdit(dataGrid, false);
            var value = DataGridBehavior.GetSingleClickEdit(dataGrid);

            // Assert
            Assert.That(value, Is.False);
        }

        [Test]
        public void SingleClickEdit_CanBeToggledMultipleTimes()
        {
            // Arrange
            var dataGrid = new DataGrid();

            // Act & Assert
            DataGridBehavior.SetSingleClickEdit(dataGrid, true);
            Assert.That(DataGridBehavior.GetSingleClickEdit(dataGrid), Is.True);

            DataGridBehavior.SetSingleClickEdit(dataGrid, false);
            Assert.That(DataGridBehavior.GetSingleClickEdit(dataGrid), Is.False);

            DataGridBehavior.SetSingleClickEdit(dataGrid, true);
            Assert.That(DataGridBehavior.GetSingleClickEdit(dataGrid), Is.True);
        }

        #endregion

        #region Multiple DataGrids Tests

        [Test]
        public void SingleClickEdit_WorksWithMultipleDataGrids()
        {
            // Arrange
            var dataGrid1 = new DataGrid();
            var dataGrid2 = new DataGrid();

            // Act
            DataGridBehavior.SetSingleClickEdit(dataGrid1, true);
            DataGridBehavior.SetSingleClickEdit(dataGrid2, false);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(DataGridBehavior.GetSingleClickEdit(dataGrid1), Is.True);
                Assert.That(DataGridBehavior.GetSingleClickEdit(dataGrid2), Is.False);
            });
        }

        #endregion
    }
}