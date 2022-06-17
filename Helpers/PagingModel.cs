

using System;

namespace App.Helper
{
    public class PagingModel
    {
        public int currentpage { get; set; }
        public int countpage { get; set; }
        public Func<int?, string> generateUrl { get; set; }
    }

}