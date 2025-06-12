using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Jiro.Core.Abstraction;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;

namespace Jiro.Infrastructure.Repositories
{
    public class MessageRepository : BaseRepository<string, Message, JiroContext>, IMessageRepository
    {
        public MessageRepository(JiroContext context) : base(context)
        {

        }
    }
}
