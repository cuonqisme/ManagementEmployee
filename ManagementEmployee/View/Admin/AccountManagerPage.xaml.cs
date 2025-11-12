using System.Windows.Controls;
using ManagementEmployee.ViewModels;

namespace ManagementEmployee.View.Admin
{
    public partial class AccountManagerPage : Page
    {
        public AccountManagerPage()
        {
            InitializeComponent();
            DataContext = new AccountManagerViewModel();
        }
    }
}
