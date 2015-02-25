using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Autodesk.ADN.Toolkit.UI;
using Autodesk.ADN.Toolkit.ViewData;
using Autodesk.ADN.Toolkit.ViewData.DataContracts;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Autodesk.ADN.ViewDataDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TreeItem _rootNode;

        GridLength _thumbnailWidth;

        string _supportedFileFilter = " Supported Formats |*.*";

        static AdnViewDataClient _viewDataClient;

        /////////////////////////////////////////////////////////////////////////////////
        // Constructor
        //
        /////////////////////////////////////////////////////////////////////////////////
        public MainWindow()
        {
            InitializeComponent();

            _viewDataClient = new AdnViewDataClient(
               Credentials.BASE_URL,
               Credentials.CONSUMER_KEY,
               Credentials.CONSUMER_SECRET);

            Authenticate();

            this.Closing += MainWindow_Closing;
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //Uncomment to display save prompt

            //var result = System.Windows.MessageBox.Show(
            //   "Save Buckets?",
            //   "Buckets Config",
            //   MessageBoxButton.YesNoCancel, 
            //   MessageBoxImage.Question, 
            //   MessageBoxResult.Yes, 
            //   MessageBoxOptions.None);

            var result = MessageBoxResult.Yes;

            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            else if (result == MessageBoxResult.Yes)
            {
                if (_rootNode == null)
                    return;

                SaveNodes();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async void Authenticate()
        {
            var tokenResult = await _viewDataClient.AuthenticateAsync();

            if (!tokenResult.IsOk())
            {
                _logger.LogError(
                    "Authentication failed: " + tokenResult.Error.Reason);
                
                return;
            }

            _logger.LogMessage("Login successful :)");
            _logger.LogMessage("Access Token: " + tokenResult.AccessToken, false);
            _logger.LogMessage("Expiration Time (sec): " + tokenResult.ExpirationTime, false);
            _logger.LogMessage("Token Type: " + tokenResult.TokenType, false);

            _rootNode = AddRootNode("Buckets");

            ExpandNode(_rootNode);

            foreach(var bucket in LoadNodes())
            {
                var bucketResponse = 
                    await _viewDataClient.GetBucketDetailsAsync(
                        bucket.BucketKey);

                if (!bucketResponse.IsOk())
                    continue;

                var node = _rootNode.AddNode(new TreeItem(bucketResponse));           

                foreach (var obj in bucket.Objects)
                {
                    var objectResponse = await _viewDataClient.GetObjectDetailsAsync(
                        bucket.BucketKey, obj.ObjectKey);

                    if (!objectResponse.IsOk())
                        continue;

                    foreach (var objDetails in objectResponse.Objects)
                    {
                        var item = new TreeItem(objDetails);

                        var thumbnailResponse = 
                            await item.LoadThumbnail();

                        node.AddNode(item);
                    }   
                }
            }

            var formatResponse = await _viewDataClient.GetSupportedFormats();

            if (formatResponse.IsOk())
            {
                string fileTypes = "";

                foreach (var fileType in formatResponse.Extensions)
                    fileTypes += "*." + fileType + ";";

                _supportedFileFilter = "Supported Formats |" + fileTypes;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        void ExpandNode(object node)
        {
            var generator = _treeView.ItemContainerGenerator;

            var item = generator.ContainerFromItem(node)
                as TreeViewItem;

            if(item != null)
                item.IsExpanded = true;
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        private TreeItem SelectedItem
        {
            get
            {
                return _treeView.SelectedItem
                    as TreeItem;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        TreeItem AddRootNode(string name)
        {
            _rootNode = new TreeItem(
                name,
                Properties.Resources.folder_open);

            ObservableCollection<TreeItem> nodes =
                new ObservableCollection<TreeItem>();

            nodes.Add(_rootNode);

            _treeView.ItemsSource = nodes;

            return _rootNode;
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async private void OnTreeViewSelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedItem != null)
            {
                _propertyGrid.SelectedObject = SelectedItem.Tag;

                if (SelectedItem.Thumbnail != null)
                {
                    _thumbnail.Source = BitmapConverter.ToBitmapSource(
                        new System.Drawing.Bitmap(SelectedItem.Thumbnail));

                    _viewGrid.ColumnDefinitions[3].Width = new GridLength(5);

                    _viewGrid.ColumnDefinitions[4].Width =
                        (_thumbnailWidth.Value != 0 ? _thumbnailWidth : new GridLength(300));
                }
                else
                {
                    _thumbnail.Source = null;

                    _viewGrid.ColumnDefinitions[3].Width = new GridLength(0);
                    _viewGrid.ColumnDefinitions[4].Width = new GridLength(0);  
                }

                if (SelectedItem.Tag == null)
                {
                    _treeView.ContextMenu = _treeView.Resources["RootCtx"]
                        as System.Windows.Controls.ContextMenu;
                }
                else if (SelectedItem.Tag is BucketDetailsResponse)
                {
                    _treeView.ContextMenu = _treeView.Resources["BucketItemCtx"]
                        as System.Windows.Controls.ContextMenu;
                }
                else if (SelectedItem.Tag is ObjectDetails)
                {
                    _treeView.ContextMenu = _treeView.Resources["ObjectItemCtx"]
                        as System.Windows.Controls.ContextMenu;

                    var thumbnailResponse =
                        await SelectedItem.LoadThumbnail();
                }
            }
        }

        void OnGridSizeChanged(object sender, EventArgs e)
        {
            if (_viewGrid.ColumnDefinitions[4].Width.Value > 0)
            {
                _thumbnailWidth = _viewGrid.ColumnDefinitions[4].Width;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async void CreateBucket_Click(object sender, RoutedEventArgs e)
        {
            BucketSettingsDlg settingsDlg = new BucketSettingsDlg();

            settingsDlg.Owner = this.Parent as Window;

            settingsDlg.ShowDialog();

            if (!settingsDlg.DialogResult.HasValue || !settingsDlg.DialogResult.Value)
                return;
            
            var bucketData = new BucketCreationData(
                settingsDlg.BucketName,
                settingsDlg.BucketPolicy);

            var bucketResponse = await _viewDataClient.CreateBucketAsync(bucketData);

            if (!bucketResponse.IsOk())
            {
                _logger.LogError("Bucket creation failed: " +
                    bucketResponse.Error.StatusCode.ToString());

                return;
            }

            _rootNode.AddNode(
                new TreeItem(bucketResponse));

            _logger.LogMessage("Bucket creation successful: " +
                settingsDlg.BucketName);
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            string[] files = UIHelper.FileSelect(
                   "Select Files",
                   _supportedFileFilter,
                   true);

            if (files == null)
                return;

            if (SelectedItem == null)
                return;

            if (!(SelectedItem.Tag is BucketDetailsResponse))
                return;

            var item = SelectedItem;

            var bucketResponse = item.Tag as BucketDetailsResponse;

            foreach (var file in files)
            {
                FileUploadInfo fi = FileUploadInfo.CreateFromFile(
                    System.IO.Path.GetFileName(file),
                    file);

                if (fi == null)
                {
                    _logger.LogError("Failed to read file: " + file);
                    _logger.LogError("Make sure it is not open by another application.", false);
                    continue;
                }

                ObjectDetailsResponse objectResponse =
                    await _viewDataClient.UploadFileAsync(
                        bucketResponse.BucketKey, fi);

                if (!objectResponse.IsOk())
                {
                    _logger.LogError(objectResponse.Error.Reason);
                    continue;
                }

                foreach (var obj in objectResponse.Objects)
                {
                    _logger.LogMessage("File upload sucessful: " + obj.ObjectKey);

                    item.AddNode(new TreeItem(obj));

                    ExpandNode(item);

                    string fileId = obj.FileId;

                    RegisterResponse registerResponse =
                        await _viewDataClient.RegisterAsync(
                           fileId);

                    if (!registerResponse.IsOk())
                    {
                        _logger.LogError(registerResponse.Error.Reason);
                        return;
                    }

                    _logger.LogMessage(
                        "File translation result: " + 
                        registerResponse.Result);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async void RefreshBucketItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is BucketDetailsResponse))
                return;

            var item = SelectedItem;

            var bucketResponse = item.Tag as BucketDetailsResponse;

            bucketResponse = await _viewDataClient.GetBucketDetailsAsync(
                bucketResponse.BucketKey);

            if (!bucketResponse.IsOk())
            {
                _logger.LogError(bucketResponse.Error.StatusCode.ToString());
                return;
            }

            item.Tag = bucketResponse;

            _logger.LogMessage("Bucket Details refreshed: " + bucketResponse.BucketKey);
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async void RefreshObjectItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is ObjectDetails))
                return;

            var item = SelectedItem;

            var objectDetails = item.Tag as ObjectDetails;

            var objectDetailsResponse = await _viewDataClient.GetObjectDetailsAsync(
                objectDetails.BucketKey,
                objectDetails.ObjectKey);

            if (!objectDetailsResponse.IsOk())
            {
                _logger.LogError(objectDetailsResponse.Error.StatusCode.ToString());
                return;
            }

            item.Tag = objectDetailsResponse.Objects[0];

            _logger.LogMessage("Object Details refreshed: " +
                objectDetails.ObjectKey);
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        void SaveNodes()
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "data.json");

            using (FileStream fs = File.Open(path, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    using (JsonWriter jw = new JsonTextWriter(sw))
                    {
                        jw.Formatting = Formatting.Indented;

                        JsonSerializer serializer = new JsonSerializer();

                        sw.Write("[");

                        foreach (TreeItem item in _rootNode.Children)
                        {
                            serializer.Serialize(jw, new BucketDetails(item));
                            sw.Write(",");
                        }

                        sw.Write("]");
                    }
                }

                fs.Close();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        List<BucketDetails> LoadNodes()
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "data.json");

            if (!File.Exists(path))
                return new List<BucketDetails>();

            using (StreamReader sr = File.OpenText(path))
            {
                JsonSerializer serializer = new JsonSerializer();

                List<BucketDetails> buckets = serializer.Deserialize(
                    sr, typeof(List<BucketDetails>)) as List<BucketDetails>;

                return buckets;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        void DeleteBucketItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is BucketDetailsResponse))
                return;

            SelectedItem.Delete();
        }

         /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        void DeleteObjectItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is ObjectDetails))
                return;

            SelectedItem.Delete();
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        class BucketDetails: BucketDetailsResponse
        {
            public List<ObjectDetails> Objects
            {
                get;
                private set;
            }

            public BucketDetails(TreeItem bucketItem)
                : base(
                ((BucketDetailsResponse)bucketItem.Tag).BucketKey,
                ((BucketDetailsResponse)bucketItem.Tag).Owner,
                ((BucketDetailsResponse)bucketItem.Tag).CreateDate,
                ((BucketDetailsResponse)bucketItem.Tag).Permissions,
                ((BucketDetailsResponse)bucketItem.Tag).Policy)
            {
                Objects = new List<ObjectDetails>();

                foreach (TreeItem item in bucketItem.Children)
                {
                    Objects.Add(item.Tag as ObjectDetails);
                }
            }

            public BucketDetails()
            {
                Objects = new List<ObjectDetails>();
            }
        };

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async void ShowThumbnail_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is ObjectDetails))
                return;

            var objectDetails = SelectedItem.Tag as ObjectDetails;

            var thumbnailResponse = await _viewDataClient.GetThumbnailAsync(
                objectDetails.FileId);

            if (!thumbnailResponse.IsOk())
            {
                _logger.LogError("Thumbnail request for " +
                    objectDetails.ObjectKey + " failed: " +
                    thumbnailResponse.Error.StatusCode.ToString());

                return;
            }

            _logger.LogMessage("Thumbnail request for " +
               objectDetails.ObjectKey + " : Success");

            ThumbnailDlg thumbnailDlg = new ThumbnailDlg(
                objectDetails.ObjectKey,
                thumbnailResponse.Image);

            thumbnailDlg.Owner = this.Parent as Window;

            thumbnailDlg.Show();
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        async void ShowViewable_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is ObjectDetails))
                return;

            var objectDetails = SelectedItem.Tag as ObjectDetails;

            var viewableResponse = await _viewDataClient.GetViewableAsync(
                objectDetails.FileId,
                ViewableOptionEnum.kAll);

            if (!viewableResponse.IsOk())
            {
                _logger.LogError("Viewable request for " +
                    objectDetails.ObjectKey + " failed: " +
                    viewableResponse.Error.StatusCode.ToString());

                return;
            }

            _logger.LogMessage("Viewable request for " +
                objectDetails.ObjectKey + " : Success");

            ViewableDlg viewableDlg = new ViewableDlg(
                objectDetails.ObjectKey,
                viewableResponse);

            viewableDlg.Owner = this.Parent as Window;

            viewableDlg.Show();
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        string GenerateViewableHtmlArgs(ObjectDetails objDetails)
        {
            string viewerPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "viewer.html");

            //copy everytime
            //if (!File.Exists(viewerPath))
            {
                using (FileStream fs = File.Open(viewerPath, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(Properties.Resources.viewer);
                    }
                }
            }

            string token = _viewDataClient.TokenResponse.AccessToken;

            string urn = objDetails.FileId.ToBase64();

            return "file:///" + viewerPath +
               "?accessToken=" + HttpUtility.UrlEncode(token) +
               "&urn=" + HttpUtility.UrlEncode(urn);
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        //version running local html file
        //void ViewInBrowser_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!(SelectedItem.Tag is ObjectDetails))
        //        return;

        //    var browserPath = UIHelper.GetDefaultBrowserPath();

        //    var objDetails = SelectedItem.Tag as ObjectDetails;

        //    string args = "\"" + GenerateViewableHtmlArgs(objDetails) + "\"";

        //    System.Diagnostics.Process.Start(browserPath, args);
        //}

        void ViewInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is ObjectDetails))
                return;

            var objDetails = SelectedItem.Tag as ObjectDetails;

            string token = _viewDataClient.TokenResponse.AccessToken;

            string urn = objDetails.FileId.ToBase64();

            string url = string.Format(
                "http://viewer.autodesk.io/node/view-helper?urn={0}&token={1}", 
                HttpUtility.UrlEncode(urn), 
                token);

            System.Diagnostics.Process.Start(url);
        }
       
        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        void ViewInBrowserCustom_Click(object sender, RoutedEventArgs e)
        {
            if (!(SelectedItem.Tag is ObjectDetails))
                return;

            var browserPath = UIHelper.GetDefaultBrowserPath();

            var objectDetails = SelectedItem.Tag as ObjectDetails;

            string [] html = UIHelper.FileSelect(
                "Select html page", "html (*.html)|*.html");

            if (html.Length == 0)
                return;

            string token = _viewDataClient.TokenResponse.AccessToken;

            string urn = objectDetails.FileId.ToBase64();

            string args = "\"file://" + html[0] +
               "?accessToken=" + HttpUtility.UrlEncode(token) +
               "&urn=" + HttpUtility.UrlEncode(urn) + "\"";

            System.Diagnostics.Process.Start(browserPath, args);
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //
        /////////////////////////////////////////////////////////////////////////////////
        public class TreeItem : INotifyPropertyChanged
        {
            TreeItem _parent;

            Bitmap _image;

            public dynamic Tag;

            ObservableCollection<TreeItem> _children =
                new ObservableCollection<TreeItem>();

            public TreeItem(string name, Bitmap image)
            {
                Name = name;

                _image = image;
            }

            public TreeItem(BucketDetailsResponse bucketResponse)
            {
                Name = bucketResponse.BucketKey;

                _image = Properties.Resources.folder_open;

                Tag = bucketResponse;
            }

            public TreeItem(ObjectDetails objectDetails)
            {
                Name = objectDetails.ObjectKey;

                _image = Properties.Resources.file;

                Tag = objectDetails;
            }

            async public Task<ThumbnailResponse> LoadThumbnail()
            {
                if (!(Tag is ObjectDetails) || Thumbnail != null)
                    return null;

                var thumbnailResponse = await _viewDataClient.GetThumbnailAsync(
                    (Tag as ObjectDetails).FileId);

                if (thumbnailResponse.IsOk())
                {
                    Thumbnail = thumbnailResponse.Image;
                }

                return thumbnailResponse;
            }

            public ImageSource Image
            {
                get
                {
                    return BitmapConverter.ToBitmapSource(_image);
                }
            }

            public System.Drawing.Image Thumbnail
            {
                get;
                private set;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(
                        this,
                        new PropertyChangedEventArgs(propertyName));
                }
            }

            private string _name;

            public string Name
            {
                get
                {
                    return "  " + _name;
                }
                set
                {
                    _name = value;

                    OnPropertyChanged("Name");
                }
            }

            public ObservableCollection<TreeItem> Children
            {
                get
                {
                    return _children;
                }
                set
                {
                    _children = value;

                    OnPropertyChanged("Children");
                }
            }

            public TreeItem AddNode(TreeItem node)
            {
                node._parent = this;

                Children.Add(node);

                OnPropertyChanged("Children");

                return node;
            }

            public void RemoveNode(TreeItem node)
            {
                Children.Remove(node);

                OnPropertyChanged("Children");
            }

            public void Delete()
            {
                _parent.RemoveNode(this);
            }
        }
    }
}
