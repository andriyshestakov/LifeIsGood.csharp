using System.Linq;
using System.Text;

namespace LifeIsGood
{
    public static class LifeGridStream
    {
        /// <summary>
        /// Reads each line treating each “*” character as a live cell and anything else as a dead cell
        /// </summary>
        /// <returns>
        /// Jagged array of dead(false)/live(true) cells representing the life grid. 
        /// NOTE: each array of jagged array could potentially have different size, i.e. possible result[j].Length != result[i].Length 
        /// </returns>
        public static bool[][] Read(string text)
        {
            var grid = new bool[0][];

            string[] lines = text.Split(new[] {'\r', '\n'}, System.StringSplitOptions.RemoveEmptyEntries);

            if (lines == null || lines.Length == 0) return grid;

            grid = new bool[lines.Length][];

            for (int i = 0; i < lines.Length; i++)
            {
                grid[i] = lines[i].Select(c => c == '*').ToArray();
            }

            return grid;
        }

        public static string Write(bool[][] grid)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < grid.Length; i++)
            {
                grid[i].Select(cell => cell ? sb.Append("*") : sb.Append("-")).ToArray();
                if (i < grid.Length - 1) sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}