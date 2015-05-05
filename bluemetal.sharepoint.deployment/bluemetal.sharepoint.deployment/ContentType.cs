using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bluemetal.sharepoint.deployment {
    public class ContentType : Deployable {
        #region Children
        public List<ListField> Fields { get; set; }
        #endregion

        internal string getSchemaXml() {
            throw new NotImplementedException();
        }

        protected override void onInit() {
            throw new NotImplementedException();
        }

        internal override void onInvalidate() {
            throw new NotImplementedException();
        }

        protected override bool getDeployed() {
            throw new NotImplementedException();
        }

        public override void forEachChild(Action<Deployable> action) {
            throw new NotImplementedException();
        }

        internal override void onCreate() {
            throw new NotImplementedException();
        }

        internal override void onDelete() {
            throw new NotImplementedException();
        }
    }
}
