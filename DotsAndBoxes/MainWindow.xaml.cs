using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
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

        private readonly Grid Grid_;
        private readonly List<List<Line>> RowLines;
        private readonly List<List<Line>> ColumnLines;
        private readonly List<List<Rectangle>> Squares;

        private double Scale;
        private double OriginX;
        private double OriginY;
        private readonly Timer ResizeTimer = new(100) { Enabled = false };

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MainWindow()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            InitializeComponent();

            // Maximize
            Application.Current.MainWindow.WindowState = WindowState.Maximized;

            // Load grid from file or create a new one.
            while (true)
            {
                PromptWindow dialog = new();
                dialog.ShowDialog();
                bool? result = dialog.CreateNewGrid;
                if (result == true)
                {
                    Grid_ = new Grid(dialog.Columns, dialog.Rows);
                    break;
                }
                else if (result == false)
                {
                    try
                    {
                        Microsoft.Win32.OpenFileDialog fileDialog = new()
                        {
                            Title = "Load Grid From File"
                        };
                        if (fileDialog.ShowDialog() == true)
                        {
                            using Stream stream = fileDialog.OpenFile();
                            BinaryFormatter bin = new();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                            Grid_ = (Grid)bin.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Unable to load grid from a file: " + e.Message);
                    }
                }
                else
                {
                    Application.Current.Shutdown();
                    return;
                }
            }

            // Create squares and column and row lines.
            ColumnLines = new List<List<Line>>();
            RowLines = new List<List<Line>>();
            Squares = new List<List<Rectangle>>();
            Redraw();

            // Register event listeners.
            Closing += Cleanup;
            MouseUp += Clicked;
            ResizeTimer.Elapsed += new ElapsedEventHandler(ResizingDone);
            SizeChanged += Resized;
        }

        private void Cleanup(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save grid to file.
            try
            {
                // Save data file.
                Microsoft.Win32.SaveFileDialog fileDialog = new();
                if (fileDialog.ShowDialog() == true)
                {
                    using Stream stream = fileDialog.OpenFile();
                    BinaryFormatter bin = new();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    bin.Serialize(stream, Grid_);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ResizingDone(object? sender, ElapsedEventArgs e)
        {
            ResizeTimer.Stop();
            Dispatcher.Invoke(Redraw);
        }

        private void Resized(object sender, SizeChangedEventArgs e)
        {
            ResizeTimer.Stop();
            ResizeTimer.Start();
        }

        private void Redraw()
        {
            Scale = Math.Min(MainCanvas.ActualWidth / (Grid_.ColumnCount - 1), MainCanvas.ActualHeight / (Grid_.RowCount - 1)) * 0.9;
            OriginX = (MainCanvas.ActualWidth - Scale * (Grid_.ColumnCount - 1)) / 2;
            OriginY = (MainCanvas.ActualHeight - Scale * (Grid_.RowCount - 1)) / 2;

            if (double.IsNaN(Scale) || double.IsNaN(OriginX) || double.IsNaN(OriginY))
            {
                Scale = 10;
                OriginX = 20;
                OriginY = 20;
            }

            // Get optimal moves.
            HashSet<Wall> optimalMoves = Grid_.GetOptimalMoves();

            MainCanvas.Children.Clear();
            ColumnLines.Clear();
            RowLines.Clear();
            Squares.Clear();

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
                                Width = Scale,
                                Height = Scale,
                                Fill = SQUARE_FILL[Grid_.GetWallCount(column, row)]
                            };
                            Canvas.SetLeft(square, column * Scale + OriginX);
                            Canvas.SetTop(square, row * Scale + OriginY);
                            Canvas.SetZIndex(square, -column * Grid_.ColumnCount - row);
                            MainCanvas.Children.Add(square);
                            Squares[column].Add(square);
                        }

                        // Draw a column.
                        wall = Grid_.GetWall(true, column, row);
                        Line line = new()
                        {
                            X1 = column * Scale + OriginX,
                            Y1 = row * Scale + OriginY,
                            X2 = column * Scale + OriginX,
                            Y2 = (row + 1) * Scale + OriginY,
                            Stroke = wall.Set ? LINE_STROKE : optimalMoves.Contains(wall) ? OPTIMAL_MOVE_STROKE : TRANSPARENT,
                            StrokeThickness = Scale / 5
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
                            X1 = column * Scale + OriginX,
                            Y1 = row * Scale + OriginY,
                            X2 = (column + 1) * Scale + OriginX,
                            Y2 = row * Scale + OriginY,
                            Stroke = wall.Set ? LINE_STROKE : optimalMoves.Contains(wall) ? OPTIMAL_MOVE_STROKE : TRANSPARENT,
                            StrokeThickness = Scale / 5
                        };
                        MainCanvas.Children.Add(line);
                        RowLines[column].Add(line);
                    }

                    // Draw a dot.
                    Ellipse ellipse = new()
                    {
                        Fill = DOT_FILL,
                        Width = Scale / 5,
                        Height = Scale / 5
                    };
                    Canvas.SetLeft(ellipse, (column - 0.1) * Scale + OriginX);
                    Canvas.SetTop(ellipse, (row - 0.1) * Scale + OriginY);
                    Canvas.SetZIndex(ellipse, 200000 + column * Grid_.ColumnCount + row);
                    MainCanvas.Children.Add(ellipse);
                }
            }
        }

        private void Clicked(object? sender, MouseButtonEventArgs e)
        {
            // Determine the wall clicked on.
            Point point = e.GetPosition(MainCanvas);
            int column = (int)((point.X - OriginX)/ Scale);
            double columnRemainder = (point.X - OriginX) % Scale;
            int row = (int)((point.Y - OriginY) / Scale);
            double rowRemainder = (point.Y - OriginY) % Scale;
            bool isColumn;

            if (columnRemainder > rowRemainder) // column + 1 or row
            {
                if (Scale - columnRemainder < rowRemainder) // column += 1
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
                if (Scale - rowRemainder < columnRemainder) // row + 1
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
