using Microsoft.SharePoint.Client;
using System;
using System.Linq.Expressions;
using SP = Microsoft.SharePoint.Client;

namespace bluemetal.sharepoint.deployment {
    public interface ISharePointContext : IDisposable {
        SP.ClientContext context { get; set; }
        void Dispose();
        void executeAsync(Action action, Action onSuccess, Action<Exception> onError);
        void executeAsync<T>(T clientObject, Action onSuccess, Action<Exception> onError, params Expression<Func<T, object>>[] retrievals) where T : ClientObject;
        void executeAsync(Action action);
        void executeAsync<T>(T clientObject, params Expression<Func<T, object>>[] retrievals) where T : ClientObject;
        void executeSync(Action action);
        void executeSync<T>(T clientObject, params Expression<Func<T, object>>[] retrievals) where T : ClientObject;
        void flush();
        SP.Web openWeb(string url);
        SP.Site siteCollection { get; }
    }
}
