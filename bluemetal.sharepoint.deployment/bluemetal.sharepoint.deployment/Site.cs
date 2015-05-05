using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bluemetal.sharepoint.deployment {
    public class Site : Deployable {
        public string Url { get; set; }

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
