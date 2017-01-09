namespace FoldMyIcons.Models.Generation
{
    using System.Collections;
    using System.Collections.Generic;

    public class IconGenerationExpandedTemplates : IEnumerable<string>
    {
        public IconGenerationExpandedTemplates(List<string> templates, IEnumerable<string> results)
            : this(true, templates, results)
        {
            
        }
        public IconGenerationExpandedTemplates(List<string> templates)
            : this(false, templates, new string[0])
        {

        }
        IconGenerationExpandedTemplates(bool valid, List<string> templates, IEnumerable<string> results)
        {
            Valid = valid;
            Templates = templates;

            Results = results;
        }

        public bool Valid { get;}
        public List<string> Templates { get; }

        public IEnumerable<string> Results { get; set; }

        public static implicit operator bool(IconGenerationExpandedTemplates value) => value.Valid;

        #region Implementation of IEnumerable
        public IEnumerator<string> GetEnumerator() 
            => Results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => ((IEnumerable) Results).GetEnumerator();
        #endregion
    }
}