using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ServerListener
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   public partial class App : Application
   {
      private void AppStartup(object sender, StartupEventArgs e)
      {
         MainWindow mw = new MainWindow();
         mw.DataContext = ViewModels.ServerViewModel.Instance;
         mw.Show();
      }
   }
}
