using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Hopscotch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const bool LOAD = true;
        private const bool SAVE = true;
        private const double ORIGIN_X = 10;
        private const double ORIGIN_Y = 10;
        private const double SCALE = 50;

        private readonly SolidColorBrush TRANSPARENT = new(Color.FromArgb(0, 0, 0, 0));
        private readonly List<SolidColorBrush> SQUARE_FILL = new() {
            new(Color.FromArgb(0, 0, 0, 0)),
            new(Color.FromArgb(0, 0, 0, 0)),
            new(Color.FromRgb(255, 255, 0)),
            new(Color.FromRgb(0, 0, 255)),
            new(Color.FromRgb(0, 0, 0))
        };
        private readonly SolidColorBrush DOT_FILL = new(Color.FromRgb(0, 0, 0));
        private readonly SolidColorBrush LINE_STROKE = new(Color.FromRgb(100, 100, 100));
        private readonly SolidColorBrush OPTIMAL_MOVE_STROKE = new(Color.FromRgb(0, 255, 0));

        private Grid Grid_;
        private List<List<Line>> RowLines;
        private List<List<Line>> ColumnLines;
        private List<List<Rectangle>> Squares;

        public MainWindow()
        {
            InitializeComponent();

            // Maximize
            Application.Current.MainWindow.WindowState = WindowState.Maximized;

            // Load grid from file or create a new one.
            if (LOAD)
            {
                try
                {
                    using Stream stream = File.Open(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/grid.bin", FileMode.Open);
                    BinaryFormatter bin = new BinaryFormatter();
                    Grid_ = (Grid)bin.Deserialize(stream);
                }
                catch (Exception)
                {
                    var dialog = new PromptWindow();
                    dialog.ShowDialog();
                    Grid_ = new Grid(dialog.Columns, dialog.Rows);
                }
            }
            else
            {
                var dialog = new PromptWindow();
                dialog.ShowDialog();
                Grid_ = new Grid(dialog.Columns, dialog.Rows);
            }

            // Get optimal moves.
            HashSet<Wall> optimalMoves = Grid_.GetOptimalMoves();

            // Create squares and column and row lines.
            ColumnLines = new List<List<Line>>();
            RowLines = new List<List<Line>>();
            Squares = new List<List<Rectangle>>();

            Wall wall;

            for (int column = 0; column < Grid_.ColumnCount; column++)
            {
                if (column < Grid_.ColumnCount - 1)
                    Squares.Add(new List<Rectangle>());
                ColumnLines.Add(new List<Line>());
                RowLines.Add(new List<Line>());
                for (int row = 0; row < Grid_.RowCount; row++)
                {
                    if (row < Grid_.RowCount - 1)
                    {
                        if (column < Grid_.ColumnCount - 1)
                        {
                            // Draw a square.
                            Rectangle square = new()
                            {
                                Width = SCALE,
                                Height = SCALE,
                                Fill = SQUARE_FILL[Grid_.GetWallCount(column, row)]
                            };
                            Canvas.SetLeft(square, column * SCALE + ORIGIN_X);
                            Canvas.SetTop(square, row * SCALE + ORIGIN_Y);
                            Canvas.SetZIndex(square, -column * Grid_.ColumnCount - row);
                            MainCanvas.Children.Add(square);
                            Squares[column].Add(square);
                        }

                        // Draw a column.
                        wall = Grid_.GetWall(true, column, row);
                        Line line = new()
                        {
                            X1 = column * SCALE + ORIGIN_X,
                            Y1 = row * SCALE + ORIGIN_Y,
                            X2 = column * SCALE + ORIGIN_X,
                            Y2 = (row + 1) * SCALE + ORIGIN_Y,
                            Stroke = wall.Set ? LINE_STROKE : optimalMoves.Contains(wall) ? OPTIMAL_MOVE_STROKE : TRANSPARENT,
                            StrokeThickness = 10
                        };
                        MainCanvas.Children.Add(line);
                        ColumnLines[column].Add(line);
                    }
                    if (column < Grid_.ColumnCount - 1)
                    {
                        // Draw a row.
                        wall = Grid_.GetWall(false, column, row);
                        Line line = new()
                        {
                            X1 = column * SCALE + ORIGIN_X,
                            Y1 = row * SCALE + ORIGIN_Y,
                            X2 = (column + 1) * SCALE + ORIGIN_X,
                            Y2 = row * SCALE + ORIGIN_Y,
                            Stroke = wall.Set ? LINE_STROKE : optimalMoves.Contains(wall) ? OPTIMAL_MOVE_STROKE : TRANSPARENT,
                            StrokeThickness = 10
                        };
                        MainCanvas.Children.Add(line);
                        RowLines[column].Add(line);
                    }

                    // Draw a dot.
                    Ellipse ellipse = new()
                    {
                        Fill = DOT_FILL,
                        Width = 10,
                        Height = 10
                    };
                    Canvas.SetLeft(ellipse, column * SCALE + ORIGIN_X - 10 / 2);
                    Canvas.SetTop(ellipse, row * SCALE + ORIGIN_Y - 10 / 2);
                    Canvas.SetZIndex(ellipse, 200000 + column * Grid_.ColumnCount + row);
                    MainCanvas.Children.Add(ellipse);
                }
            }

            // Register event listeners.
            Closing += Cleanup;
            CompositionTarget.Rendering += Render;
            MouseUp += Clicked;
        }

        private void Cleanup(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save grid to file.
            if (!SAVE) return;
            try
            {
                // Save data file.
                using Stream stream = File.Open(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/grid.bin", FileMode.Create);
                BinaryFormatter bin = new BinaryFormatter();
                bin.Serialize(stream, Grid_);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Render(object? sender, EventArgs e)
        {
            
        }

        private void Clicked(object? sender, MouseButtonEventArgs e)
        {
            // Determine the wall clicked on.
            Point point = e.GetPosition(MainCanvas);
            int column = (int)((point.X - ORIGIN_X)/ SCALE);
            double columnRemainder = (point.X - ORIGIN_X) % SCALE;
            int row = (int)((point.Y - ORIGIN_Y) / SCALE);
            double rowRemainder = (point.Y - ORIGIN_Y) % SCALE;
            bool isColumn;

            if (columnRemainder > rowRemainder) // column + 1 or row
            {
                if (SCALE - columnRemainder < rowRemainder) // column += 1
                {
                    column++;
                    isColumn = true;
                }
                else // row
                {
                    isColumn = false;
                }
            }
            else // row + 1 or column
            {
                if (SCALE - rowRemainder < columnRemainder) // row + 1
                {
                    row++;
                    isColumn = false;
                }
                else // column
                {
                    isColumn = true;
                }
            }


            if (isColumn) {
                if (row < 0 || row >= Grid_.RowCount - 1 || column < 0 || column >= Grid_.ColumnCount) return; // Click out of bounds.

                // Toggle a column.
                Wall wall = Grid_.GetWall(true, column, row);
                Grid_.SetWall(wall, !wall.Set);
                // ColumnLines[column][row] = !wall.Set ? LINE_STROKE : TRANSPARENT;
            }
            else
            {
                if (row < 0 || row >= Grid_.RowCount || column < 0 || column >= Grid_.ColumnCount - 1) return; // Click out of bounds.

                // Toggle a row.
                Wall wall = Grid_.GetWall(false, column, row);
                Grid_.SetWall(wall, !wall.Set);
                // RowLines[column][row] = !wall.Set ? LINE_STROKE : TRANSPARENT;
            }

            // Update squares.
            if (isColumn)
            {
                if (column > 0)
                    Squares[column - 1][row].Fill = SQUARE_FILL[Grid_.GetWallCount(column - 1, row)]; // Update left square.
                if (column < Grid_.ColumnCount - 1)
                    Squares[column][row].Fill = SQUARE_FILL[Grid_.GetWallCount(column, row)]; // Update right square.
            }
            else
            {
                if (row > 0)
                    Squares[column][row - 1].Fill = SQUARE_FILL[Grid_.GetWallCount(column, row - 1)]; // Update up square.
                if (row < Grid_.RowCount - 1)
                    Squares[column][row].Fill = SQUARE_FILL[Grid_.GetWallCount(column, row)]; // Update down square.
            }

            // Update the stroke of all lines.
            HashSet<Wall> optimalMoves = Grid_.GetOptimalMoves();
            Wall wall1;
            for (int column1 = 0; column1 < Grid_.ColumnCount; column1++)
            {
                for (int row1 = 0; row1 < Grid_.RowCount; row1++)
                {
                    if (row1 < Grid_.RowCount - 1)
                    {
                        // Column lines do not exist in the last row.
                        wall1 = Grid_.GetWall(true, column1, row1);
                        ColumnLines[column1][row1].Stroke = wall1.Set ? LINE_STROKE : optimalMoves.Contains(wall1) ? OPTIMAL_MOVE_STROKE : TRANSPARENT;
                    }
                    if (column1 < Grid_.ColumnCount - 1)
                    {
                        // Row lines do not exist in the last column.
                        wall1 = Grid_.GetWall(false, column1, row1);
                        RowLines[column1][row1].Stroke = wall1.Set ? LINE_STROKE : optimalMoves.Contains(wall1) ? OPTIMAL_MOVE_STROKE : TRANSPARENT;
                    }
                }
            }
        }
    }
}
