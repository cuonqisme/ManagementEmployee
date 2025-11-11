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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ManagementEmployee.View.Login
{
    /// <summary>
    /// Interaction logic for MyTextBox.xaml
    /// </summary>
    public partial class MyTextBox : UserControl
    {
        public MyTextBox()
        {
            InitializeComponent();
            UpdateHintVisibility();
            textBox.TextChanged += TextBox_TextChanged;
        }

        public string Hint
        {
            get { return (string)GetValue(HintProperty); }
            set { SetValue(HintProperty, value); }
        }

        public static readonly DependencyProperty HintProperty = DependencyProperty.Register(
            "Hint", typeof(string), typeof(MyTextBox));

        public string Text
        {
            get { return textBox.Text; }
            set { textBox.Text = value; }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateHintVisibility();
        }

        private void UpdateHintVisibility()
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBlockHint.Visibility = Visibility.Visible;
            }
            else
            {
                textBlockHint.Visibility = Visibility.Collapsed;
            }
        }
    }
}
