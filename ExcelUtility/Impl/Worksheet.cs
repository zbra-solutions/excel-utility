﻿using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace ExcelUtility.Impl
{
    internal class Worksheet : IWorksheet
    {
        #region IWorksheet Members

        private XElement sheet;
        private XNamespace Namespace { get { return sheet.GetDefaultNamespace(); } }

        public SharedStrings SharedStrings { get; set; }
        public string Name { get; set; }
        public int SheetId { get; set; }

        internal Worksheet(XElement sheet)
        {
            this.sheet = sheet;
        }

        public Column GetColumn(string name)
        {
            /*
            return (from col in Sheet.Descendants(XName.Get("col", Sheet.GetDefaultNamespace().ToString()))
                    where col.At
                    select new Column(this, name)
                       ).FirstOrDefault();
             */
            return null;
        }

        public Cell GetCell(string name)
        {
            Cell cell = (from c in sheet.Descendants(Namespace + "c")
                         where c.Attribute("r").Value == name
                         select new Cell(c, this, name)).FirstOrDefault();
            return cell == null ? CreateNewCell(name) : cell;
        }

        public Row GetRow(string name)
        {
            throw new NotImplementedException();
        }

        public Shape DrawShape(double x, double y, double width, double height)
        {
            throw new NotImplementedException();
        }

        public void SaveChanges(string xmlPath)
        {
            SharedStrings.Save(xmlPath);
            sheet.Save(string.Format("{0}/worksheets/sheet{1}.xml", xmlPath, SheetId));
        }

        public void RemoveUnusedStringReferences(IList<StringReference> unusedStringRefences)
        {
            IEnumerable<XElement> cells = (from cell in sheet.Descendants(Namespace + "c")
                                           where cell.Attribute("t") != null && cell.Attribute("t").Value == "s"
                                           select cell).ToList();
            foreach (XElement cell in cells)
                if (unusedStringRefences.Any(c => c.Index == Convert.ToInt32(cell.Descendants(Namespace + "v").First().Value)))
                    unusedStringRefences.Remove(unusedStringRefences.First(c => c.Index == Convert.ToInt32(cell.Descendants(Namespace + "v").First().Value)));
        }

        public void UpdateStringReferences(IList<StringReference> stringRefences)
        {
            foreach (StringReference stringReference in stringRefences)
            {
                IList<XElement> cells = (from cell in sheet.Descendants(Namespace + "c")
                                         where cell.Attribute("t") != null && cell.Attribute("t").Value == "s" && Convert.ToInt32(cell.Descendants(Namespace + "v").First().Value) == stringReference.OldIndex
                                         select cell).ToList();
                foreach (XElement cell in cells)
                    cell.Descendants(Namespace + "v").First().Value = stringReference.Index.ToString();
            }
        }

        public int CountStringsUsed()
        {
            return (from cell in sheet.Descendants(Namespace + "c")
                    where cell.Attribute("t") != null && cell.Attribute("t").Value == "s"
                    select cell).ToList().Count;
        }

        #endregion

        private Cell CreateNewCell(string name)
        {
            XElement newCell = new XElement(Namespace + "c", new XElement(Namespace + "v"));
            newCell.SetAttributeValue("r", name);
            XElement row = sheet.Descendants(Namespace + "row").Where(r => r.Attribute("r").Value == Regex.Match(name, @"\d+").Value).FirstOrDefault();
            //Check if row is null.
            row.Add(newCell);
            return new Cell(newCell, this, name);
        }
    }
}
