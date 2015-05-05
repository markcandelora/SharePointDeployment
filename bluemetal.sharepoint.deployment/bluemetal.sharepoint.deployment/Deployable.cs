using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SP = Microsoft.SharePoint.Client;

namespace bluemetal.sharepoint.deployment {
    public abstract class Deployable {
        private bool? _isDeployed;
        public SharePointContext context { get; protected set; }
        public Web parentWeb { get { return (this.parent == null) ? null : (this.parent as Web) ?? this.parent.parentWeb; } }
        public Site parentSite { get { return (this.parent == null) ? null : (this.parent as Site) ?? this.parent.parentSite; } }
        public Deployable parent { get; set; }
        public virtual bool? isDeployed {
            get {
                return this._isDeployed;
            }
            internal set {
                this._isDeployed = value;
                if (this._isDeployed.HasValue && !this.isDeployed.Value) {
                    this.forEachChild(i => i.isDeployed = false);
                }
            }
        }
        public bool initialized { get; protected set; }

        protected virtual void init(SharePointContext context) {
            this.init(context, false);
        }

        protected virtual void init(SharePointContext context, bool force) {
            if (force || !this.initialized) {
                this.context = context;
                this.onInit();
                this.isDeployed = this.getDeployed(force);
                this.initialized = true;
            }

            this.forEachChild(i => { i.parent = this;
                                     i.init(this.context); });
        }

        protected virtual SP.Web getParentSPWeb() {
            var returnValue = (this.parentWeb != null) ? this.parentWeb.spWeb : null;
            if (returnValue == null) {
                var url = (this.parentWeb != null) ? this.parentWeb.siteRelativeUrl : "/";
                returnValue = this.context.openWeb(url);
            }

            return returnValue;
        }

        public bool getDeployed(bool force) {
            if (force || !this.isDeployed.HasValue) {
                this.isDeployed = this.getDeployed();
            }
            return this.isDeployed.Value;
        }

        public void delete() {
            if (this.isDeployed.Value) {
                this.onDelete();
                this.isDeployed = false;
                this.context.flush();
                this.invalidate();
                this.context.invalidate();
            }
        }

        public void invalidate() {
            if (this.parent != null) this.parent.invalidate();

            if (this.initialized) {
                this.initialized = false;
                this.onInvalidate();
                this.forEachChild(i => i.invalidate());
            }
        }

        internal virtual void create() {
            if (!this.isDeployed.Value) {
                this.onCreate();
            }
            this.forEachChild(i => i.create());
        }

        protected abstract void onInit();
        internal abstract void onInvalidate();
        internal abstract void onCreate();
        internal abstract void onDelete();
        protected abstract bool getDeployed();
        public abstract void forEachChild(Action<Deployable> action);
    }
}
