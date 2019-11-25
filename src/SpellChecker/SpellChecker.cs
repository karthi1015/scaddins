﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using NHunspell;
using Caliburn.Micro;
using System.Text.RegularExpressions;
using System.Collections;

namespace SCaddins.SpellChecker
{
    public class SpellChecker : IEnumerator
    {
        private Document document;
        private Hunspell hunspell;
        private List<CorrectionCandidate> allTextParameters;
        private int currentIndex;
        private CorrectionCandidate current;

        public SpellChecker(Document document)
        {
            this.document = document;

            hunspell = new Hunspell(
                            @"C:\Code\scaddins\etc\en_AU.aff",
                            @"C:\Code\scaddins\etc\en_AU.dic");

            //add some arch specific words
            hunspell.Add("approver");
            hunspell.Add(@"&");
            hunspell.Add(@"-");
            hunspell.Add(@"Autodesk");

            allTextParameters = GetAllTextParameters(document);
            currentIndex = -1;
        }

        ~SpellChecker()
        {
            hunspell.Dispose();
        }

        /// <summary>
        /// Return the current CorrectionCandidate
        /// </summary>
        public object Current => allTextParameters[currentIndex];

        public void AddToIgnoreList(string ignore)
        {
            hunspell.Add(ignore);
        }

        /// <summary>
        /// get spelling suggestions for the current CorrectionCandidate
        /// </summary>
        /// <returns></returns>
        public List<string> GetCurrentSuggestions()
        {
            return hunspell.Suggest(allTextParameters[currentIndex].Current as string);
        }

        public bool MoveNext()
        {
            if (allTextParameters == null || allTextParameters.Count == 0) return false;
            while (currentIndex < allTextParameters.Count) {
                if (currentIndex == -1) currentIndex = 0;
                if (allTextParameters[currentIndex].MoveNext()) {
                    return true;
                } else {
                    if (currentIndex < (allTextParameters.Count - 1)) {
                        currentIndex++;
                        if (allTextParameters[currentIndex].MoveNext()) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get all user modifiable parameters in the revit doc.
        /// Only get parameters of string storage types, as there's not much point spell cheking numbers.
        /// 
        /// </summary>
        /// <param name="doc">Revit doc to spell check</param>
        /// <returns>parmaeters</returns>
        private List<CorrectionCandidate> GetAllTextParameters(Document doc)
        {
            var candidates = new List<CorrectionCandidate>();
            var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

            foreach (Element element in collector) {
                var parameterSet = element.Parameters;
                foreach (var parameter in parameterSet) {

                    if (parameter is Autodesk.Revit.DB.Parameter) {
                        var p = (Autodesk.Revit.DB.Parameter)parameter;
                        if (p != null && p.StorageType == StorageType.String) {
                            var rc = new CorrectionCandidate(p, hunspell);
                            if (!string.IsNullOrEmpty(rc.OriginalText)) {
                                candidates.Add(rc);
                            }
                        }
                    }
                }
            }
            return candidates;
        }
    }
}
