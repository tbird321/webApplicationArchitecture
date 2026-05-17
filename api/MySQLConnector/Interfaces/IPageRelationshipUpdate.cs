using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector.Interfaces
{
    public interface IPageRelationshipUpdate
    {
        Task AssociateWithPage(int parentId, int pageId, MySqlConnection? myConnect = null);
    }
}
