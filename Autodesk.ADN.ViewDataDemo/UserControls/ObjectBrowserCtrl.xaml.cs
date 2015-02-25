using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
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
using Autodesk.ADN.Toolkit.ViewData.DataContracts;

namespace Autodesk.ADN.ViewDataDemo
{
    /// <summary>
    /// Interaction logic for ObjectBrowserCtrl.xaml
    /// </summary>
    public partial class ObjectBrowserCtrl : UserControl
    {
        private object _object;

        public ObjectBrowserCtrl()
        {
            InitializeComponent();
        }

        public object Object
        {
            set
            {
                _object = value;

                tvObjectGraph.DataContext = 
                    new ObjectViewModelHierarchy(_object);
            }
            get
            {
                return _object;
            }
        }

        private ObjectViewModel SelectedItem
        {
            get
            {
                return tvObjectGraph.SelectedItem 
                    as ObjectViewModel;
            }
        }

        private void OnTreeViewSelectedItemChanged(
           object sender,
           RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedItem != null)
            {
                _propertyGrid.SelectedObject = SelectedItem.Object;
            }
        }

        dynamic ToExpandoObject(object obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj.GetType()))
                expando.Add(property.Name, property.GetValue(obj));

            return expando as ExpandoObject;
        }
    }

    public class ObjectViewModel : INotifyPropertyChanged
    {
        ReadOnlyCollection<ObjectViewModel> _children;
        ObjectViewModel _parent;
        PropertyInfo _info;
        object _object;      
        Type _type;

        bool _isExpanded;
        bool _isSelected;

        public ObjectViewModel(object obj)
            : this(obj, null, null)
        {
        }

        public object Object
        {
            get
            {
                return _object;
            }
        }

        ObjectViewModel(object obj, PropertyInfo info, ObjectViewModel parent)
        {
            _object = obj;
            _info = info;

            if (_object != null)
            {
                _type = obj.GetType();

                if (!IsPrintableType(_type))
                {
                    // load the _children object with an empty collection 
                    // to allow the + expander to be shown
                    _children = new ReadOnlyCollection<ObjectViewModel>(
                        new ObjectViewModel[] 
                        { 
                            new ObjectViewModel(null) 
                        });
                }
            }

            _parent = parent;
        }

        public void LoadChildren()
        {
            if (_object != null)
            {
                // exclude value types and strings from listing child members
                if (!IsPrintableType(_type))
                {
                    // the public properties of this object are its children
                    // exclude indexed parameters for now
                    var children = _type.GetProperties()
                        .Where(p => (!p.GetIndexParameters().Any() && IsBrowsable(p))) 
                        .Select(p => new ObjectViewModel(p.GetValue(_object, null), p, this))
                        .ToList();

                    // if this is a collection type, add the contained items to the children
                    var collection = _object as IEnumerable;
                    
                    if (collection != null)
                    {
                        foreach (var item in collection)
                        {
                            // todo: add something to view the index value
                            children.Add(new ObjectViewModel(item, null, this)); 
                        }
                    }

                    _children = new ReadOnlyCollection<ObjectViewModel>(children);

                    this.OnPropertyChanged("Children");
                }
            }
        }

        bool IsBrowsable(PropertyInfo pi)
        {
            BrowsableAttribute ba = Attribute.GetCustomAttribute(
                pi, 
                typeof (BrowsableAttribute)) as BrowsableAttribute;

            return (ba != null ? (pi.PropertyType.Name == "List`1" ? true : ba.Browsable) : true);
        }

        /// <summary>
        /// Gets a value indicating if the object graph 
        /// can display this type without enumerating its children
        /// </summary>
        static bool IsPrintableType(Type type)
        {
            return type != null && (
                type.IsPrimitive ||
                type.IsAssignableFrom(typeof(string)) ||
                type.IsEnum);
        }

        public ObjectViewModel Parent
        {
            get { return _parent; }
        }

        public PropertyInfo Info
        {
            get { return _info; }
        }

        public ReadOnlyCollection<ObjectViewModel> Children
        {
            get { return _children; }
        }

        public string Name
        {
            get
            {
                var name = string.Empty;

                if (_info != null)
                {
                    name = _info.Name;
                }

                return name;
            }
        }

        public string Type
        {
            get
            {
                var type = string.Empty;

                if (_object != null)
                {
                    type = string.Format("({0})", _type.Name);
                }
                else
                {
                    if (_info != null)
                    {
                        type = string.Format("({0})", _info.PropertyType.Name);
                    }
                }

                return type.Replace("`1", "");
            }
        }

        public string Value
        {
            get
            {
                var value = string.Empty;

                if (_object != null)
                {
                    if (IsPrintableType(_type))
                    {
                        value = _object.ToString();
                    }
                }
                else
                {
                    value = "<null>";
                }

                return value;
            }
        }

        #region Presentation Members

        public bool IsExpanded
        {
            get 
            { 
                return _isExpanded; 
            }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;

                    if (_isExpanded)
                    {
                        LoadChildren();
                    }

                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                {
                    _parent.IsExpanded = true;
                }
            }
        }

        public bool IsSelected
        {
            get 
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        public bool NameContains(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(Name))
            {
                return false;
            }

            return Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        public bool ValueContains(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(Value))
            {
                return false;
            }

            return Value.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }

    public class ObjectViewModelHierarchy
    {
        readonly ReadOnlyCollection<ObjectViewModel> _firstGeneration;

        readonly ObjectViewModel _rootObject;

        public ObjectViewModelHierarchy(object rootObject)
        {
            _rootObject = new ObjectViewModel(rootObject);

            _firstGeneration = new ReadOnlyCollection<ObjectViewModel>(
                new ObjectViewModel[] 
                { 
                    _rootObject
                });
        }

        public ReadOnlyCollection<ObjectViewModel> FirstGeneration
        {
            get { return _firstGeneration; }
        }
    }
}
