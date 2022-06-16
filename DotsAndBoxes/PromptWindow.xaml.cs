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
        public PromptWindow() => InitializeComponent();

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

        private void OKButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            int columns;
            int rows;
            if (int.TryParse(ColumnsTextBox.Text, out columns) && int.TryParse(RowsTextBox.Text, out rows) && columns > 1 && rows > 1)
                DialogResult = true;
        }
    }
}
