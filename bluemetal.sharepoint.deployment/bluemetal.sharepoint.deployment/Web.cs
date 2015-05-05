using bluemetal.sharepoint.deployment.utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SP = Microsoft.SharePoint.Client;
using Xml = System.Xml;

namespace bluemetal.sharepoint.deployment {
    public class Web : Deployable, IDisposable {
        internal SP.Web spWeb { get; set; }

        public string siteRelativeUrl {
            get {
                return UrlUtility.GetRelativeUrl(this.parentSite.Url, this.fullUrl);
            }
        }

        public string fullUrl {
            get {
                string returnValue;
                string root;
                if (this.parentWeb == null) {
                    root = this.parentSite.Url;
                } else {
                    root = this.parentWeb.fullUrl;
                }
                returnValue = UrlUtility.JoinUrl(root, this.urlSegment);
                return returnValue;
            }
        }

        public Web() {
            this.features = new List<Guid>();
            this.fields = new List<SiteField>();
            this.webs = new List<Web>();
            this.lists = new List<List>();
        }

        public void deploy() {
            this.create();
            this.context.flush();
        }

        public void init(string userName, string password, bool force) {
            base.init(new SharePointContext(this.fullUrl, userName, password), force);
        }

        public void init(string userName, string password) {
            this.init(userName, password, false);
        }

        public void init(ICredentials credentials, bool force) {
            base.init(new SharePointContext(this.fullUrl, credentials), force);
        }

        public void init(ICredentials credentials) {
            this.init(credentials, false);
        }

        protected override void onInit() { }

        internal override void onInvalidate() {
            this.spWeb = null;
        }

        protected override bool getDeployed() {
            var returnValue = false;
            this.spWeb = this.context.openWeb(this.siteRelativeUrl);
            this.isDeployed = returnValue = this.context.tryExecuteSync(this.spWeb);
            if (!this.isDeployed.Value) {
                this.spWeb = null;
            }
            return returnValue;
        }

        internal override void onCreate() {
            var creationInfo = new SP.WebCreationInformation {
                Title = this.title,
                Url = this.urlSegment,
                Language = this.language,
                Description = this.description,
                UseSamePermissionsAsParentSite = true,
                WebTemplate = this.webTemplate
            };
            SP.Web parentSpWeb = this.getParentSPWeb();
            this.context.executeAsync(() => this.spWeb = parentSpWeb.Webs.Add(creationInfo), () => { this.isDeployed = this.getDeployed(); });
        }

        internal override void onDelete() {
            this.webs.ForEach(i => i.onDelete());
            if (this.spWeb == null) this.getDeployed();
            this.context.executeAsync(() => { this.spWeb.DeleteObject(); }, () => { this.isDeployed = false; this.spWeb = null; });
        }

        public void postDeploy() {
            throw new NotImplementedException();
        }

        public void LoadFromXml(Xml.XmlReader reader) {
            throw new NotImplementedException();
        }

        public override void forEachChild(Action<Deployable> action) {
            if (this.fields != null) this.fields.ForEach(action);
            if (this.lists != null) this.lists.ForEach(action);
            if (this.webs != null) this.webs.ForEach(action);
        }

        #region Definition Info
        public string urlSegment { get; set; }
        public string title { get; set; }
        public string webTemplate { get; set; }
        public string description { get; set; }
        public int language { get; set; }
        #endregion

        #region Children
        public List<Web> webs { get; set; }
        public List<List> lists { get; set; }
        public List<Guid> features { get; set; }
        public List<SiteField> fields { get; set; }
        #endregion

        public void Dispose() {
            if (this.context != null) this.context.Dispose();
            if (this.webs != null) this.webs.ForEach(i => i.Dispose());
        }
    }
}
