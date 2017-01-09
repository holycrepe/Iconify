namespace FoldMyIcons.Models.Generation
{using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using JetBrains.Annotations;
    using Puchalapalli.IO;

    public class IconGenerationTemplates : List<string>
    {
        private List<string> _names;

        /// <summary>
        /// <para>
        /// <c>Names</c> are individual format specifications whose values are validated. 
        /// </para>
        /// Specifically, all <see cref="Names"/> should produce non-rooted paths 
        /// </summary>
        public List<string> Names
        {
            get { return _names; }
            set
            {
                if (value != null)
                    _names = value;
            }
        }

        public List<string> Templates
        {
            get { return this; }
            set
            {
                if (value != null)
                {
                    this.Clear();
                    this.AddRange(value);
                }
            }
        }
        /// <summary>
        /// <para>
        /// Creates new <see cref="IconGenerationTemplates"/> with explicitly provided <see cref="Names"/>
        /// </para>
        /// <para>
        /// <c>Names</c> are individual format specifications whose values are validated. 
        /// </para>
        /// Specifically, all <see cref="Names"/> should produce non-rooted paths 
        /// </summary>
        /// <param name="names">
        /// <para>
        /// <c>Names</c> are individual format specifications whose values are validated. 
        /// </para>
        /// Specifically, all <see cref="Names"/> should produce non-rooted paths 
        /// </param>
        public IconGenerationTemplates(params string[] names)
        {
            _names = new List<string>(names);
        }

        bool ValidateName(string expandedName) => expandedName.Length == 1 || (expandedName.Length > 1 && expandedName[1] != ':');
        bool ValidateArguments(object[] arguments) 
            => Names.Select(formatSpec => string.Format(formatSpec, arguments)).All(ValidateName);

        public IconGenerationExpandedTemplates Generate(params object[] arguments)
        {
            if (!ValidateArguments(arguments))
                return new IconGenerationExpandedTemplates(Templates);
            var results = Templates.Select(template => string.Format(template, arguments));
            return new IconGenerationExpandedTemplates(Templates, results);
        }

        [StringFormatMethod("template")]
        public new void Add([NotNull] string template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            Templates.Add(template);
        }

        [StringFormatMethod("templates")]
        public void Add(params string[] templates) => Templates.AddRange(templates);
    }
}