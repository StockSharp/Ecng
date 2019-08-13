/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

using NPOI.SS.Formula;
using NPOI.XSSF.UserModel;
using System;
using NPOI.SS.UserModel;
namespace NPOI.XSSF.UserModel
{

    /**
     * XSSF wrapper for a cell under Evaluation
     * 
     * @author Josh Micich
     */
    public class XSSFEvaluationCell : IEvaluationCell
    {

        private IEvaluationSheet _evalSheet;
        private XSSFCell _cell;

        public XSSFEvaluationCell(ICell cell, XSSFEvaluationSheet EvaluationSheet)
        {
            _cell = (XSSFCell)cell;
            _evalSheet = EvaluationSheet;
        }

        public XSSFEvaluationCell(ICell cell)
            : this(cell, new XSSFEvaluationSheet(cell.Sheet))
        {

        }

        public Object IdentityKey => _cell;

	    public XSSFCell GetXSSFCell()
        {
            return _cell;
        }
        public bool BooleanCellValue => _cell.BooleanCellValue;

	    public CellType CellType => _cell.CellType;

	    public int ColumnIndex => _cell.ColumnIndex;

	    public int ErrorCellValue => _cell.ErrorCellValue;
	    public double NumericCellValue => _cell.NumericCellValue;
	    public int RowIndex => _cell.RowIndex;
	    public IEvaluationSheet Sheet => _evalSheet;
	    public String StringCellValue => _cell.RichStringCellValue.String;

	    #region IEvaluationCell ³ÉÔ±


        public CellType CachedFormulaResultType => _cell.CachedFormulaResultType;

	    #endregion
    }
}
