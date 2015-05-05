using SP = Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Specialized;

namespace bluemetal.sharepoint.deployment {
    public class SiteField : Field {
        public SiteField(Guid id, SP.FieldType type, string name) : base(type, name) {
            this.id = id;
        }

        protected override bool getDeployed() {
            bool returnValue = false;
            this.spField = this.parentWeb.spWeb.Fields.GetById(this.id.Value);
            this.isDeployed = returnValue = this.context.tryExecuteSync(this.spField);
            return returnValue;
        }

        internal override void onCreate() {
            this.context.executeAsync(
                () => this.spField = this.getParentSPWeb().Fields.AddFieldAsXml(this.getSchemaXml().ToString(), false, SP.AddFieldOptions.AddFieldInternalNameHint),
                () => this.isDeployed = this.getDeployed()
                );
        }

        internal override void onDelete() {
            throw new NotImplementedException();
        }
    }
}
