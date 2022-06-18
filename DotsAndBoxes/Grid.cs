using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Hopscotch
{
    enum MyEnum
    {

    }

    [Serializable()]
    public class Grid
    {
        public readonly int ColumnCount;
        public readonly int RowCount;
        private List<List<bool>> Columns;
        private List<List<bool>> Rows;
        private List<List<int>> WallCount;

        public Grid(int columnCount, int rowCount)
        {
            ColumnCount = columnCount;
            RowCount = rowCount;

            Columns = Util.Empty2DList(columnCount, rowCount - 1, false); // Column lines are not created in the last row.
            Rows = Util.Empty2DList(columnCount - 1, rowCount, false); // Row lines are not created in the last column.
            WallCount = new();
            RecalculateWallCount();
        }

        public void SetWall(Wall wall, bool set) => SetWall(wall.IsColumn, wall.Column, wall.Row, set);


        public void SetWall(Wall wall) => SetWall(wall.IsColumn, wall.Column, wall.Row, wall.Set);

        public void SetWall(bool isColumn, int column, int row, bool set)
        {
            int wallCountIncrement = set ? 1 : -1;
            if (isColumn)
            {
                Columns[column][row] = set;
                // Update wall counts.
                if (column > 0) WallCount[column - 1][row] += wallCountIncrement; // Not the first column: update the left square.
                if (column < ColumnCount - 1) WallCount[column][row] += wallCountIncrement; // Not the last column: update right square.
            }
            else
            {
                Rows[column][row] = set;
                // Update wall counts.
                if (row > 0) WallCount[column][row - 1] += wallCountIncrement; // Not the first row: update the up square.
                if (row < RowCount - 1) WallCount[column][row] += wallCountIncrement; // Not the last row: update down square.
            }
        }

        public Wall GetWall(bool isColumn, int column, int row)
        {
            return new(isColumn ? Columns[column][row] : Rows[column][row], isColumn, column, row);
        }

        public void RecalculateWallCount()
        {
            WallCount = Util.Empty2DList(ColumnCount - 1, RowCount - 1, 0);
            for (int column = 0; column < ColumnCount; column++)
            {
                for (int row = 0; row < RowCount; row++)
                {
                    if (row < RowCount - 1 && GetWall(true, column, row).Set)
                    {
                        // Not the last row and the column wall is set.
                        if (column < ColumnCount - 1) WallCount[column][row]++; // Right square.
                        if (column > 0) WallCount[column - 1][row]++; // Left square.
                    }
                    if (column < ColumnCount - 1 && GetWall(false, column, row).Set)
                    {
                        // Not the last column and the row wall is set.
                        if (row < RowCount - 1) WallCount[column][row]++; // Below square.
                        if (row > 0) WallCount[column][row - 1]++; // Above square.
                    }
                }
            }
        }

        public SquareWalls GetSquareWalls(int column, int row)
        {
            if (column < 0 || column >= ColumnCount - 1 || row < 0 || row >= RowCount - 1) throw new IndexOutOfRangeException();
            SquareWalls squareWalls = new(
                new(Rows[column][row], false, column, row),
                new(Columns[column + 1][row], true, column + 1, row),
                new(Rows[column][row + 1], false, column, row + 1),
                new(Columns[column][row], true, column, row)
            );
            return squareWalls;
        }

        public int GetWallCount(int column, int row)
        {
            return WallCount[column][row];
        }

        public bool IsEnclosed(int column, int row) => GetWallCount(column, row) == 4;

        public HashSet<Wall> GetOptimalMoves()
        {
            HashSet<Wall> optimalMoves = new();

            // Find unset walls on squares with three set walls.
            for (int column = 0; column < ColumnCount - 1; column++)
            {
                for (int row = 0; row < RowCount - 1; row++)
                {
                    if (GetWallCount(column, row) == 3)
                    {
                        // The square has three walls. Fill in the last wall to complete the square.

                        SquareWalls squareWalls = GetSquareWalls(column, row);
                        // Exactly one of the below conditions should evaluate to true.
                        if (!squareWalls.Top.Set) optimalMoves.Add(squareWalls.Top); // Top
                        if (!squareWalls.Right.Set) optimalMoves.Add(squareWalls.Right); // Right
                        if (!squareWalls.Bottom.Set) optimalMoves.Add(squareWalls.Bottom); // Bottom
                        if (!squareWalls.Left.Set) optimalMoves.Add(squareWalls.Left); // Left
                    }
                }
            }
            if (optimalMoves.Count > 0) return optimalMoves;

            // Find walls with adjacent squares with less than two walls.
            for (int column = 0; column < ColumnCount - 1; column++)
            {
                for (int row = 0; row < RowCount - 1; row++)
                {
                    if (GetWallCount(column, row) < 2)
                    {
                        // The square has less than two walls.

                        SquareWalls squareWalls = GetSquareWalls(column, row);
                        if (!squareWalls.Top.Set && (row == 0 || GetWallCount(column, row - 1) < 2))
                            // The top wall is not set and the above square (if one exists) has less than two walls.
                            optimalMoves.Add(squareWalls.Top);
                        if (!squareWalls.Right.Set && (column == ColumnCount - 2 || GetWallCount(column + 1, row) < 2))
                            // The right wall is not set and the right square (if one exists) has less than two walls.
                            optimalMoves.Add(squareWalls.Right);
                        if (!squareWalls.Bottom.Set && (row == RowCount - 2 || GetWallCount(column, row + 1) < 2))
                            // The bottom wall is not set and the below square (if one exists) has less than two walls.
                            optimalMoves.Add(squareWalls.Bottom);
                        if (!squareWalls.Left.Set && (column == 0 || GetWallCount(column - 1, row) < 2))
                            // The left wall is not set and the left square (if one exists) has less than two walls.
                            optimalMoves.Add(squareWalls.Left);
                    }
                }
            }
            if (optimalMoves.Count > 0) return optimalMoves;

            // Find walls that each trigger a shortest chain (multiple chains may have the least length).
            // Save grid data because it will be temporarly mutated when determining chains (this helps
            // simplify cases where chains wrap around). When a chain ends along its length (not at its
            // other end to form a perfect loop) walls in its "tail" will result in a shorter chain.
            List<List<bool>> columnsCopy = Columns.ConvertAll(column => new List<bool>(column));
            List<List<bool>> rowsCopy = Rows.ConvertAll(column => new List<bool>(column));
            List<List<int>> wallCountCopy = WallCount.ConvertAll(column => new List<int>(column));

            void RestoreGridData()
            {
                Columns = columnsCopy.ConvertAll(column => new List<bool>(column));
                Rows = rowsCopy.ConvertAll(column => new List<bool>(column));
                WallCount = wallCountCopy.ConvertAll(column => new List<int>(column));
            }

            int GetChainLength(int currentColumn, int currentRow)
            {
                Debug.WriteLine($"Evaluating chain ({currentColumn}, {currentRow}).");
                int chainLength = 0;
                SquareWalls squareWalls;

                // While the current position is in squares and the current square has three walls.
                while (currentColumn >= 0 && currentColumn < ColumnCount - 1 && currentRow >= 0 && currentRow < RowCount - 1 && GetWallCount(currentColumn, currentRow) == 3)
                {
                    Debug.WriteLine($"Coordinate: ({currentColumn}, {currentRow}), Length: {chainLength}.");

                    // Set the unset wall and move in that direction to the next square.
                    squareWalls = GetSquareWalls(currentColumn, currentRow);
                    if (!squareWalls.Top.Set)
                    {
                        Debug.WriteLine("Top");
                        SetWall(squareWalls.Top, true);
                        currentRow--;
                    }
                    else if (!squareWalls.Right.Set)
                    {
                        Debug.WriteLine("Right");
                        SetWall(squareWalls.Right, true);
                        currentColumn++;
                    }
                    else if (!squareWalls.Bottom.Set)
                    {
                        Debug.WriteLine("Bottom");
                        SetWall(squareWalls.Bottom, true);
                        currentRow++;
                    }
                    else if (!squareWalls.Left.Set)
                    {
                        Debug.WriteLine("Left");
                        SetWall(squareWalls.Left, true);
                        currentColumn--;
                    }
                    else break;
                    
                    // A square must have been made. Increase the length of this chain.
                    chainLength++;

                    // Handles the edge case where the last line results in two boxes instead of one.
                    if (currentColumn >= 0 && currentColumn < ColumnCount - 1 && currentRow >= 0 && currentRow < RowCount - 1 && GetWallCount(currentColumn, currentRow) == 4) chainLength++;
                }

                Debug.WriteLine($"Chain length: {chainLength}.");
                return chainLength;
            }

            // Get the length of each chain triggered by a wall.
            Dictionary<Wall, int> chainlengths = new();
            for (int column = 0; column < ColumnCount - 1; column++)
            {
                for (int row = 0; row < RowCount - 1; row++)
                {
                    if (GetWallCount(column, row) == 2)
                    {
                        // The square has two walls.

                        SquareWalls firstSquare = GetSquareWalls(column, row);

                        // For each unset of wall of this square, calculate (if not already done) the length of the chain triggered if it was set.
                        // Both the length of "sides" of the chain need to be calculated. The first starts in the square we are already in and
                        // the second starts in the square in direction of the triggering wall.
                        if (!firstSquare.Top.Set && !chainlengths.ContainsKey(firstSquare.Top)) {
                            Wall chainStart = firstSquare.Top;
                            SetWall(chainStart, true);
                            chainlengths[chainStart] = GetChainLength(column, row) + GetChainLength(column, row - 1);
                            RestoreGridData();
                        }
                        if (!firstSquare.Right.Set && !chainlengths.ContainsKey(firstSquare.Right))
                        {
                            Wall chainStart = firstSquare.Right;
                            SetWall(chainStart, true);
                            chainlengths[chainStart] = GetChainLength(column, row) + GetChainLength(column + 1, row);
                            RestoreGridData();
                        }
                        if (!firstSquare.Bottom.Set && !chainlengths.ContainsKey(firstSquare.Bottom))
                        {
                            Wall chainStart = firstSquare.Bottom;
                            SetWall(chainStart, true);
                            chainlengths[chainStart] = GetChainLength(column, row) + GetChainLength(column, row + 1);
                            RestoreGridData();
                        }
                        if (!firstSquare.Left.Set && !chainlengths.ContainsKey(firstSquare.Left))
                        {
                            Wall chainStart = firstSquare.Left;
                            SetWall(chainStart, true);
                            chainlengths[chainStart] = GetChainLength(column, row) + GetChainLength(column - 1, row);
                            RestoreGridData();
                        }
                    }
                }
            }

            // Get the walls that each trigger the shortest chain.
            int minimumLength = int.MaxValue;
            foreach (var pair in chainlengths) Debug.WriteLine($"{(pair.Key.IsColumn ? "Column" : "Row")} ({pair.Key.Column}, {pair.Key.Row}) generates a chain with length {pair.Value}.");
            foreach (var length in chainlengths.Values) if (length < minimumLength) minimumLength = length;
            foreach (var pair in chainlengths) if (pair.Value == minimumLength) optimalMoves.Add(pair.Key);

            return optimalMoves;
        }
    }
}
