﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExcelUtility.Utils;

namespace ExcelUtility.Impl
{
    internal class Row : IRow
    {
        public static Row FromExisting(XElementData data, double defaultHeight, SharedStrings sharedStrings, SheetColumns sheetColumns)
        {
            return new Row(data, defaultHeight, sharedStrings, sheetColumns);
        }

        public static Row New(XElementData data, int index, double defaultHeight, SharedStrings sharedStrings, SheetColumns sheetColumns)
        {
            return new Row(data, index, defaultHeight, sharedStrings, sheetColumns);
        }

        private List<ICell> cells = null; // lazy load
        private double defaultHeight;
        private SharedStrings sharedStrings;
        private SheetColumns sheetColumns;

        public XElementData Data { get; set; }
        public int Index { get; private set; }
        public IEnumerable<ICell> DefinedCells { get { return LazyLoadCells(); } }
        
        public double Height
        { 
            get 
            {
                var ht = Data["ht"];
                return ht == null ? defaultHeight : double.Parse(ht, NumberFormatInfo.InvariantInfo);
            } 
            set 
            {
                Data.SetAttributeValue("ht", value == defaultHeight ? null : (object)value); 
            } 
        }

        // existing rows constructor
        private Row(XElementData data, double defaultHeight, SharedStrings sharedStrings, SheetColumns sheetColumns)
        {
            this.Data = data;
            this.defaultHeight = defaultHeight;
            this.sharedStrings = sharedStrings;
            this.sheetColumns = sheetColumns;
            Index = int.Parse(data["r"], NumberFormatInfo.InvariantInfo);
            data.RemoveAttribute("spans"); // clear spans attribute, will be recalculated
        }

        // new rows constructor
        private Row(XElementData data, int index, double defaultHeight, SharedStrings sharedStrings, SheetColumns sheetColumns)
        {
            if (index == 0)
                throw new ArgumentException("Row index can't be zero (0)", "index");
            this.Data = data;
            this.defaultHeight = defaultHeight;
            this.sharedStrings = sharedStrings;
            this.sheetColumns = sheetColumns;
            Index = index;
            data.SetAttributeValue("r", index);
            data.SetAttributeValue("x14ac", "dyDescent", 0.25); // office 2010 specific attribute
        }

        public ICell GetCell(string columnName)
        {
            LazyLoadCells();
            var search = new FakeCell() { Name = columnName + Index };
            int insert = cells.BinarySearch(search, CompareCells);
            if (insert < 0)
            {
                insert = ~insert;
                AddCell(columnName, insert);
            }
            return cells[insert];
        }

        private void AddCell(string columnName, int cellIndex)
        {
            XElementData cellData;
            if (cellIndex == 0)
                cellData = Data.Add("c");
            else
                cellData = ((Cell)cells[cellIndex - 1]).Data.AddAfterSelf("c");
            var newCell = Cell.New(cellData, columnName + Index, sharedStrings);
            newCell.Style = sheetColumns.GetColumn(columnName).Style;
            cells.Insert(cellIndex, newCell);
        }

        public ICell GetCell(int columnIndex)
        {
            return GetCell(ColumnUtil.GetColumnName(columnIndex));
        }

        private int CompareCells(ICell cell1, ICell cell2)
        {
            int compare = cell1.Name.Length.CompareTo(cell2.Name.Length);
            if (compare == 0)
                return cell1.Name.CompareTo(cell2.Name);
            return compare;
        }

        private IList<ICell> LazyLoadCells()
        {
            if (cells == null)
                cells = Data.Descendants("c").Select(c => (ICell)(Cell.FromExisting(c, sharedStrings))).ToList();
            return cells;
        }

        private class FakeCell : ICell
        {
            public string StringValue { get; set; }
            public double DoubleValue { get; set; }
            public string Name { get; set; }
            public long LongValue { get; set; }
            public bool IsTypeString { get; set; }
            public int? Style { get; set; }
        }

    }
}
