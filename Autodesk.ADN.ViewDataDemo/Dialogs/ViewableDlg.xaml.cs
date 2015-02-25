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
using Autodesk.ADN.Toolkit.UI;
using Autodesk.ADN.Toolkit.ViewData.DataContracts;

namespace Autodesk.ADN.ViewDataDemo
{
    /// <summary>
    /// Interaction logic for ThumbnailDlg.xaml
    /// </summary>
    public partial class ViewableDlg : Window
    {
        public ViewableDlg(string title, ViewableResponse viewableResponse)
        {
            InitializeComponent();

            Title = "Viewable: " + title;

            _objectBrowserCtrl.Object = viewableResponse;
        }
    }
}
