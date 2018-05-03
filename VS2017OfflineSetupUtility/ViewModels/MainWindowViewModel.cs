﻿/*
    Copyright © 2017 Deepak Rathi 
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace VS2017OfflineCleanupUtil.ViewModels
{
    class MainWindowViewModel:BindableBase
    {

        #region DirectoryNames
        /// <summary>
        /// Contain all directory names for selected folder. Left for future purpose; binding to UI list
        /// </summary>
        private ObservableCollection<VSModule> _moduleCollection = new ObservableCollection<VSModule>();

        public ObservableCollection<VSModule> ModuleCollection
        {
            get { return _moduleCollection; }
            set { SetProperty(ref _moduleCollection, value); }
        }
        #endregion

        #region OldVersionModule
        private ObservableCollection<VSModule> _oldVersionModule = new ObservableCollection<VSModule>();
        /// <summary>
        /// Contain all directory names for selected folder
        /// </summary>
        public ObservableCollection<VSModule> OldVersionModule
        {
            get { return _oldVersionModule; }
            set
            {
                if (SetProperty(ref _oldVersionModule, value))
                    DeleteOldVersionCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region SelectedFolderPath
        private string _selectedFolderPath = default(string);
        /// <summary>
        /// Contain SelectedFolderPath string
        /// </summary>
        public string SelectedFolderPath
        {
            get { return _selectedFolderPath; }
            set {
                if (SetProperty(ref _selectedFolderPath, value))
                {
                    DeleteOldVersionCommand.RaiseCanExecuteChanged();
                }
            }
        }
        #endregion

        #region SelectFolderCommand
        private DelegateCommand _selectFolderCommand;

        public DelegateCommand SelectFolderCommand
        {
            get
            {
                return _selectFolderCommand ?? (_selectFolderCommand = new DelegateCommand(() =>
                {
                    try
                    {

                        CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                        dialog.InitialDirectory = Path.GetDirectoryName(Directory.GetCurrentDirectory()); //Select the parent dir of execution dir
                        dialog.IsFolderPicker = true;
                        dialog.AddToMostRecentlyUsedList = false;
                        dialog.Title = "Select VS2017 offline setup folder";

                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            SelectedFolderPath = dialog.FileName;
                            DirectoryInfo dirInfo = new DirectoryInfo(SelectedFolderPath);

                            //classification
                            var directories = dirInfo.GetDirectories();
                            foreach (var directory in directories)
                            {
                                var vsModule = new VSModule();
                                if (directory.Name.Contains(","))
                                {
                                    var stringSplit = directory.Name.Split(',').ToList();
                                    vsModule.Name = stringSplit.FirstOrDefault();
                                    vsModule.Version = stringSplit[1];
                                    stringSplit.Remove(vsModule.Name);
                                    stringSplit.Remove(vsModule.Version);
                                    if (stringSplit.Count() > 0)
                                    {
                                        foreach (var item in stringSplit)
                                            vsModule.Name = vsModule.Name + "," + item;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                                vsModule.FullPath = directory.FullName;
                                ModuleCollection.Add(vsModule);
                            }

                            //Select all the Modules with same name from ModuleCollection
                            var duplicateModules =
                            ModuleCollection.Where(module =>
                            ModuleCollection
                            .Except(new ObservableCollection<VSModule> { module })
                            .Any(x => x.Name == module.Name)
                            ).ToObservableCollection();

                            //Get all the old version modules/folder from duplicateModules
                            OldVersionModule =
                            duplicateModules.Where(module =>
                            duplicateModules
                            .Except(new ObservableCollection<VSModule> { module })
                            .Any(x => x.Name == module.Name && x.VersionObject.CompareTo(module.VersionObject) > 0)
                            ).ToObservableCollection();

                            if (!OldVersionModule.Any())
                                MessageBox.Show("Old version folder does not exist.");
                        }
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                    }
                }));
            }
        }
        #endregion

        #region DeleteOldVersionCommand
        private DelegateCommand _deleteOldVersionCommand;

        public DelegateCommand DeleteOldVersionCommand
        {
            get {
                return _deleteOldVersionCommand ?? (_deleteOldVersionCommand = new DelegateCommand(() =>
                {
                    try
                    {
                        //Delete old version folder and files
                        foreach (var folder in OldVersionModule)
                        {
                            Directory.Delete(folder.FullPath, true);
                        }
                        OldVersionModule.Clear();
                        MessageBox.Show("Operation successful.");
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                    }
                }, ()=>!string.IsNullOrWhiteSpace(SelectedFolderPath) && OldVersionModule?.Count>0));
            }
        }

        #endregion
    }

    public class VSModule
    {
        #region Name
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        #endregion

        #region Version
        private string _version;

        public string Version
        {
            get { return _version; }
            set { _version = value;
                VersionObject = new System.Version(value.Split('=')[1]);
            }
        }
        #endregion

        public string FullPath;
        public System.Version VersionObject { get; private set; }
    }
}
