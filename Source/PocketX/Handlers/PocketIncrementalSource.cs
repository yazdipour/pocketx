using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Collections;
using PocketSharp.Models;

namespace PocketX.Handlers
{
    internal class PocketIncrementalSource
    {
        public class Articles : IIncrementalSource<PocketItem>
        {
            public async Task<IEnumerable<PocketItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
                => await PocketHandler.GetInstance().GetListAsync(State.unread, false, null, null, pageSize, pageIndex * pageSize);
        }
        public class Favorites : IIncrementalSource<PocketItem>
        {
            public async Task<IEnumerable<PocketItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
                => await PocketHandler.GetInstance().GetListAsync(State.all, true, null, null, pageSize, pageIndex * pageSize);
        }
        public class Archives : IIncrementalSource<PocketItem>
        {
            public async Task<IEnumerable<PocketItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
                => await PocketHandler.GetInstance().GetListAsync(State.archive, false, null, null, pageSize, pageIndex * pageSize);
        }
    }
}
