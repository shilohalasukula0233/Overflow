using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public record QuestionCreated(string QuestionId,string Title,string Content,DateTime Created,List<string> Tags);
}
