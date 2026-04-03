using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts;

public record QuestionUpdated(string QuestionId, string Title, string Content, string[] Tags);
