﻿// (C) Copyright 2018 by Andrew Nicholas
//
// This file is part of SCaddins.
//
// SCaddins is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SCaddins is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with SCaddins.  If not, see <http://www.gnu.org/licenses/>.

namespace SCaddins.ExportManager.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Data;
    using System.Windows.Input;
    using Caliburn.Micro;

    internal class SCexportViewModel : Screen
    {
        private readonly ExportManager exportManager;
        private double currentProgress;
        private ExportLog log;
        private List<string> printTypes;
        private string searchText;
        private string selectedPrintType;
        private List<ExportSheet> selectedSheets = new List<ExportSheet>();
        private ObservableCollection<ExportSheet> sheets;
        private ICollectionView sheetsCollection;

        public SCexportViewModel(ExportManager exportManager)
        {
            printTypes = (new string[] { "Print A3", "Print A2", "Print Full Size" }).ToList();
            selectedPrintType = "Print A3";
            this.exportManager = exportManager;
            this.sheets = new ObservableCollection<ExportSheet>(exportManager.AllSheets);
            Sheets = CollectionViewSource.GetDefaultView(this.sheets);
            Sheets.SortDescriptions.Add(new SortDescription("FullExportName", ListSortDirection.Ascending));
            CurrentProgress = 0;
            ProgressBarMaximum = 1;
            log = new ExportLog();
        }

        public static dynamic DefaultWindowSettings
        {
            get
            {
                dynamic settings = new ExpandoObject();
                settings.Height = 480;
                settings.Icon = new System.Windows.Media.Imaging.BitmapImage(
                    new System.Uri("pack://application:,,,/SCaddins;component/Assets/scexport.png"));
                settings.Width = 768;
                settings.Title = "SCexport - By Andrew Nicholas";
                settings.ShowInTaskbar = false;
                settings.SizeToContent = System.Windows.SizeToContent.Manual;
                return settings;
            }
        }

        public double CurrentProgress
        {
            get
            {
                return currentProgress;
            }

            set
            {
                currentProgress = value;
                NotifyOfPropertyChange(() => CurrentProgress);
                NotifyOfPropertyChange(() => ProgressBarText);
            }
        }

        public bool IsSearchTextFocused
        {
            get; set; 
        }

        public BindableCollection<string> PrintTypes
        {
            get
            {
                return new BindableCollection<string>(printTypes);
            }
        }

        public double ProgressBarMaximum
        {
            get;
            set;
        }

        public string ProgressBarText
        {
            get
            {
                if (CurrentProgress == 0) {
                    var numberOfSheets = selectedSheets.Count;
                    return numberOfSheets + @" Sheet[s] Selected To Export/Print";
                } else {
                    string percentageCompete = ((CurrentProgress / selectedSheets.Count) * 100).ToString();
                    return "Exporting " + CurrentProgress + @"/" + selectedSheets.Count + @"("+ percentageCompete  + @"%) [Elapsed Time = " +  log.TimeSinceStart + @"]";
                }
            }
        }

        public string SearchText
        {
            get
            {
                return searchText;
            }

            set
            {
                if (value != searchText) {
                    searchText = value;
                }            
            }
        }

        public string SelectedPrintType
        {
            get
            {
                return selectedPrintType;
            }

            set
            {
                if (value != selectedPrintType)
                {
                    selectedPrintType = value;
                    NotifyOfPropertyChange(() => SelectedPrintType);
                }
            }
        }

        public ExportSheet SelectedSheet
        {
            get; set;
        }

        public ICollectionView Sheets
        {
            get
            {
                return this.sheetsCollection;
            }

            set
            {
                this.sheetsCollection = value;
                NotifyOfPropertyChange(() => Sheets);
            }
        }

        public ObservableCollection<ViewSheetSetCombo> ViewSheetSets
        {
            get { return exportManager.AllViewSheetSets; }
        }

        public void AddRevision()
        {
            var revisionSelectionViewModel = new RevisionSelectionViewModel(exportManager.Doc);
            bool? result = SCaddinsApp.WindowManager.ShowDialog(revisionSelectionViewModel, null, RevisionSelectionViewModel.DefaultWindowSettings);
            bool newBool = result.HasValue ? result.Value : false;
            if (newBool)
            {
                if (revisionSelectionViewModel.SelectedRevision != null)
                {
                    ExportManager.AddRevisions(selectedSheets, revisionSelectionViewModel.SelectedRevision.Id, exportManager.Doc);
                    NotifyOfPropertyChange(() => Sheets);
                }
            }
        }

        public void CopySheets()
        {
            var sheetCopierModel = new SCaddins.SheetCopier.ViewModels.SheetCopierViewModel(exportManager.UIDoc);
            sheetCopierModel.AddSheets(selectedSheets);
            this.IsNotifying = false;
            SCaddinsApp.WindowManager.ShowDialog(
                sheetCopierModel,
                null,
                SheetCopier.ViewModels.SheetCopierViewModel.DefaultWindowSettings);
            this.IsNotifying = true;
        }

        public void CreateUserViews()
        {
            ViewUtilities.UserView.ShowSummaryDialog(
                ViewUtilities.UserView.Create(selectedSheets, exportManager.Doc));
        }

        public void ExecuteFilterView(KeyEventArgs keyArgs)
        {
            if (keyArgs.OriginalSource.GetType() == typeof(System.Windows.Controls.TextBox)) {
                if (keyArgs.Key == Key.Enter) {
                    var filter = new System.Predicate<object>(
                    item =>
                       item != null
                       &&
                       -1 < ((ExportSheet)item).SheetDescription.IndexOf(SearchText, System.StringComparison.OrdinalIgnoreCase)
                       ||
                       -1 < ((ExportSheet)item).SheetNumber.IndexOf(SearchText, System.StringComparison.OrdinalIgnoreCase));
                    Sheets.Filter = filter;
                    NotifyOfPropertyChange(() => Sheets);
                }
                return;
            }

            switch (keyArgs.Key)
            {
                case Key.C:
                    RemoveViewFilter();
                    break;

                case Key.L:
                    ShowLatestRevision();
                    break;

                default:
                    break;
            }

            if (keyArgs.Key == Key.O)
            {
                OpenViewsCommand();
            }

            if (keyArgs.Key == Key.S)
            {
                var activeSheetNumber = ExportManager.CurrentViewNumber(exportManager.Doc);
                if (activeSheetNumber == null)
                {
                    return;
                }
                ExportSheet ss = sheets.Where<ExportSheet>(item => (item).SheetNumber.Equals(activeSheetNumber)).First<ExportSheet>();
                SelectedSheet = ss;
                NotifyOfPropertyChange(() => SelectedSheet);
            }

            if (keyArgs.Key == Key.Escape)
            {
                TryClose();
            }

            if (keyArgs.Key == Key.D1)
            {
                FilterByNumber("1");
            }

            if (keyArgs.Key == Key.D2)
            {
                FilterByNumber("2");
            }
        }

        public void Export()
        {
            log.Clear();
            log.Start("Beginning Export.");
            ProgressBarMaximum = selectedSheets.Count;
            NotifyOfPropertyChange(() => ProgressBarMaximum);
            System.Windows.Forms.Application.DoEvents();
            foreach (ExportSheet sheet in selectedSheets)
            {
                CurrentProgress += 1;
                System.Windows.Forms.Application.DoEvents();
                exportManager.ExportSheet(sheet, log);
                System.Windows.Forms.Application.DoEvents();
            }
            CurrentProgress = 0;
            log.Stop("Finished Export.");
            TryShowExportLog();
        }

        public void FixScaleBars()
        {
            ExportManager.FixScaleBars(selectedSheets, exportManager.Doc);
        }

        public void OpenViewsCommand()
        {
            OpenSheet.OpenViews(selectedSheets);
        }

        public void OptionsButton()
        {
            dynamic settings = new ExpandoObject();
            settings.Height = 640;
            settings.Width = 480;
            settings.Title = "SCexport - Options";
            settings.ShowInTaskbar = false;
            settings.ResizeMode = System.Windows.ResizeMode.NoResize;
            settings.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            var optionsModel = new OptionsViewModel(exportManager, this);
            SCaddinsApp.WindowManager.ShowDialog(optionsModel, null, settings);
        }

        public void Print(string printerName, int printMode)
        {
            log.Clear();
            log.Start("Starting print...");
            ProgressBarMaximum = selectedSheets.Count;
            NotifyOfPropertyChange(() => ProgressBarMaximum);
            System.Windows.Forms.Application.DoEvents();
            foreach (ExportSheet sheet in selectedSheets.OrderBy(x => x.SheetNumber).ToList())
            {
                CurrentProgress += 1;
                System.Windows.Forms.Application.DoEvents();
                exportManager.Print(sheet, printerName, printMode, log);
                System.Windows.Forms.Application.DoEvents();
            }
            CurrentProgress = 0;
            log.Stop("Finished Print.");
            TryShowExportLog();
        }

        public void PrintA2()
        {
            Print(exportManager.PrinterNameLargeFormat, 2);
        }

        public void PrintA3()
        {
            Print(exportManager.PrinterNameA3, 3);
        }

        public void PrintButton()
        {
            if (selectedPrintType == "Print A3")
            {
                PrintA3();
            }
            if (selectedPrintType == "Print A2")
            {
                PrintA2();
            }
            if (selectedPrintType == "Print Full Size")
            {
                PrintFullsize();
            }
        }

        public void PrintFullsize()
        {
            Print(exportManager.PrinterNameLargeFormat, 1);
        }

        public void RemoveUnderlays()
        {
            ViewUtilities.ViewUnderlays.RemoveUnderlays(selectedSheets, exportManager.Doc);
        }

        public void RemoveViewFilter()
        {
            Sheets.Filter = null;
            SearchText = string.Empty;
            NotifyOfPropertyChange(() => Sheets);
            NotifyOfPropertyChange(() => SearchText);
        }

        public void RenameSheets()
        {
            var renameManager = new SCaddins.RenameUtilities.RenameManager(
                exportManager.Doc,
                selectedSheets.Select(s => s.Id).ToList());
            var renameSheetModel = new SCaddins.RenameUtilities.ViewModels.RenameUtilitiesViewModel(renameManager);
            renameSheetModel.SelectedParameterCategory = "Sheets";
            SCaddinsApp.WindowManager.ShowDialog(renameSheetModel, null, RenameUtilities.ViewModels.RenameUtilitiesViewModel.DefaultWindowSettings);
            foreach (ExportSheet exportSheet in selectedSheets)
            {
                exportSheet.UpdateName();
                exportSheet.UpdateNumber();
            }
            NotifyOfPropertyChange(() => Sheets);
        }

        public void Row_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs obj)
        {
            var grid = sender as System.Windows.Controls.DataGrid;
            selectedSheets = grid.SelectedItems.Cast<ExportSheet>().ToList();
            NotifyOfPropertyChange(() => ProgressBarText);
            NotifyOfPropertyChange(() => Sheets);
        }

        public void ShowLatestRevision()
        {
            var revDate = ExportManager.LatestRevisionDate(exportManager.Doc);
            var filter = new System.Predicate<object>(item => ((ExportSheet)item).SheetRevisionDate.Equals(revDate));
            Sheets.Filter = filter;
            NotifyOfPropertyChange(() => Sheets);
        }

        public void TryShowExportLog()
        {
            if (exportManager.ShowExportLog && log != null) {
                var vm = new ViewModels.ExportLogViewModel(log);
                SCaddinsApp.WindowManager.ShowDialog(vm, null, ViewModels.ExportLogViewModel.DefaultWindowSettings);
            }
        }

        public void TurnNorthPointsOff()
        {
            ExportManager.ToggleNorthPoints(selectedSheets, exportManager.Doc, false);
        }

        public void TurnNorthPointsOn()
        {
            ExportManager.ToggleNorthPoints(selectedSheets, exportManager.Doc, true);
        }

        public void VerifySheets()
        {
            exportManager.Update();
            NotifyOfPropertyChange(() => Sheets);
        }

        private void FilterByNumber(string number)
        {
            var activeSheetName = ExportManager.CurrentViewNumber(exportManager.Doc);
            var filter = new System.Predicate<object>(item => Regex.IsMatch(((ExportSheet)item).SheetNumber, @"^\D*" + number));
            Sheets.Filter = filter;
        }
    }
}