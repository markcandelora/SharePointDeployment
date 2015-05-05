using bluemetal.sharepoint.deployment.utilities;
using SP = Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.IO;

namespace bluemetal.sharepoint.deployment {
    public class List : Deployable {
        internal SP.List spList;

        public List(SP.ListTemplateType templateType, string title) {
            this.templateType = templateType;
            this.title = title;
            this.urlSegment = UrlUtility.JoinUrl("Lists", title.removeNonWordCharacters());

            this.ContentTypes = new List<ContentType>();
            this.Fields = new List<ListField>() { ListField.TitleFieldDefinition };
            this.Views = new List<ListView>() { ListView.GetDefaultView() };
            this.Forms = new List<ListForm>() {
                new ListForm(SP.PageType.DisplayForm),
                new ListForm(SP.PageType.EditForm),
                new ListForm(SP.PageType.NewForm)
                };
        }

        protected override void onInit() {
            this.forEachChild((i) => {
                if (i is ListField) {
                    ((ListField)i).parentList = this;
                }
                });
        }

        internal override void onInvalidate() {
            this.spList = null;
        }

        public override void forEachChild(Action<Deployable> action) {
            if (this.Fields != null) this.Fields.ForEach(action);
            if (this.ContentTypes != null) this.ContentTypes.ForEach(action);
            if (this.Forms != null) this.Views.ForEach(action);
            if (this.Views != null) this.Views.ForEach(action);
        }

        protected override bool getDeployed() {
            var returnValue = false;
            var spWeb = this.getParentSPWeb();
            this.spList = spWeb.Lists.GetByTitle(this.title);
            this.isDeployed = returnValue = this.context.tryExecuteSync(this.spList);
            return returnValue;
        }

        internal override void onDelete() {
            throw new NotImplementedException();
        }

        public void preDeploy() {
            throw new NotImplementedException();
        }

        internal override void create() {
            if (!this.isDeployed.Value) {
                this.onCreate();
            } else {
                this.forEachChild(i => i.create());
            }
        }

        internal override void onCreate() {
            if (!this.isDeployed.Value) {
                var doc = this.getSchemaXml();
                var list = doc.Root;
                SP.ListCreationInformation info = new SP.ListCreationInformation() {
                    Title = list.Attribute("Title").Value,
                    Url = list.Attribute("Url").Value,
                    TemplateType = (int)SP.ListTemplateType.GenericList,
                    TemplateFeatureId = new Guid("00bfea71-de22-43b2-a848-c05709900100"),
                    CustomSchemaXml = doc.ToString()
                };

                var web = this.getParentSPWeb();
                this.context.executeAsync(() => web.Lists.Add(info), () => { this.isDeployed = this.getDeployed(); });
            } else {
                this.forEachChild(i => i.onCreate());
            }
        }

        public void postDeploy() {
            throw new NotImplementedException();
        }

        public void LoadFromXml(System.Xml.XmlReader reader) {
            if (reader.LocalName == "List") {
                this.title = reader.GetAttribute("title");
                this.description = reader.GetAttribute("description");
                this.quickLaunchOption = reader.GetAttributeEnum("quickLaunchOption", SP.QuickLaunchOptions.DefaultValue);
                this.templateType = (SP.ListTemplateType)reader.GetAttributeInt("templateType", 101);
                this.urlSegment = reader.GetAttribute("url");
            }
        }

        public string fullUrl {
            get { return UrlUtility.JoinUrl(this.parentWeb.fullUrl, this.urlSegment); }
        }

        protected virtual XDocument getSchemaXml() {
            XDocument returnValue;
            if (string.IsNullOrEmpty(this.xmlDefinition)) {
                XElement contentTypes = new XElement("ContentTypes", "");
                XElement fields = new XElement("Fields");
                XElement views = new XElement("Views");
                XElement forms = new XElement("Forms");

                XElement list = new XElement(XName.Get("List", "http://schemas.microsoft.com/sharepoint/"),
                                                new XAttribute(XNamespace.Xmlns + "ows", "Microsoft SharePoint"),
                                                new XAttribute("Title", this.title),
                                                new XAttribute("Url", this.urlSegment),
                                                new XAttribute("BaseType", 0),
                                                new XAttribute("FolderCreation", this.allowFolderCreation.ToString(Boolean.Case.Upper)),
                                                new XElement("MetaData", contentTypes, fields, views, forms)
                                                );

                if (!string.IsNullOrEmpty(this.description)) list.Add(new XAttribute("Description", this.description));

                //Add fields (include fields from content types that might be missing from the fields collection)
                this.ContentTypes.ForEach(ct => {
                    ct.Fields.Where(ctf => { return !this.Fields.Any(f => ctf.id == f.id); })
                             .ForEach(i => { this.Fields.Add(i); });
                });
                this.Fields.ForEach(i => fields.Add(i.getSchemaXml()));

                //Add content types
                this.ContentTypes.ForEach(i => contentTypes.Add(i.getSchemaXml()));

                //Add views
                this.Views.ForEach(i => views.Add(i.getSchemaXml()));

                //Add list forms
                this.Forms.ForEach(i => forms.Add(i.getSchemaXml()));

                list.setNamespaceOnDecendants(XNamespace.Get("http://schemas.microsoft.com/sharepoint/"));

                returnValue = new XDocument(list);
            } else {
                returnValue = XDocument.Parse(this.xmlDefinition);
            }

            return returnValue;
        }

        #region List data
        
        #region GetItems
        public List<ListItem> GetItems(SP.CamlQuery query) {
            var returnValue = new List<ListItem>();
            var items = this.spList.GetItems(query);
            this.context.executeSync(items);

            foreach (var item in items) {
                returnValue.Add(ListItem.FromSpItem(item));
            }

            return returnValue;
        }
        public List<ListItem> GetItems(string viewXml, string folderPath, SP.ListItemCollectionPosition position) {
            return this.GetItems(new SP.CamlQuery() { ViewXml = viewXml, FolderServerRelativeUrl = folderPath, ListItemCollectionPosition = position });
        }
        public List<ListItem> GetItems(string folderPath, SP.ListItemCollectionPosition position, string query, int rowLimit, string scope, params string[] fields) {
            return this.GetItems(ListView.getBasicSchemaXml(query, rowLimit, true, scope, fields).ToString(), folderPath, position);
        }
        public List<ListItem> GetItems() {
            return this.GetItems(SP.CamlQuery.CreateAllItemsQuery());
        }
        #endregion

        #region Add Items
        public void AddItems(params ListItem[] items) { this.AddItems("", items); }
        public void AddItems(string folderUrl, params ListItem[] items) {
            items.ForEach(i => this.context.executeAsync(() => {
                var spItem = this.spList.AddItem(new SP.ListItemCreationInformation() {
                    FolderUrl = folderUrl, 
                    LeafName = null,
                    UnderlyingObjectType = SP.FileSystemObjectType.File
                    });
                i.applyValues(spItem);
                spItem.Update();
            }));
        }
        #endregion
        
        #endregion

        #region Definition Info
        public string title { get; set; }
        public string description { get; set; }
        public SP.QuickLaunchOptions quickLaunchOption { get; set; }
        public SP.ListTemplateType templateType { get; set; }
        public string urlSegment { get; set; }
        public bool allowFolderCreation { get; set; }
        public string xmlDefinition { get; set; }
        #endregion

        #region Children
        public List<ListField> Fields { get; set; }
        public List<ContentType> ContentTypes { get; set; }
        public List<ListView> Views { get; set; }
        public List<ListForm> Forms { get; set; }
        #endregion
    }
}
