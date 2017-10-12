﻿using AvalonStudio.Projects;
using AvalonStudio.Shell;
using AvalonStudio.Utils;
using Microsoft.DotNet.Cli.Sln.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace AvalonStudio.Extensibility.Projects
{
    public class VisualStudioSolution : ISolution
    {
        private SlnFile _solutionModel;
        private Dictionary<Guid, ISolutionItem> _solutionItems;

        public static VisualStudioSolution Load(string fileName)
        {
            return new VisualStudioSolution(SlnFile.Read(fileName));
        }

        private VisualStudioSolution(SlnFile solutionModel)
        {
            var shell = IoC.Get<IShell>();
            _solutionModel = solutionModel;
            _solutionItems = new Dictionary<Guid, ISolutionItem>();

            Parent = Solution = this;

            Id = Guid.NewGuid();

            Items = new ObservableCollection<ISolutionItem>();

            LoadFolders();

            LoadProjects();

            BuildTree();

            this.PrintTree();
        }

        private void BuildTree()
        {
            var nestedProjects = _solutionModel.Sections.FirstOrDefault(section => section.Id == "NestedProjects");

            if (nestedProjects != null)
            {
                foreach (var nestedProject in nestedProjects.Properties)
                {
                    _solutionItems[Guid.Parse(nestedProject.Key)].SetParent(_solutionItems[Guid.Parse(nestedProject.Value)] as ISolutionFolder);
                }
            }
        }

        private void LoadFolders()
        {
            var solutionFolders = _solutionModel.Projects.Where(p => p.TypeGuid == ProjectTypeGuids.SolutionFolderGuid);

            foreach (var solutionFolder in solutionFolders)
            {
                var newItem = new SolutionFolder(solutionFolder.Id, solutionFolder.Name, this);
                _solutionItems.Add(newItem.Id, newItem);
                Items.InsertSorted(newItem);
            }
        }

        private void LoadProjects()
        {
            var solutionProjects = _solutionModel.Projects.Where(p => p.TypeGuid != ProjectTypeGuids.SolutionFolderGuid);

            foreach (var project in solutionProjects)
            {
                var projectType = project.GetProjectType();

                IProject newProject = null;

                if (projectType != null)
                {
                    newProject = projectType.Load(this, project.FilePath);
                }
                else
                {
                    newProject = new UnsupportedProjectType(this, project.FilePath);
                }

                newProject.Id = Guid.Parse(project.Id);
                newProject.Solution = this;

                _solutionItems.Add(newProject.Id, newProject);
                Items.InsertSorted(newProject);
            }
        }

        public string Name => Path.GetFileNameWithoutExtension(_solutionModel.FullPath);

        public string Location => _solutionModel.FullPath;

        public IProject StartupProject { get; set; }

        public ObservableCollection<IProject> Projects { get; set; }

        public string CurrentDirectory => Path.GetDirectoryName(_solutionModel.FullPath);

        public ObservableCollection<ISolutionItem> Items { get; private set; }

        public ISolution Solution { get; set; }

        public ISolutionFolder Parent { get; set; }

        public Guid Id { get; set; }

        public IProject AddProject(IProject project)
        {
            throw new NotImplementedException();
        }

        public ISourceFile FindFile(string path)
        {
            throw new NotImplementedException();
        }

        public void RemoveProject(IProject project)
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            _solutionModel.Write();
        }

        public void SetItemParent(ISolutionItem item, ISolutionFolder parent)
        {
            var nestedProjects = _solutionModel.Sections.FirstOrDefault(section => section.Id == "NestedProjects");

            nestedProjects.Properties.Remove(item.Id.ToString());

            item.SetParent(parent);

            if (parent != this)
            {
                nestedProjects.Properties[item.Id.ToString()] = parent.Id.ToString();
            }

            _solutionModel.Write();
        }

        public int CompareTo(ISolutionItem other)
        {
            throw new NotImplementedException();
        }
    }
}
