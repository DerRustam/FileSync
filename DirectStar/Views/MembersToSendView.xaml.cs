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

namespace DirectStar.Views
{
    /// <summary>
    /// Логика взаимодействия для MembersToSendView.xaml
    /// </summary>
    public partial class MembersToSendView : Window
    {
        private List<string> members;

        public List<string> Selected
        {
            get { return members; }
        }

        public MembersToSendView()
        {
            InitializeComponent();
            members = new List<string>();
            OK_b.IsEnabled = false;
        }

        private void OK_b_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Cancel_b_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            members.Add((sender as CheckBox).Content.ToString());
            if (members.Count != 0)
            {
                OK_b.IsEnabled = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            members.Remove((sender as CheckBox).Content.ToString());
            if(members.Count == 0)
            {
                OK_b.IsEnabled = false;
            }
        }
    }
}
