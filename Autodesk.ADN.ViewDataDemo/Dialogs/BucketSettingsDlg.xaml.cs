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
using System.Windows.Shapes;
using Autodesk.ADN.Toolkit.ViewData.DataContracts;

namespace Autodesk.ADN.ViewDataDemo
{
    /// <summary>
    /// Interaction logic for BucketSettingsDlg.xaml
    /// </summary>
    public partial class BucketSettingsDlg : Window
    {
        class BucketPolicyItem
        {
            string _text;

            public BucketPolicyEnum Value
            {
                get;
                private set;
            }

            public BucketPolicyItem(
                string text, 
                BucketPolicyEnum value)
            {
                _text = text;
                Value = value;
            }

            public override string ToString()
            {
                return _text;
            }
        }

        public BucketSettingsDlg()
        {
            InitializeComponent();

            string sceneName = "ADN-" + DateTime.Now.ToString(
                "dd.MM.yyyy-HH.mm.ss",
                CultureInfo.InvariantCulture);

            _tbBucketName.Text = sceneName;

            _cbBucketPolicy.Items.Add(
                new BucketPolicyItem(
                    "Transient", 
                    BucketPolicyEnum.kTransient));

            _cbBucketPolicy.Items.Add(
               new BucketPolicyItem(
                   "Temporary",
                   BucketPolicyEnum.kTemporary));

            _cbBucketPolicy.Items.Add(
               new BucketPolicyItem(
                   "Persistent",
                   BucketPolicyEnum.kPersistent));

            _cbBucketPolicy.SelectedIndex = 2;
        }

        public string BucketName
        {
            get
            {
                return _tbBucketName.Text;
            }
        }

        public BucketPolicyEnum BucketPolicy
        {
            get
            {
                var item = _cbBucketPolicy.SelectedItem
                    as BucketPolicyItem;

                return item.Value;
            }
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void tbSceneName_TextChanged(object sender, EventArgs e)
        {
            if (_tbBucketName.Text.Length == 0)
            {
                bOK.IsEnabled = false;
            }
            else
            {
                bOK.IsEnabled = true;
            }
        }
    }
}