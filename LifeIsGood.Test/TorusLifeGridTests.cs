using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LifeIsGood.csharp.Test
{
    [TestClass]
    public class TorusLifeGridTests
    {
        [TestMethod]
        public void torusLifeGrid_sample_test()
        {
            var actual = DoTest(@"_____
__*__
_***_
__*__
_____");

            var expected = @"-----
-***-
-*-*-
-***-
-----";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void torusLifeGrid_Beehive_test()
        {
            var actual = DoTest(@"------
--**--
-*--*-
--**--
------");

            var expected = @"------
--**--
-*--*-
--**--
------";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void torusLifeGrid_Blinker_test()
        {
            var actual = DoTest(@"-----
--*--
--*--
--*--
-----");

            var expected = @"-----
-----
-***-
-----
-----";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void torusLifeGrid_Glider_test()
        {
            var actual = DoTest(@"------
--*---
---**-
--**--
------");

            var expected = @"------
---*--
----*-
--***-
------";
            Assert.AreEqual(expected, actual);
        }

        private string DoTest(string input)
        {
            var world = LifeGridStream.Read(input);

            var torusLifeGrid = new TorusLifeGrid(world);

            torusLifeGrid.Evolve();

            return LifeGridStream.Write(torusLifeGrid.World);
        }
    }
}
