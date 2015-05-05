using SP = Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Specialized;

namespace bluemetal.sharepoint.deployment {
    public class ListField : Field {
        public List parentList { get; set; }
        public SiteField siteField { get; protected set; }
        public bool isSiteField { get; protected set; }

        public ListField(SP.FieldType type, string name) : base(type, name) { }

        protected ListField(Guid id) {
            this.isSiteField = true;
            this.id = id;
        }

        protected override bool getDeployed() {
            bool returnValue = false;
            if (this.id.HasValue) {
                this.spField = this.parentWeb.spWeb.Fields.GetById(this.id.Value);
            } else {
                this.spField = this.parentWeb.spWeb.Fields.GetByInternalNameOrTitle(this.internalName);
            }
            this.isDeployed = returnValue = this.context.tryExecuteSync(this.spField);
            return returnValue;
        }

        protected override void onInit() {
            if (this.isSiteField) {
                var web = this.parentWeb;
                while (this.siteField == null && web != null) {
                    if (web.fields != null) this.siteField = web.fields.FirstOrDefault(i => i.id == this.id);
                    web = web.parentWeb;
                }
                if (this.siteField == null) throw new InvalidOperationException(string.Format("Can not find site field with id {0}", this.id));
            } else {
                base.onInit();
            }
        }

        internal override void onCreate() {
            if (this.isSiteField) {
                this.context.executeSync(() => { this.spField = this.parentList.spList.Fields.Add(this.siteField.spField); } );
                this.isDeployed = true;
            } else {
                this.context.executeAsync(
                    () => this.spField = this.parentList.spList.Fields.AddFieldAsXml(this.getSchemaXml().ToString(), this.addToDefaultView, this.fieldOptions),
                    () => this.isDeployed = this.getDeployed()
                    );
            }
        }

        internal override void onDelete() {
            throw new NotImplementedException();
        }

        internal override XElement getSchemaXml() {
            return (this.isSiteField) ? this.siteField.getSchemaXml() : base.getSchemaXml();
        }

        public bool addToDefaultView { get; set; }
        public SP.AddFieldOptions fieldOptions { get; set; }

        public static ListField FromSiteField(SiteField field) {
            return ListField.FromSiteField(field.id.Value);
        }

        public static ListField FromSiteField(Guid id) {
            return new ListField(id);
        }

        public static ListField TitleFieldDefinition {
            get {
                return new ListField(SP.FieldType.Text, "$Resources:core,Title;") {
                    id = new Guid("fa564e0f-0c70-4ab9-b863-0177e6ddd247"),
                    required = true,
                    internalName = "Title",
                    options = { { "SourceID", "http://schemas.microsoft.com/sharepoint/v3" }, 
                                { "StaticName", "Title" }, 
                                { "MaxLength", "255" } }
                };
            }
        }
    }
}
