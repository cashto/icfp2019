using System;
using System.IO;
using System.Linq;
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
            var fileName = @"C:\Users\cashto\Documents\GitHub\icfp2019\problems\prob-102.desc";

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

        [TestMethod]
        public void TestBlockers()
        { 
            var blockers = State.Blockers(new Point(2, 1)).ToList();

            Assert.AreEqual(2, blockers.Count);
            Assert.AreEqual(new Point(1, 0), blockers[0]);
            Assert.AreEqual(new Point(1, 1), blockers[1]);
        }

        [TestMethod]
        public void TestGenerateMap()
        {
            var desc = GenerateMap.Generate(new MapSpecification() { Size = 50, MinVertexes = 0, MaxVertexes = 10000 });
            var state = new State(desc);
            Console.WriteLine(state.Board);
        }

        [TestMethod]
        public void TestPathFinding()
        {
            var board = new Board(30, 30);
            var undo = new BoardUndo();
            board.Set(new Point(0, 1), Board.Wall, undo);
            board.Set(new Point(1, 0), Board.Wall, undo);
            var middle = new Point(15, 15);
            var bottomLeft = new Point(0, 0);
            var topRight = new Point(29, 29);

            var path = board.PathFind(bottomLeft, middle);
            Assert.IsNull(path);

            path = board.PathFind(topRight, middle);
            Assert.AreEqual(29, path.Count);
            Assert.AreEqual(path.First(), topRight);
            Assert.AreEqual(path.Last(), middle);
        }

        [TestMethod]
        public void TestNotAllPainted()
        {
            var fileName = @"C:\Users\cashto\Documents\GitHub\icfp2019\problems\block-10.desc";

            var desc = File.ReadAllText(fileName);

            var state = new State(desc);

            var moves = File.ReadAllText(@"C:\Users\cashto\Documents\GitHub\icfp2019\solutions\block-10.sol");

            var endState = state.MultiMove(moves);

            Console.WriteLine(endState);
        }
    }
}