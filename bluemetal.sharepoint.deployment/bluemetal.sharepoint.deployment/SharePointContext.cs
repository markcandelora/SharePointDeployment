using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using SP = Microsoft.SharePoint.Client;

namespace bluemetal.sharepoint.deployment {
    public class SharePointContext : ISharePointContext {
        private const int MAX_REQUESTS = 10;
        private string _url;
        private ICredentials _credentials;
        private int _pendingRequests = 0;
        private Action _asyncActions;
        private Action _successActions;
        private Action<Exception> _errorActions;

        public SP.ClientContext context { get; set; }
        public SP.Site siteCollection { get { return this.context.Site; } }

        public SharePointContext(string url, ICredentials credentials) {
            this._credentials = credentials;
            this._url = url;
            this.initializeContext();
        }

        public SharePointContext(string url, string userName, string password) {
            SecureString securePwd = new SecureString();
            password.ToList().ForEach(c => securePwd.AppendChar(c));
            this._credentials = new SP.SharePointOnlineCredentials(userName, securePwd);
            this._url = url;
            this.initializeContext();
        }

        protected void initializeContext() {
            this.context = new SP.ClientContext(this._url) {
                Credentials = this._credentials,
                AuthenticationMode = SP.ClientAuthenticationMode.Default
                };
        }

        public SP.Web openWeb(string url) {
            SP.Web returnValue = this.context.Site.OpenWeb(url);
            this.executeAsync(() => { returnValue = this.context.Site.OpenWeb(url); });
            return returnValue;
        }

        public void executeSync(Action action) {
            //this.flush();
            //this._errorActions += (ex) => { throw ex; };
            //action();
            this.executeAsync(action);
            this.flush();
        }

        public void executeSync<T>(T clientObject, params Expression<Func<T, object>>[] retrievals) where T : SP.ClientObject {
            //this.flush();
            //this._errorActions += (ex) => { throw ex; };
            //this.context.Load(clientObject, retrievals);
            this.executeAsync(clientObject, retrievals);
            this.flush();
        }

        public bool tryExecuteSync<T>(T clientObject, params Expression<Func<T, object>>[] retrievals) where T : SP.ClientObject {
            bool returnValue = false;

            try {
                this.executeSync(clientObject, retrievals);
                returnValue = true;
            } catch (SP.ServerException) {
                returnValue = false;
            }

            return returnValue;
        }

        public void executeAsync(Action action) {
            this.executeAsync(action, (Action)null);
        }

        public void executeAsync(Action action, Action onSuccess) {
            this.executeAsync(action, onSuccess, (ex) => { throw ex; });
        }

        public void executeAsync(Action action, Action onSuccess, Action<Exception> onError) {
            if (onSuccess != null) this._successActions += onSuccess;
            if (onError != null) this._errorActions += onError;
            this._asyncActions += action;

            this._pendingRequests++;
            if (this._pendingRequests >= MAX_REQUESTS) {
                this.flush();
            }
        }

        public void executeAsync<T>(T clientObject, params Expression<Func<T, object>>[] retrievals) where T : SP.ClientObject {
            this.executeAsync(clientObject, null, retrievals);
        }

        public void executeAsync<T>(T clientObject, Action onSuccess, params Expression<Func<T, object>>[] retrievals) where T : SP.ClientObject {
            this.executeAsync(clientObject, null, (ex) => { throw ex; }, retrievals);
        }

        public void executeAsync<T>(T clientObject, Action onSuccess, Action<Exception> onError, params Expression<Func<T, object>>[] retrievals) where T : SP.ClientObject {
            if (onSuccess != null) this._successActions += onSuccess;
            if (onError != null) this._errorActions += onError;
            this._asyncActions += () => this.context.Load(clientObject, retrievals);

            this._pendingRequests++;
            if (this._pendingRequests >= MAX_REQUESTS) {
                this.flush();
            }
        }

        public void flush() {
            if (this._pendingRequests > 0 | this.context.HasPendingRequest) {
                Exception remoteError = null;
                bool success = false;
                int retries = 3;
                while (retries-- > 0 && !success) {
                    try {
                        if (this._asyncActions != null) this._asyncActions();
                        this.context.ExecuteQuery();
                        remoteError = null;
                        success = true;
                    } catch (SP.ServerException ex) {
                        remoteError = ex;
                    } catch (Exception ex) {
                        remoteError = ex;
                    }
                }

                var successActions = this._successActions;
                var errorActions = this._errorActions;
                this.resetPendingOperations();

                if (remoteError == null) {
                    if (successActions != null) {
                        successActions();
                    }
                } else {
                    if (errorActions != null) {
                        errorActions(remoteError);
                    }
                }
            }
        }

        protected void resetPendingOperations() {
            this._pendingRequests = 0;
            this._asyncActions = null;
            this._successActions = null;
            this._errorActions = null;
        }

        public void invalidate() {
            this.resetPendingOperations();
            this.context.Dispose();
            this.initializeContext();
        }

        public void Dispose() {
            this.flush();
            try { this.context.Dispose(); } catch { /* Ignore, we're outta here */ }
        }

    }
}
