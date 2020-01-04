using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeIsGood
{
    /// <summary>
    /// Two-dimensional orthogonal grid with left and right edges, and the top and bottom edges also, stitched together yielding a toroidal array
    /// Stitching the edges implies that the top and the bottom edges have the same size, and the same applies for left and right edges.
    /// </summary>
    /// <remarks>
    /// Using jagged arrays. Jagged arrays are faster and each array of a jagged occupies its block of memory (vs multidimensional array is a single block of memory). 
    /// </remarks>
    public class TorusLifeGrid
    {
        private readonly bool[][] torusGrid;
        public TorusLifeGrid(bool[][] grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            // Torus is achieved as two-dimensional grid with left and right edges, and the top and bottom edges also, stitched together yielding a toroidal array
            // Stitching the edges implies that the top and the bottom edges have the same size, and the same applies for left and right edges.
            // For this reason we will resize all jagged inner arrays to be of max inner array length 
            var gridMaxColumns = grid.Select(r => r.Length).Max();

            torusGrid = new bool[grid.Length][];
            for (int i = 0; i < grid.Length; i++)
            {
                var newArray = new bool[gridMaxColumns];
                grid[i].CopyTo(newArray, 0);
                torusGrid[i] = newArray;
            }
        }

        public bool[][] World => torusGrid; 

        /// <summary>
        /// To store new state we could either create identical jagged array, which is memory intensive, 
        /// or we could store new lines in buffer and update existing jagged array when we do not need the line for next neighbour calculations
        /// Using buffer technique 
        /// </summary>
        public void Evolve()
        {
            if (torusGrid == null || torusGrid.Length == 0) return;

            bool[] nextStateFirstRow = null;
            bool[] nextStatePreviousLine = null;
            bool[] nextStateCurrentLine = null;

            for (int i = 0; i < torusGrid.Length; i++) // current row i
            {
                nextStatePreviousLine = nextStateCurrentLine;

                nextStateCurrentLine = new bool[torusGrid[i].Length];

                var neighbourRows = GetNeighbourRows(torusGrid, i).ToArray(); //  Neighbour Rows are Preceding Row, Row at an index and Following Row

                var liveNeighboursCountByColumn = GetLiveNeighboursCountByColumn(neighbourRows).ToArray(); // array with the count of live cells in 3 neighbour Rows by the same cell (column)

                for (int j = 0; j < torusGrid[i].Length; j++) // current cell j 
                {
                    var isLive = torusGrid[i][j];

                    var liveNeighboursCount = CountLiveNeighbours(liveNeighboursCountByColumn, j, isLive);

                    nextStateCurrentLine[j] = Evolve(isLive, liveNeighboursCount);
                }

                // update torusGrid
                if (nextStatePreviousLine != null)
                {
                    torusGrid[i - 1] = nextStatePreviousLine;
                }

                if (i == 0) // first row. keep old state until the end as it is required for the last row calc
                {
                    nextStateFirstRow = nextStateCurrentLine;
                    nextStateCurrentLine = null;
                }
                else if (i == torusGrid.Length - 1) //last row. we finished 
                {
                    torusGrid[0] = nextStateFirstRow;
                    torusGrid[i] = nextStateCurrentLine;
                }
            }
        }

        /// <summary>
        /// Any live cell with fewer than two live neighbors dies, as if by underpopulation.
        /// Any live cell with two or three live neighbors lives on to the next generation.
        /// Any live cell with more than three live neighbors dies, as if by overpopulation.
        /// Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
        /// </summary>
        /// <param name="cellValue"></param>
        /// <param name="liveNeighboursCount"></param>
        /// <returns></returns>
        private bool Evolve(bool liveCell, int liveNeighboursCount)
        {
            //These rules can be condensed into the following:

            //  Any live cell with two or three neighbors survives.
            if (liveCell && liveNeighboursCount == 2 || liveNeighboursCount == 3) return true;

            //  Any dead cell with three live neighbors becomes a live cell.
            if (!liveCell && liveNeighboursCount == 3) return true;

            //  All other live cells die in the next generation.Similarly, all other dead cells stay dead.
            return false;
        }

        private static int CountLiveNeighbours(int[] liveNeighboursCountByColumn, int cellndex, bool cellValue)
        {
            var lastCellIndex = liveNeighboursCountByColumn.Length - 1;

            // if cell index is the first cell then preceding cell is the last cell of the row as we use torus, otherwise just cellndex - 1 
            var precedingCellIndex = cellndex == 0 ? lastCellIndex : cellndex - 1;

            // if cell index is the last cell then following cell is the first cell of the row as we use torus, otherwise just cellndex + 1
            var followingCellIndex = cellndex == lastCellIndex ? 0 : cellndex + 1;

            return liveNeighboursCountByColumn[precedingCellIndex] +
                liveNeighboursCountByColumn[cellndex] +
                liveNeighboursCountByColumn[followingCellIndex]
                - (cellValue ? 1 : 0);
        }

        /// <summary>
        /// we build neighbourRows as 3 arrays of the same legth 
        /// if we add up all live cells in each column ( for each cell index j ) we will have summary array of live neighbours + self 
        /// </summary>
        /// <param name="neighbourRows"></param>
        /// <returns></returns>
        private static IEnumerable<int> GetLiveNeighboursCountByColumn(bool[][] neighbourRows)
        {
            int[] liveNeighboursCount = new int[neighbourRows[0].Length];

            for (int j = 0; j < neighbourRows[0].Length; j++) // current cell j 
            {
                int liveCount = 0;
                if (neighbourRows[0][j]) liveCount++;
                if (neighbourRows[1][j]) liveCount++;
                if (neighbourRows[2][j]) liveCount++;
                liveNeighboursCount[j] = liveCount;
            }

            return liveNeighboursCount;
        }

        /// <summary>
        /// Neighbour Rows are Preceding Row, Row at an index and the Following Row
        /// </summary>
        /// <param name="torusGrid"></param>
        /// <param name="rowIndex"></param>
        /// <returns> Preceding Row, Row at an index and Following Row selected from torusGrid </returns>
        private static IEnumerable<bool[]> GetNeighbourRows(bool[][] torusGrid, int rowIndex)
        {
            if (torusGrid == null) yield break;

            var rowCount = torusGrid.Length;

            if (rowCount == 0) yield break;

            if (rowCount == 1) // not sure we even need to cover this case and if it is valid at all 
            {
                yield return torusGrid[0];
                yield return torusGrid[0];
                yield return torusGrid[0];
            }

            var lastRowIndex = rowCount - 1;

            // if rowIndex is the first row then preceding row is the last row of the grid as we use torus, otherwise just  rowIndex - 1 
            var precedingRowIndex = rowIndex == 0 ? lastRowIndex : rowIndex - 1;

            // if rowIndex is the last row then following row is the first row of the grid as we use torus, otherwise just  rowIndex + 1
            var followingRowIndex = rowIndex == lastRowIndex ? 0 : rowIndex + 1;

            yield return torusGrid[precedingRowIndex];
            yield return torusGrid[rowIndex];
            yield return torusGrid[followingRowIndex];
        }
    }
}
