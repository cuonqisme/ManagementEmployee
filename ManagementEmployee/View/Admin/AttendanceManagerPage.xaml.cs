using ManagementEmployee.Models;
using ManagementEmployee.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ManagementEmployee.View.Admin
{
    public partial class AttendanceManagerPage : Page
    {
        private readonly AttendanceManagerViewModel _vm = new AttendanceManagerViewModel();

        public AttendanceManagerPage()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += async (_, __) => await _vm.InitAsync();
        }
    }
}
