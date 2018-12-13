/* 
Licensed under the Apache License, Version 2.0

http://www.apache.org/licenses/LICENSE-2.0
*/
using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO;

namespace ResUtility
{
    [XmlRoot(ElementName = "LocaleResource")]
    public class LocaleResource : INotifyPropertyChanged
    {
        private static Regex regex = new Regex(@"\p{IsArabic}|\p{IsHebrew}");
        public FlowDirection FlowDirection
        {
            get
            {
                return regex.IsMatch(Value) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }
        }

        public void ResetUndo()
        {
            UndoList.Clear();
            RedoList.Clear();
        }
        public bool CanUndo => UndoList.Any();
        public bool CanRedo => RedoList.Any();

        public void Undo()
        {
            if (CanUndo == false)
                return;
            RedoList.Push(_value);
            _value = UndoList.Pop();
            OnPropertyChanged("Value");
            OnPropertyChanged("CanUndo");
            OnPropertyChanged("CanRedo");
        }

        public void Redo()
        {
            if (CanRedo == false)
                return;
            UndoList.Push(_value);
            _value = RedoList.Pop();
            OnPropertyChanged("Value");
            OnPropertyChanged("CanUndo");
            OnPropertyChanged("CanRedo");
        }

        private readonly Stack<string> UndoList = new Stack<string>();
        private readonly Stack<string> RedoList = new Stack<string>();

        private List<string> _nameChain;
        private string _name;
        private string _value;

        [XmlElement(ElementName = "Value")]
        public string Value
        {
            get => _value; set
            {
                UndoList.Push(_value);
                _value = value;
                OnPropertyChanged("Value");
                OnPropertyChanged("CanUndo");
                OnPropertyChanged("CanRedo");
            }
        }
        [XmlAttribute(AttributeName = "Name")]
        public string Name
        {
            get => _name; set
            {
                _name = value;
                _nameChain = new List<string>();
                if (string.IsNullOrEmpty(_name) == false)
                {
                    _nameChain.AddRange(_name.Split('.'));
                }
                OnPropertyChanged("Name");
            }
        }
        [XmlIgnore]
        public List<string> NameChain => _nameChain;
        [XmlIgnore]
        public int Index { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    [XmlRoot(ElementName = "Language")]
    public class Resource : INotifyPropertyChanged
    {
        [XmlIgnore]
        public Guid Guid { get; set; }
        public Resource()
        {
            Guid = Guid.NewGuid();
            LocaleResources = new ObservableCollection<LocaleResource>();
        }
        #region Fields
        private ObservableCollection<LocaleResource> _localeResources;
        private string _name;
        private ObservableCollection<ResourceItem> _items;
        #endregion
        #region Properties
        [XmlElement(ElementName = "LocaleResource")]
        public ObservableCollection<LocaleResource> LocaleResources
        {
            get => _localeResources; set
            {
                _localeResources = value;
                OnPropertyChanged("LocaleResources");
                OnPropertyChanged("Items");
            }
        }
        [XmlAttribute(AttributeName = "Name")]
        public string Name
        {
            get => _name; set
            {
                _name = value;
                OnPropertyChanged("LocaleResources");
            }
        }
        [XmlIgnore]
        public ObservableCollection<ResourceItem> Items
        {
            get
            {
                Update();
                return _items;
            }
        }
        private FileInfo _fileInfo;
        [XmlIgnore]
        public FileInfo FileInfo
        {
            get => _fileInfo; set
            {
                _fileInfo = value;
                OnPropertyChanged("FileInfo");
            }
        }



        #endregion
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
        #region Methods

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        private void GetChilds(ObservableCollection<ResourceItem> items, IEnumerable<LocaleResource> localeResources, int l = 0, ResourceItem parent = null)
        {
            foreach (var localRecource in localeResources)
            {
                if (localRecource.NameChain.Count <= l)
                    continue;
                if (items.Any(x => x.Name == localRecource.NameChain[l]) == false)
                {
                    var item = new ResourceItem()
                    {
                        Name = localRecource.NameChain[l],
                        Items = new ObservableCollection<ResourceItem>(),
                        Parent = parent
                    };
                    GetChilds(item.Items, localeResources.Where(x => x.NameChain.Count > l && x.NameChain[l] == item.Name).ToList(), l + 1, item);
                    items.Add(item);
                }
            }
        }

        public void Update()
        {
            _items = new ObservableCollection<ResourceItem>();
            _items.Add(new ResourceItem()
            {
                Name = ResourceItem.Root,
                Parent = null,
                Items = new ObservableCollection<ResourceItem>()
            });
            GetChilds(_items[0].Items, LocaleResources, 0, _items[0]);
            OnPropertyChanged("Itmes");
        }
        #endregion

    }

    public class ResourceItem
    {
        public const string Root = "Root";
        public ObservableCollection<ResourceItem> Items { get; set; }
        public string Name { get; set; }
        public ResourceItem Parent { get; set; }
        public Visibility HasChildren => Items.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FullPath => Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public string Path
        {
            get
            {
                if (Parent == null)
                    return "";
                if (Parent.Parent == null)
                {
                    return Name;
                }
                return Parent.Path + '.' + Name;
            }
        }
    }
}

