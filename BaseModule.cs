using Nancy;

namespace Rest.Service
{
    public abstract class BaseModule:NancyModule
    {
        protected dynamic DB;

        public BaseModule(IDBFactory dbFactory,string path ):base(path)
        {
            DB=dbFactory.DB();
        }
    }
}