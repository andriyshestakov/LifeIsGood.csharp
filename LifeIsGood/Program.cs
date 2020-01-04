using System;
using System.IO;

namespace LifeIsGood
{
    /// <summary>
    /// Implements Conway's Game Of Life badly
    /// https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life on a torus
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            bool[][] world = null;
            using (StreamReader sr = new StreamReader("sample_input.txt")) // Considering "Given the time constraint you can assume that the input will be well formed." -  IOException  are not handle
            {
                string text = sr.ReadToEnd();

                if (string.IsNullOrEmpty(text)) return -1;

                world = LifeGridStream.Read(text);
            }

            var torusLifeGrid = new TorusLifeGrid(world);

            torusLifeGrid.Evolve();

            Console.WriteLine(LifeGridStream.Write(torusLifeGrid.World));

            return 42;
        }
    }
}