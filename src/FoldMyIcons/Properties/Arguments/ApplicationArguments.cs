namespace FoldMyIcons.Properties.Arguments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Fclp;
    using Folders;
    using Puchalapalli.Extensions.Collections;
    using Puchalapalli.Extensions.Primitives;
    using DirectoryIO = Alphaleonis.Win32.Filesystem.Directory;

    public class ApplicationArguments
    {
        private List<string> _directories;
        private IconRootFolders _roots;
        private string _icon;

        public static ApplicationArguments Options { get; set; }

        public static class CommandLine
        {
            public static string[] Arguments { get; set; }
            public static ICommandLineParserResult Result { get; set; }
            public static void Parse()
            {
                var p = new FluentCommandLineParser<ApplicationArguments>();
                p.Setup(a => a.Directories)
                    .As('d', "directory")
                    .WithDescription("Active Directories for any start-up command. Also, if specified, the first active directory will be selected in the directory listing on application start. Otherwise, the selected directory from the last time the application ran will be selected");
                p.Setup(a => a.Icon)
                    .As('i', "icon")
                    .WithDescription("Active Icon for any start-up command. Also, if specified, the active icon will be selected in the icon listing on application start. Otherwise, the selected icon from the last time the application ran will be selected");
                p.Setup(a => a.Roots.Icons)
                    .As('o', "icons")
                    .WithDescription("Icons Root Directory");
                p.Setup(a => a.Roots.Action)
                    .As('r', "root")
                    .WithDescription("Root Directory");
                p.Setup(a => a.Roots.Content)
                    .As('c', "content")
                    .WithDescription("Content Root Directory");
                Result = p.Parse(Arguments);
                Options = p.Object;
            }
        }
        

        public List<string> Directories
        {
            get
            {
                return _directories ?? (_directories = CommandLine.Arguments.Where(DirectoryIO.Exists).ToList());
            }
            set
            {
                _directories = value;
            }
        }

        public IconRootFolders Roots
        {
            get { return _roots ?? (_roots = new IconRootFolders()); }
            set { _roots = value; }
        }

        public string Icon
        {
            get { return _icon?.IfNotEmpty(); }
            set { _icon = value; }
        }

        public string Icons { get; set; }
        public string Content { get; set; }

        public string Directory 
            => Directories.NotEmpty().FirstOrDefault();


        static ApplicationArguments()
        {
            var args = Environment.GetCommandLineArgs();
            CommandLine.Arguments = args.ToList().GetRange(1, args.Length - 1).ToArray();
            CommandLine.Parse();
        }
    }
}