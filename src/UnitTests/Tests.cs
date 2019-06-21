using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
    [TestMethod]
    public void TestMethod1()
    {
        Assert.AreEqual(4, Program.Add(2, 2));
    }
}
