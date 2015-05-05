using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SP = Microsoft.SharePoint.Client;

namespace bluemetal.sharepoint.deployment {
    public class ListView : Deployable {
        internal SP.View spListView;

        public ListView(string name, params string[] viewFields) : this(ListViewType.HTML, name, viewFields) { }
        public ListView(ListViewType type, string name, params string[] viewFields) {
            if (name == null) throw new ArgumentException("Name cannot be null", "name");

            this.type = type;
            this.name = name;
            this.nameResource = nameResource;
            this.query = query;
            this.viewFields = new List<string>(viewFields);
            this.url = string.Concat(name.removeNonWordCharacters(), ".aspx");
            this.query = "<Where />";
            this.toolbarType = ListToolbarType.Standard;
            this.xslLink = "main.xsl";
            this.xslLinkDefault = true;
            this.jsLink = "clienttemplates.js";
            this.rowLimit = 30;
            this.paged = true;
            this.setupPath = @"pages\viewpage.aspx";
            this.webPartZoneId = "Main";
            this.baseViewId = 1;
        }

        public static ListView GetDefaultView() {
            return new ListView("All Items", "LinkTitle") { defaultView = true, url = "AllItems.aspx", nameResource = "$Resources:core,objectiv_schema_mwsidcamlidC24;" };
        }

        public static ListView GetViewAllFields(string name, List list) {
            return new ListView(name, list.Fields.Select(i => i.internalName).ToArray());
        }

        public static XElement getBasicSchemaXml(string query, int rowLimit, bool paged, string scope, params string[] fields) {
            XElement returnValue;
            var viewFields = new XElement("ViewFields");
            returnValue = new XElement("View", viewFields);
            fields.ForEach(i => viewFields.Add(new XElement("FieldRef", new XAttribute("Name", i), "")));

            if (rowLimit > 0)                        returnValue.Add(new XElement("RowLimit", new XAttribute("Paged", paged.ToString(Boolean.Case.Upper)), rowLimit));
            if (!string.IsNullOrEmpty(query))        returnValue.Add(new XElement("Query", XElement.Parse(query)));
            /* Add scope to view definition */

            return returnValue;
        }

        public XElement getSchemaXml() {
            XElement returnValue = ListView.getBasicSchemaXml(this.query, this.rowLimit, this.paged, this.scope, this.viewFields.ToArray());
            returnValue.Add(
                new XAttribute("BaseViewID", this.baseViewId),
                new XAttribute("Type", this.type.ToString()),
                new XAttribute("DisplayName", this.nameResource ?? this.name),
                new XAttribute("SetupPath", this.setupPath),
                new XAttribute("Url", this.url),
                new XAttribute("WebPartZoneID", this.webPartZoneId)
                );

            if (this.defaultView)                         returnValue.Add(new XAttribute("DefaultView", this.defaultView.ToString(Boolean.Case.Upper)));
            if (this.toolbarType != ListToolbarType.None) returnValue.Add(new XElement("Toolbar", new XAttribute("Type", this.toolbarType.ToString())));
            if (!string.IsNullOrEmpty(this.xslLink))      returnValue.Add(new XElement("XslLink", new XAttribute("Default", this.xslLinkDefault.ToString(Boolean.Case.Upper)), this.xslLink));
            if (!string.IsNullOrEmpty(this.jsLink))       returnValue.Add(new XElement("JSLink", this.jsLink));

            return returnValue;
        }

        protected override void onInit() {
        }

        internal override void onInvalidate() {
            this.spListView = null;
        }

        protected override bool getDeployed() {
            var returnValue = false;
            var list = (List)this.parent;
            var view = list.spList.Views.GetByTitle(this.name);
            if (returnValue = this.context.tryExecuteSync(view)) {
                this.spListView = view;
            }
            return returnValue;
        }

        public override void forEachChild(Action<Deployable> action) {
        }

        internal override void onCreate() {
        }

        internal override void onDelete() {
        }

        #region View properties
        public string xslLink { get; set; }
        public bool xslLinkDefault { get; set; }
        public string jsLink { get; set; }
        public ListToolbarType toolbarType { get; set; }
        public ListViewType type { get; set; }
        public string name { get; set; }
        public string query { get; set; }
        public int rowLimit { get; set; }
        public bool paged { get; set; }
        public string scope { get; set; }
        public List<string> viewFields { get; set; }
        public bool defaultView { get; set; }
        public string setupPath { get; set; }
        public string url { get; set; }
        public string webPartZoneId { get; set; }
        public int baseViewId { get; set; }
        public string nameResource { get; set; }
        #endregion
    }
}
