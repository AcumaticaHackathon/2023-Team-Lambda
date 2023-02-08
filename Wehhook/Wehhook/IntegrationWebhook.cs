using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Web;

using PX.Data;
using PX.Data.Webhooks;
using PX.Objects.CR;
using PX.Objects.PM;
using Newtonsoft.Json;
using System.Collections.Generic;
using PX.Api;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using PX.Data.Wiki.Parser;

namespace Wehhook
{

    public class Record
    {
        public string Origin { get; set; }
        public string Type { get; set; }
        public List<dynamic> Data { get; set; }
    }



    public class IntegrationWebhook : IWebhookHandler
    {

        public async Task<System.Web.Http.IHttpActionResult> ProcessRequestAsync(
          HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ok = new OkResult(request);
            Dictionary<string, string> notification = new Dictionary<string, string>();
            string origin = "";
            string dataType = "";
            string content = "";
            using (var scope = GetAdminScope())
            {
                content = await request.Content.ReadAsStringAsync();
                notification = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                var _queryParameters = HttpUtility.ParseQueryString(request.RequestUri.Query);

                origin = _queryParameters[0];
                dataType = _queryParameters[1];
            }


            PX.Common.PXContext.SetScreenID("SM206015");
            PX.Common.PXContext.SetBranchID(16);

            SYProviderMaint graph = PXGraph.CreateInstance<SYProviderMaint>();

            string recordName = origin + "_" + dataType;
            SYProvider provider = new SYProvider
            {
                Name = origin + "_" + dataType,
            };

            SYProvider prov = PXSelect<SYProvider,
            Where<SYProvider.name, Equal<Required<SYProvider.name>>>>.Select(graph, provider.Name);
            if (prov == null)
            {
                provider = graph.Providers.Insert(provider);

                provider.ProviderType = "PX.DataSync.XMLSYProvider";
                provider = graph.Providers.Update(provider);
                prov = provider;
                foreach (SYProviderParameter param in graph.Parameters.Select())
                {
                    switch (param.Name)
                    {
                        case "FileName":
                            param.Value = "filename";
                            break;
                        case "Encoding":
                            param.Value = "US-ASCII";
                            break;
                        case "Format":
                            param.Value = "Flat";
                            break;

                    }
                    graph.Parameters.Update(param);
                }


                graph.Actions.PressSave();

                PX.SM.FileInfo file = AddData(graph.Providers.Cache, graph.Providers.Current, provider.Name, content);

                //object t = new object();

                //SYProviderParameter param = PXSelect<SYProviderParameter, Where<SYProviderParameter.providerID, Equal<Current<SYProvider.providerID>>,And<SYProviderParameter.name>>.Select(graph);

                graph.Actions.PressSave();

                //graph.Parameters.Cache.RaiseFieldSelecting<SYProviderParameter.value>(graph.Parameters.Current, ref t, true);

                //string extension = file.Name.Substring(Math.Max(0, file.Name.LastIndexOf('.')));
                //string prefix = MimeTypes.GetMimeType(extension).StartsWith("image/") ? "Image:" : "{up}";
                //string WikiLink = "[" + prefix + PXBlockParser.EncodeSpecialChars(file.Name) + "]";
                object newval = new object();
                foreach (SYProviderParameter param in graph.Parameters.Select())
                {
                    //graph.Parameters.Cache.RaiseFieldDefaulting<SYProviderParameter.value>(param, out newval);
                    switch (param.Name)
                    {
                        case "FileName":
                            param.Value = GetAttachedFileName(graph);
                            break;

                    }
                    graph.Parameters.Update(param);
                }
                graph.Actions.PressSave();

                graph.FillSchemaObjects.Press();

                foreach (SYProviderObject obj in graph.Objects.Select())
                {
                    //graph.Parameters.Cache.RaiseFieldDefaulting<SYProviderParameter.value>(param, out newval);

                    obj.IsActive = true;

                    graph.Objects.Update(obj);
                }

                graph.FillSchemaFields.Press();
                graph.Actions.PressSave();
            }

            SYMapping map = PXSelect<SYMapping,
            Where<SYMapping.name, Equal<Required<SYMapping.name>>>>.Select(graph, provider.Name);
            if (map == null)
            {
                SYImportMaint_Extension.CreateScenario(prov.Name, prov.ProviderID);
            }
            else if (map.ScreenID != null)
            {

                RunScenario(graph, map.MappingID);
                //SYImportProcessSingle graphProc = PXGraph.CreateInstance<SYImportProcessSingle>();


                //SYMappingActive mapsingle = PXSelect<SYMappingActive, Where<SYMappingActive.mappingType, Equal<SYMapping.mappingType.typeImport>, And<SYMappingActive.isActive, Equal<True>, And<SYMappingActive.providerType, NotEqual<BPEventProviderType>>>>>.Select(graphProc);

            }
            return ok;
        }

        private IDisposable GetAdminScope()
        {
            var userName = "admin";
            if (PXDatabase.Companies.Length > 0)
            {
                var company = PXAccess.GetCompanyName();
                if (string.IsNullOrEmpty(company))
                {
                    company = PXDatabase.Companies[0];
                }
                userName = userName + "@" + company;
            }
            return new PXLoginScope(userName);
        }

        private string GetAttachedFileName(PXGraph graph)
        {
            //Clear cached select
            PXSelectJoin<PX.SM.UploadFile,
                                    InnerJoin<NoteDoc, On<PX.SM.UploadFile.fileID, Equal<NoteDoc.fileID>>>,
                                    Where<NoteDoc.noteID, Equal<Current<SYProvider.noteID>>>,
                                    OrderBy<Desc<PX.SM.UploadFile.createdDateTime>>>.Clear(graph);
            PX.SM.UploadFile file = PXSelectJoin<PX.SM.UploadFile,
                                    InnerJoin<NoteDoc, On<PX.SM.UploadFile.fileID, Equal<NoteDoc.fileID>>>,
                                    Where<NoteDoc.noteID, Equal<Current<SYProvider.noteID>>>,
                                    OrderBy<Desc<PX.SM.UploadFile.createdDateTime>>>.Select(graph);
            return file == null ? null : file.Name;
        }

        public static PX.SM.FileInfo AddData(PXCache cache, SYProvider provider, string name, string data)
        {
            string xml = MapToXML(data);
            //Build the file name.
            var fileName = name + ".xml";
            //Create xml file
            var file = new PX.SM.FileInfo(fileName, null, Encoding.UTF8.GetBytes(xml));
            //Attach the file
            return AttachFile(cache, provider, file);
        }
        public static PX.SM.FileInfo AttachFile(PXCache sender, SYProvider obj, PX.SM.FileInfo file)
        {
            PX.Common.PXContext.SetScreenID("SM206015");
            var filegraph = PXGraph.CreateInstance<PX.SM.UploadFileMaintenance>();
            if (filegraph.SaveFile(file, PX.SM.FileExistsAction.CreateVersion))
            {

                PXNoteAttribute.AttachFile(sender, obj, file);
                //PXDatabase.Insert<NoteDoc>(
                //                    new PXDataFieldAssign("NoteID", PXDbType.UniqueIdentifier, obj.NoteID),
                //                    new PXDataFieldAssign("FileID", PXDbType.UniqueIdentifier, file.UID));

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

        public static void RunScenario(SYProviderMaint graph, Guid? mappingid)
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
                //operation.Validate = true;
                //operation.ValidateAndSave = true;
                //operation.SkipHeaders = mappingActive.SkipHeaders;

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
