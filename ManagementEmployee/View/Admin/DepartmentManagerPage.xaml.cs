using ManagementEmployee.ViewModels;
using ManagementEmployee.ViewModels.Admin;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ManagementEmployee.View.Admin
{
    public partial class DepartmentManagerPage : Page
    {
        private DepartmentManagerViewModel VM => DataContext as DepartmentManagerViewModel;

        public DepartmentManagerPage()
        {
            InitializeComponent();
            DataContext = new DepartmentManagerViewModel();

            Loaded += (_, __) => VM.LoadedCommand.Execute(null);
        }

        // Bridge nút Assign -> VM.AssignAsync(SelectedItems)
        private async void BtnAssign_Click(object sender, RoutedEventArgs e)
        {
            IList selected = lbAvailable?.SelectedItems;
            await VM.AssignAsync(selected);
        }

        // Bridge nút Remove -> VM.RemoveAsync(SelectedItems)
        private async void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            IList selected = dgDeptEmployees?.SelectedItems;
            await VM.RemoveAsync(selected);
        }
    }
}
