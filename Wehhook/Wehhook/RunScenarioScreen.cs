using System;
using System.Collections;
using System.Collections.Generic;
using PX.SM;
using PX.Data;
using PX.Objects.CS;
using PX.Api;
using PX.Objects.CR;
using PX.Objects.EP;
using System.Linq;

namespace ImportScenarioRun
{
	[PXHidden]
	public class ImportResultsDAC : PX.Data.IBqlTable
	{
		#region Selected

		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected>
		{
		}

		protected bool? _Selected = false;

		/// <summary>
		/// Indicates whether the record is selected for mass processing or not.
		/// </summary>
		[PXBool]
		[PXUIField(DisplayName = "Selected")]
		public bool? Selected
		{
			get
			{
				return _Selected;
			}
			set
			{
				_Selected = value;
			}
		}

		#endregion Selected

		#region GI

		public abstract class gI : PX.Data.BQL.BqlString.Field<gI>
		{
		}

		protected String _GI;

		[PXString(10, IsKey = true)]
		[PXUIField(DisplayName = "GI")]
		public virtual String GI
		{
			get
			{
				return this._GI;
			}
			set
			{
				this._GI = value;
			}
		}

		#endregion GI
	}
	public class RunScenarioScreen : PXGraph<RunScenarioScreen>
	{
        [PXVirtualDAC]
		public PXProcessing<ImportResultsDAC> Scenario;

		#region view delegate
		protected virtual IEnumerable scenario()
		{
			List<ImportResultsDAC> list = new List<ImportResultsDAC>();
			ImportResultsDAC item = new ImportResultsDAC();

			item.GI = "RUNSCENARIO";

			list.Add(item);
			return list;
		}

		#endregion view delegate
		public RunScenarioScreen()
		{
			Scenario.SetProcessEnabled(false);
			Scenario.SetProcessVisible(false);
			Scenario.SetProcessAllCaption("Run Scenario");
		}

		//public virtual void ImportResultsDAC_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		//{
		//	ImportResultsDAC row = e.Row as ImportResultsDAC;
		//	if (row != null)
		//	{
		//		string mappingid = "A6526122-2C3F-4CF7-94CE-4F315F617B7D"; //guid
		//		Scenario.SetProcessDelegate(list => RunScenario(mappingid, list));
		//		Scenario.SetProcessAllEnabled(true);
		//	}
		//}

		//public static void RunScenario(string mappingid, List<ImportResultsDAC> list)
		//{
		//	Guid mapid = new Guid(mappingid);
		//	RunScenarioScreen graph = PXGraph.CreateInstance<RunScenarioScreen>();
		//	RunScenario(graph, mapid);
		//}

		public static void RunScenario(RunScenarioScreen graph, Guid mappingid)
		{

			bool? prepareOperation = true;
			bool? importOperation = true;

			SYMappingActive mappingActive = PXSelect<SYMappingActive, Where<SYMappingActive.mappingID, Equal<Required<SYMappingActive.mappingID>>>>.Select(graph, mappingid);

			if (mappingActive != null)
			{

				SYImportOperation operation = new SYImportOperation();
				operation.MappingID = mappingActive.MappingID;
				operation.Operation = "C";      //C Prepare and Import,  P prepare
				operation.BreakOnError = mappingActive.BreakOnError;
				operation.BreakOnTarget = mappingActive.BreakOnTarget;
				operation.Validate = true;
				operation.ValidateAndSave = true;
				operation.SkipHeaders = mappingActive.SkipHeaders;

			try
				{
					
					var importGraph = PXGraph.CreateInstance<SYImportProcessSingle>();
					importGraph.MappingsSingle.Current = mappingActive;
					importGraph.Operation.Current = operation;
					importGraph.PrepareImport.Press();
					PXLongOperation.WaitCompletion(importGraph.UID);
					importGraph.Clear(PXClearOption.ClearAll);
					importGraph.MappingsSingle.Current = importGraph.MappingsSingle.Search<SYMapping.name>(mappingActive.Name);
					
					mappingActive = importGraph.MappingsSingle.Current;


				}
				catch (Exception ex)
				{

				}

			}

		}
	}
}
