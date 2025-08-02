// SimpleApp.Tests/CalculatorTests.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleApp;

namespace SimpleApp.Tests
{
    [TestClass]
    public class CalculatorTests
    {
        [TestMethod]
        public void Add_ReturnsSum()
        {
            var calc = new Calculator();
            Assert.AreEqual(5, calc.Add(2, 3));
        }
    }
}