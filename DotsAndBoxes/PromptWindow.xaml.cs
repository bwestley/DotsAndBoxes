using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Hopscotch
{
    /// <summary>
    /// Interaction logic for PromptWindow.xaml
    /// </summary>
    public partial class PromptWindow : Window
    {
        public bool? CreateNewGrid;

        public PromptWindow()
        {
            InitializeComponent();
            CreateNewGrid = null;
        }

        public int Columns
        {
            get => int.Parse(ColumnsTextBox.Text);
            set => ColumnsTextBox.Text = value.ToString();
        }
        public int Rows
        {
            get => int.Parse(RowsTextBox.Text);
            set => RowsTextBox.Text = value.ToString();
        }

        private void LoadButtonClicked(object sender, RoutedEventArgs e)
        {
            CreateNewGrid = false;
            DialogResult = true;
        }

        private void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ColumnsTextBox.Text, out int columns) && int.TryParse(RowsTextBox.Text, out int rows) && columns > 1 && rows > 1)
            {
                CreateNewGrid = true;
                DialogResult = true;
            }
        }
    }
}
