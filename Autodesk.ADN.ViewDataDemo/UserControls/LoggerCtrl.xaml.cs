using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Autodesk.ADN.ViewDataDemo
{
    /// <summary>
    /// Interaction logic for LoggerCtrl.xaml
    /// </summary>
    public partial class LoggerCtrl : UserControl
    {
        public LoggerCtrl()
        {
            InitializeComponent();
        }

        string GetTimeStamp()
        {
            DateTime now = DateTime.Now;

            return now.ToString(
                "dd/MM/yyyy - HH:mm:ss",
                CultureInfo.InvariantCulture);
        }

        void AppendText(string text, System.Windows.Media.Brush color, bool bold)
        {
            var doc = _logger.Document;

            Paragraph paragraph = new Paragraph();
            paragraph.Foreground = color;
            paragraph.LineHeight = 5;

            Inline content = new Run(text);

            if (bold)
            {
                content = new Bold(content);
            }

            paragraph.Inlines.Add(content);

            doc.Blocks.Add(paragraph);
        }

        public void LogMessage(
            string msg,
            bool appendDateTime = true,
            string separator = "\n")
        {
            if (appendDateTime)
            {
                AppendText(separator + GetTimeStamp(),
                    System.Windows.Media.Brushes.Blue, true);
            }

            AppendText(msg,
                System.Windows.Media.Brushes.Black, false);

            _logger.ScrollToEnd();
        }

        public void LogError(
            string msg,
            bool appendDateTime = true,
            string separator = "\n")
        {
            if (appendDateTime)
            {
                AppendText(separator + GetTimeStamp(),
                    System.Windows.Media.Brushes.Blue, true);
            }

            AppendText(msg,
                System.Windows.Media.Brushes.Red, false);

            _logger.ScrollToEnd();
        }
    }
}
