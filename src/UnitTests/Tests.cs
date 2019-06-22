using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Solver
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestBoardInit()
        {
            var fileName = @"C:\Users\cashto\Documents\GitHub\icfp2019\problems\prob-003.desc";

            var desc = File.ReadAllText(fileName);

            var state = new State(desc);

            state = state.MultiMove("DDDDDDDDDDDDDDDDD");

            Console.WriteLine(state.Board.ToString(state.Position));
        }

        [TestMethod]
        public void TestSolver()
        {
            var fileName = @"C:\Users\cashto\Documents\GitHub\icfp2019\problems\prob-003.desc";

            var desc = File.ReadAllText(fileName);

            var state = new State(desc);
        }

        [TestMethod]
        public void TestRotate()
        {
            var p = new Point(0, 1);
            Assert.AreEqual(new Point(-1, 0), p.RotateLeft());
            Assert.AreEqual(new Point(1, 0), p.RotateRight());
        }

        [TestMethod]
        public void TestDirection()
        {
            var fileName = @"C:\Users\cashto\Documents\GitHub\icfp2019\problems\prob-003.desc";

            var desc = File.ReadAllText(fileName);

            var state = new State(desc);
            var state2 = state.Move('Q').Item1;
            var state3 = state2.Move('E').Item1;
            var state4 = state2.Move('Q').Item1;

            Assert.AreEqual(0, state.Direction);
            Assert.AreEqual(3, state2.Direction);
            Assert.AreEqual(0, state3.Direction);
            Assert.AreEqual(2, state4.Direction);
        }
    }
}