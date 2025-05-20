using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Buf
{
    internal interface BufControllerImpl
    {
        string keywordId { get; }
        string keywordIconId { get; }

        string GetBufName();
        string GetBufActivatedText();

        string GetKeywordText();
    }
}
