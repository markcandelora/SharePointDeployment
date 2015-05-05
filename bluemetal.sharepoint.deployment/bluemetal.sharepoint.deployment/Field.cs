using SP = Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace bluemetal.sharepoint.deployment {
    public abstract class Field : Deployable {
        internal SP.Field spField { get; set; }

        protected Field() { }

        public Field(SP.FieldType type, string name) {
            this.fieldType = type;
            this.internalName = name;
            this.title = name;

            this.choices = new List<string>();
            this.options = new Dictionary<string,string>();
        }

        protected override void onInit() {
            if (string.IsNullOrEmpty(this.internalName)) {
                this.internalName = this.title.removeNonWordCharacters();
            }
        }

        internal override void onInvalidate() {
            this.spField = null;
        }

        public override void forEachChild(Action<Deployable> action) { }

        internal virtual XElement getSchemaXml() {
            XElement returnValue = new XElement("Field");
            
            if (this.id.HasValue) returnValue.Add(new XAttribute("ID", this.id.Value.ToString("B")));

            returnValue.Add(
                new XAttribute("Type", this.fieldType.ToString()),
                new XAttribute("Name", this.internalName),
                new XAttribute("DisplayName", this.title),
                new XAttribute("Required", this.required.ToString(Boolean.Case.Upper))
                );

            if (!string.IsNullOrEmpty(this.defaultValue)) {
                returnValue.Add(new XAttribute("Default", this.defaultValue));
            }

            foreach (string key in this.options.Keys) {
                returnValue.Add(new XAttribute(key, this.options[key]));
            }

            if (this.fieldType == SP.FieldType.Choice) {
                XElement choices = new XElement("Choices");
                this.choices.ForEach(i => choices.Add(new XAttribute("Choice", i)));
                returnValue.Add(choices);
            }

            return returnValue;
        }

        public virtual SP.FieldType fieldType { get; set; }
        public virtual Guid? id { get; set; }
        public virtual string title { get; set; }
        public virtual string internalName { get; set; }
        public virtual string defaultValue { get; set; }
        public virtual bool required { get; set; }
        public virtual List<string> choices { get; set; }
        public virtual Dictionary<string,string> options { get; set; }
    }
}
