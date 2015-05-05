using bluemetal.sharepoint.deployment.utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SP = Microsoft.SharePoint.Client;

namespace bluemetal.sharepoint.deployment {
    public class ListForm {
        public SP.PageType type { get; set; }
        public string url { get; set; }
        public string setupPath { get; set; }
        public string webPartZoneID { get; set; }
        public ListForm(SP.PageType type) {
            this.type = type;
            this.url = (type == SP.PageType.DisplayForm) ? "DispForm.aspx" : string.Concat(type.ToString(), ".aspx");
            this.setupPath = @"pages\form.aspx";
            this.webPartZoneID = "Main";
        }

        public XElement getSchemaXml() {
            return new XElement("Form",
                new XAttribute("Type", this.type.ToString()),
                new XAttribute("Url", this.url),
                new XAttribute("SetupPath", this.setupPath),
                new XAttribute("WebPartZoneID", this.webPartZoneID)
                );
        }
    }
}
