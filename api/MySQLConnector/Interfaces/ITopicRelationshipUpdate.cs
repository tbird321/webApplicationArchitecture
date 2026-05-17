using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector.Interfaces
{
    public interface ITopicRelationshipUpdate
    {
        Task AssociateWithTopic(int parentId, int topicId, MySqlConnection? myConnect = null);
    }
}
