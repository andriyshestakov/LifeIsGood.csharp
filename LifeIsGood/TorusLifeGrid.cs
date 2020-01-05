using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeIsGood
{
    /// <summary>
    /// Two-dimensional grid with the left and right (and the top and bottom) edges stitched together yielding a toroidal array
    /// Stitching the edges implies that the top and the bottom (left and right) edges have the same size
    /// </summary>
    /// <remarks>
    /// Using jagged arrays.
    /// Jagged arrays are faster and each array of a jagged occupies its block of memory (vs multidimensional array is a single block of memory). 
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

            // resize all inner jagged arrays to be the same size (if they are not); the size of the max inner array 
            var gridMaxColumns = grid.Select(r => r.Length).Max();

            torusGrid = new bool[grid.Length][];
            for (int i = 0; i < grid.Length; i++)
            {
                torusGrid[i] = new bool[gridMaxColumns];
                grid[i].CopyTo(torusGrid[i], 0);
            }
        }

        public bool[][] World => torusGrid; 

        /// <summary>
        /// To store new state we could either create identical jagged array, which is memory intensive, 
        /// or we could store new lines in buffer and update existing jagged array when we do not need the line for next neighbours calculations
        /// line buffer technique is used.
        /// 
        /// If we expect life to evolve in parallel then locking or non-blocking synchronisation techniques should be considered. Not supported at the moment
        /// </summary>
        public void Evolve()
        {
            if (torusGrid == null || torusGrid.Length == 0) return;

            var lastCellIndex = torusGrid[0].Length - 1;

            bool[] nextStateFirstRow = null;
            bool[] nextStatePreviousLine = null;
            bool[] nextStateCurrentLine = null;

            for (int i = 0; i < torusGrid.Length; i++) // current row i
            {
                nextStatePreviousLine = nextStateCurrentLine;

                nextStateCurrentLine = new bool[torusGrid[i].Length];

                // array with the count of live cells in corresponding 3 neighbour rows (summed by the same cell/column). 
                var rowLiveCountByColumn = GetCountOfLiveCellsInNeighbourRowsByColumns(torusGrid, i); 

                for (int j = 0; j < torusGrid[i].Length; j++) // current cell j 
                {
                    var isLive = torusGrid[i][j];

                    // if cell index is the first cell then preceding cell is the last cell of the row as we use torus, otherwise just j - 1 
                    // if cell index is the last cell then following cell is the first cell of the row as we use torus, otherwise just j + 1
                    var liveNeighboursCount = rowLiveCountByColumn[j == 0 ? lastCellIndex : j - 1] 
                                              + rowLiveCountByColumn[j] 
                                              + rowLiveCountByColumn[j == lastCellIndex ? 0 : j + 1] 
                                              - (isLive ? 1 : 0); // rowLiveCountByColumn include self cell count hence we need to subtract it 

                    nextStateCurrentLine[j] = EvolveCell(isLive, liveNeighboursCount);
                }

                // update torusGrid
                if (nextStatePreviousLine != null)
                {
                    torusGrid[i - 1] = nextStatePreviousLine;
                }

                if (i == 0) // first row. keep old state until the end as it is required for the last row calculation
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
        /// We get neighbour rows - it is an array of 3 arrays , of 3 rows
        /// If we count live sells in columns ( e.i. in each of 3 arrays at the same cell) we will have summary of "column" neighbour live count
        /// e.g.
        /// NeighbourRows:
        ///     --**--
        ///     -**---
        ///     --**-*
        /// Will result in array:
        ///     013201
        ///  </summary>
        /// <param name="torusGrid"></param>
        /// <param name="rowIndex"></param>
        /// <returns>array of int containing count of live cells in columns(same cell index) across three arrays </returns>
        private static int[] GetCountOfLiveCellsInNeighbourRowsByColumns(bool[][] torusGrid, int rowIndex)
        {
            var neighbourRows = GetNeighbourRows(torusGrid, rowIndex).ToArray();

            int[] liveNeighboursCountPerColumn  = new int[neighbourRows[0].Length];

            for (int j = 0; j < neighbourRows[0].Length; j++) // current cell j 
            {
                // sum live cell at the same position  across the rows 
                int liveCount = 0;
                if (neighbourRows[0][j]) liveCount++;
                if (neighbourRows[1][j]) liveCount++;
                if (neighbourRows[2][j]) liveCount++;
                liveNeighboursCountPerColumn[j] = liveCount;
            }

            return liveNeighboursCountPerColumn;
        }

        /// <summary>
        /// Neighbour Rows are Preceding Row, Row at an index and the Following Row
        /// As we have torus the first row has a last as preceding and the laset row has a first as following
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

        /// <summary>
        /// Any live cell with fewer than two live neighbors dies, as if by underpopulation.
        /// Any live cell with two or three live neighbors lives on to the next generation.
        /// Any live cell with more than three live neighbors dies, as if by overpopulation.
        /// Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
        /// </summary>
        /// <param name="cellValue"></param>
        /// <param name="liveNeighboursCount"></param>
        /// <returns></returns>
        private bool EvolveCell(bool liveCell, int liveNeighboursCount)
        {
            //These rules can be condensed into the following:

            //  Any live cell with two or three neighbors survives.
            if (liveCell && liveNeighboursCount == 2 || liveNeighboursCount == 3) return true;

            //  Any dead cell with three live neighbors becomes a live cell.
            if (!liveCell && liveNeighboursCount == 3) return true;

            //  All other live cells die in the next generation.Similarly, all other dead cells stay dead.
            return false;
        }
    }
}
