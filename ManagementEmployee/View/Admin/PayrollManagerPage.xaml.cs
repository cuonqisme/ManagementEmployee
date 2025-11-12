using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ManagementEmployee.ViewModels.Admin;

namespace ManagementEmployee.View.Admin
{
    public partial class PayrollManagerPage : Page
    {
        public PayrollManagerPage()
        {
            InitializeComponent();
            DataContext = new PayrollManagerViewModel();
            Loaded += (_, __) => (DataContext as PayrollManagerViewModel)?.LoadedCommand.Execute(null);

            // Nếu muốn chặn dán/ký tự không phải số cho các ô số, có thể thêm handlers ở đây

        }

        private void AddNumericGuards(params TextBox[] boxes)
        {
            foreach (var tb in boxes)
            {
                if (tb == null) continue;
                tb.PreviewTextInput += NumericOnly;
                DataObject.AddPastingHandler(tb, OnPasteNumericOnly);
            }
        }
        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (!char.IsDigit(c) && c != '.' && c != ',') { e.Handled = true; return; }
        }
        private void OnPasteNumericOnly(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                foreach (char c in text)
                    if (!char.IsDigit(c) && c != '.' && c != ',') { e.CancelCommand(); return; }
            }
            else e.CancelCommand();
        }
    }
}
