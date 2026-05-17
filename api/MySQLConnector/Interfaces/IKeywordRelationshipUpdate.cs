using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector.Interfaces
{

    public interface IKeywordRelationshipUpdate
    {
        Task AssociateWithKeyword(int parentId, int keywordId, MySqlConnection? myConnect = null);
    }
}
