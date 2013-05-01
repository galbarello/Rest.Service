using System.Configuration;
using Simple.Data;

namespace Rest.Service
{

    public interface IDBFactory
    {
        dynamic DB();
    }

    public class DBFactory:IDBFactory
    {
        dynamic _db;

        public dynamic DB()
        {
            if (_db == null)
                _db = Database.OpenConnection(ConfigurationManager.ConnectionStrings["database"].ConnectionString);
            return _db;
        }       
    }
}