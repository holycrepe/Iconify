namespace FoldMyIcons.Folders
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using Alphaleonis.Win32.Filesystem;
    using JetBrains.Annotations;
    using Puchalapalli.Extensions.Collections;
    using Puchalapalli.Helpers.Utils;
    using Puchalapalli.IO;
    using Puchalapalli.Utilities.Primitives.Strings.Regex;

    public class IconRoots<T> : INotifyPropertyChanged
    {

        private T _action;
        private T _icons;
        private T _content;
        private T _library;
        private ObservableCollection<string> _labels;
        private Regex[] _rootLabelReplacementRegex;

        private const string ROOT_LABEL_REGEX_TEMPLATE
            = @"(?x)^
		            (?<Root>[^\\]+)
		            (?<Parent>(?:\\[^\\]+)*?)
		            (?:\\(?!\k<Root>\\|$)(?<Search>{{{RootLabel}}})(?=\\|$))
		            (?<SubPath>(?:\\[^\\]+)*?)
		            (?<Name>(?:\\[^\\]+))?
                    $";


        public virtual ObservableCollection<string> Labels
        {
            get { return _labels ?? (_labels = new ObservableCollection<string>()); }
            set
            {
                _labels = value; 
                OnPropertyChanged();
            }
        }

        public T Action
        {
            get { return Get(IconRootType.Action); }
            set { Set(IconRootType.Action, value); }
        }

        public T Icons
        {
            get { return Get(IconRootType.Icons); }
            set { Set(IconRootType.Icons, value); }
        }

        public T Library
        {
            get { return Get(IconRootType.Library); }
            set { Set(IconRootType.Library, value); }
        }

        public T Content
        {
            get { return Get(IconRootType.Content); }
            set { Set(IconRootType.Content, value); }
        }

        protected Regex[] RootLabelReplacementRegex
        {
            get { return _rootLabelReplacementRegex ?? (_rootLabelReplacementRegex = GetAlternateRootLabelRegex()); }
            set { _rootLabelReplacementRegex = value; }
        }

        public void RefreshLabelsRegex()
        {
            RootLabelReplacementRegex = null;
        }
        
        public virtual T Get(IconRootType type)
        {
            switch (type)
            {
                case IconRootType.Icons:
                    return _icons;
                case IconRootType.Library:
                    return _library;
                case IconRootType.Content:
                    return _content;
                case IconRootType.Action:
                    return _action;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        public void Set(IconRootType type, T value)
        {
            switch (type)
            {
                case IconRootType.Icons:
                    _icons = value;
                    break;
                case IconRootType.Library:
                    _library = value;
                    break;
                case IconRootType.Content:
                    _content = value;
                    break;
                case IconRootType.Action:
                    _action = value;
                    break;
                case IconRootType.Labels:
                    var info = value as DirectoryInfo;
                    if (info != null)
                    {
                        Labels.Add(info?.Name);
                        RefreshLabelsRegex();
                        break;
                    }
                    var str = value?.ToString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {                        
                        var label = Paths.GetFileName(str);
                        if (Labels.Contains(label))
                            return;
                        Labels.Add(label);
                        var sortedLabels = Labels.OrderBy(x => x).ToArray();
                        Labels.Clear();
                        Labels.AddRange(sortedLabels);
                        RefreshLabelsRegex();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            OnPropertyChanged(type.ToString());
        }

        Regex GetAlternateRootLabelRegexObject(string roots)
            => new Regex(ROOT_LABEL_REGEX_TEMPLATE.Replace("{{{RootLabel}}}", roots), RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        Regex GetAlternateRootLabelRegexGroup()
        {
            var roots = RegexGenerator.CreateGroup(Labels.Select(Regex.Escape));
            return GetAlternateRootLabelRegexObject(roots);

        }

        Regex[] GetAlternateRootLabelRegex()
            => Labels
                .Select(Regex.Escape)
                .Select(GetAlternateRootLabelRegexObject)
                .ToArray();

        private readonly string[] AlternateRootLabelsRegexReplacements =
        {
            @"${Root}\${Search}${SubPath}${Name}",
            @"${Search}${SubPath}${Name}",
            @"${Search}${SubPath}\${Root}${Name}"
        };
        public IEnumerable<string> GetAlternateRootLabels(string label)
        {
            foreach (var regex in RootLabelReplacementRegex)
            {
                var match = regex.Match(label);
                if (!match.Success)
                    continue;
                foreach (var replacement in AlternateRootLabelsRegexReplacements)
                {
                    var result = match.Result(replacement);
                    if (label != result)
                        yield return result;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}