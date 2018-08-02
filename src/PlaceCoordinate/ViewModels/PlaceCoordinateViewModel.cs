﻿
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Caliburn.Micro;

namespace SCaddins.PlaceCoordinate.ViewModels
{
    class PlaceCoordinateViewModel : Screen
    {
        private List<Autodesk.Revit.DB.FamilySymbol> familiesInModel;
        private FamilySymbol selectedFamilySymbol;
        private Document doc;
        private bool useSharedCoordinates;

        public PlaceCoordinateViewModel(Document doc)
        {
            this.doc = doc;
            useSharedCoordinates = true;
            familiesInModel = Command.GetAllFamilySymbols(doc);
            selectedFamilySymbol = Command.TryGetDefaultSpotCoordFamily(familiesInModel, doc);
            if (selectedFamilySymbol == null) {
                selectedFamilySymbol = Command.TryLoadDefaultSpotCoordFamily(familiesInModel, doc); 
                if (selectedFamilySymbol != null) {
                    familiesInModel.Add(selectedFamilySymbol);
                }
            }
        }

        public double XCoordinate
        {
            get; set;
        }

        public double YCoordinate
        {
            get; set;
        }

        public double ZCoordinate
        {
            get; set;
        }

        public string XCoordinateLabel
        {
            get
            {
                return UseSharedCoordinates ? "East / West" : "X Value";
            }
        }

        public string YCoordinateLabel
        {
            get
            {
                return UseSharedCoordinates ? "North / South" : "Y Value";
            }
        }

        public string ZCoordinateLabel
        {
            get
            {
                return UseSharedCoordinates ? "Elevation" : "Z Value";
            }
        }


        public bool UseSharedCoordinates
        {
            get { return useSharedCoordinates; }
            set
            {
                if (value != useSharedCoordinates)
                {
                    useSharedCoordinates = value;
                    NotifyOfPropertyChange(() => XCoordinateLabel);
                    NotifyOfPropertyChange(() => YCoordinateLabel);
                    NotifyOfPropertyChange(() => ZCoordinateLabel);
                }
            }
        }

        public FamilySymbol SelectedFamilySymbol
        {
            get
            {
                return selectedFamilySymbol;
            }
            set
            {
                if (value != selectedFamilySymbol)
                {
                    selectedFamilySymbol = value;
                    NotifyOfPropertyChange(() => SelectedFamilySymbol);
                    NotifyOfPropertyChange(() => PlaceFamilyAtCoordinateIsEnabled);
                }
            }
        }

        public List<Autodesk.Revit.DB.FamilySymbol> FamilySymbols
        {
            get {
                return familiesInModel;
            } set
            {
                if (value != familiesInModel)
                {
                    familiesInModel = value;
                    NotifyOfPropertyChange(() => FamilySymbols);
                }
            }
        }

        public static dynamic DefaultWindowSettings
        {
            get
            {
                dynamic settings = new System.Dynamic.ExpandoObject();
                settings.Height = 400;
                //settings.Icon = new System.Windows.Media.Imaging.BitmapImage(
                //  new System.Uri("pack://application:,,,/SCaddins;component/Assets/scasfar.png")
                //  );
                settings.Title = "Place Coordinate - By Andrew Nicholas";
                settings.ShowInTaskbar = false;
                settings.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                settings.ResizeMode = System.Windows.ResizeMode.CanResize;
                return settings;
            }
        }

        public void Cancel()
        {
            TryClose(false);
        }

        public void PlaceFamilyAtCoordinate()
        {
            Command.PlaceFamilyAtCoordinate(doc, SelectedFamilySymbol, new XYZ(XCoordinate, YCoordinate, ZCoordinate), UseSharedCoordinates);
            TryClose(true);
        }

        public bool PlaceFamilyAtCoordinateIsEnabled
        {
            //get { return false; }
            get { return SelectedFamilySymbol != null; }
        }
    }
}