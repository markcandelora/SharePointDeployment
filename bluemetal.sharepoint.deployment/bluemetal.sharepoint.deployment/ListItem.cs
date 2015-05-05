using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SP = Microsoft.SharePoint.Client;

namespace bluemetal.sharepoint.deployment {
    public class ListItem : Dictionary<string, object> {
        public static ListItem FromSpItem(SP.ListItem item) {
            var returnValue = new ListItem();
            item.FieldValues.ForEach(i => returnValue[i.Key] = i.Value);
            return returnValue;
        }

        internal void applyValues(SP.ListItem spItem) {
            this.ForEach(i => spItem[i.Key] = i.Value);
        }
    }
}
