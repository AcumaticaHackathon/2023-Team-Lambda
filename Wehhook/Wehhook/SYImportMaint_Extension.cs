using System;
using PX.Objects;
using PX.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using static PX.SM.EMailAccount;
using System.Text;
using PX.Objects.GL;
namespace PX.Api
{
    public class SYImportMaint_Extension : PXGraphExtension<PX.Api.SYImportMaint>
    {
        #region Event Handlers        public PXAction<PX.Api.SYMapping> Attach;
        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Test")]
        protected void attach()
        {
            // Test values            string json = "{\"field1\":\"SO\",\"field2\":\"000001\",\"field3\":\"On Hold\",\"field4\":\"1\",\"field5\":\"Battery\",\"field6\":\"15\"}";
            string name = "MyTestName_Type";
            PXGraph providerGraph = PXGraph.CreateInstance<SYProviderMaint>();
            SYProvider providerTemplate = PXSelect<SYProvider, Where<SYProvider.providerID,
                Equal<Required<SYMapping.providerID>>,
                And<SYProviderObject.isActive, Equal<True>>>>.Select(providerGraph, new Guid("6e95363b-82eb-4faa-9ba8-4d1c3e164181"));
            //-----------------------------            //var provider = CreateProvider(name);            var scenario = CreateScenario(name, providerTemplate.ProviderID); // provider.ProviderID); // new Guid("e6ed068c-1ce9-4089-8e21-633482f7b0f9"));            AddData(providerGraph.Caches["SYProvider"], providerTemplate, name , json);
        }
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), "PersistingCheck", PXPersistingCheck.Nothing)]
        protected virtual void SYMapping_ScreenID_CacheAttached(PXCache sender) { }
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), "PersistingCheck", PXPersistingCheck.Nothing)]
        protected virtual void SYMapping_GraphName_CacheAttached(PXCache sender) { }
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), "PersistingCheck", PXPersistingCheck.Nothing)]
        protected virtual void SYMapping_ViewName_CacheAttached(PXCache sender) { }
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), "PersistingCheck", PXPersistingCheck.Nothing)]
        protected virtual void SYMapping_GridViewName_CacheAttached(PXCache sender) { }
        protected void _(Events.RowSelected<SYMapping> e, PXRowSelected BaseMethod)
        {
            BaseMethod(e.Cache, e.Args);
            PXUIFieldAttribute.SetRequired<SYMapping.screenID>(this.Base.Mappings.Cache, false);
            PXUIFieldAttribute.SetRequired<SYMapping.graphName>(this.Base.Mappings.Cache, false);
            PXUIFieldAttribute.SetRequired<SYMapping.viewName>(this.Base.Mappings.Cache, false);
            PXUIFieldAttribute.SetRequired<SYMapping.gridViewName>(this.Base.Mappings.Cache, false);
        }
        public static SYProvider CreateProvider(string name)
        {
            PXGraph graph = PXGraph.CreateInstance<SYProviderMaint>();
            var cache = graph.Caches["SYProvider"];
            SYProvider providerTemplate = PXSelect<SYProvider, Where<SYProvider.providerID,
                Equal<Required<SYMapping.providerID>>,
                And<SYProviderObject.isActive, Equal<True>>>>.Select(graph, new Guid("6e95363b-82eb-4faa-9ba8-4d1c3e164181"));
            SYProvider provider = (SYProvider)cache.CreateCopy(providerTemplate);
            cache.SetValueExt<SYProvider.name>(provider, name);
            //    new SYProvider()            //{            //    Name = name,            //    ProviderType = "PX.DataSync.XMLSYProvider"            //};            cache.Insert(provider);
            cache.Current = provider;
            //graph.Mappings.SetValueExt<SYMapping.providerID>(scenario, providerID);            //scenario.ProviderObject = fileName;            graph.Actions.PressSave();
            return provider;
        }
        public static SYMapping CreateScenario(string name, Guid? providerID)
        {
            PXGraph graph = PXGraph.CreateInstance<SYImportMaint>();
            SYProviderObject providerObject = PXSelect<SYProviderObject, Where<SYProviderObject.providerID,
                Equal<Required<SYMapping.providerID>>,
                And<SYProviderObject.isActive, Equal<True>>>>.Select(graph, providerID);
            var scenario = new SYMapping()
            {
                Name = name,
                ProviderID = providerID,
                ProviderObject = providerObject?.Name
            };
            var cache = graph.Caches["SYMapping"];
            scenario = (SYMapping)cache.Insert(scenario);
            //graph.Mappings.SetValueExt<SYMapping.providerID>(scenario, providerID);            //scenario.ProviderObject = fileName;            graph.Actions.PressSave();
            graph.Actions.PressSave();
            return scenario;
        }
        public static void AddData(PXCache cache, SYProvider provider, string name, string data)
        {
            string xml = MapToXML(data);
            //Build the file name.
            var fileName = name + ".xml";
            //Create xml file
            var file = new PX.SM.FileInfo(fileName, null, Encoding.UTF8.GetBytes(xml));
            //Attach the file
            AttachFile(cache, provider, file);
        }
        public static PX.SM.FileInfo AttachFile<TDac>(PXCache sender, TDac obj, PX.SM.FileInfo file)
        {
            var filegraph = PXGraph.CreateInstance<PX.SM.UploadFileMaintenance>();
            if (filegraph.SaveFile(file, PX.SM.FileExistsAction.CreateVersion))
            {
                PXNoteAttribute.AttachFile(sender, obj, file);
            }
            return file;
        }
        public static string MapToXML(string json)
        {
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var xml = new XElement("Root",
                new XElement("Columns",
                    from key in dictionary.Keys select new XElement("Column", new XAttribute("Name", key))
                ),
                new XElement("Rows",
                    new XElement("Row",
                        from key in dictionary.Keys select new XAttribute(key, dictionary[key])
                    )
                )
            );
            return xml.ToString();
        }
        #endregion    }
    }
}