using Retlang.Fibers;
using Retlang.Channels;
using Castle.ActiveRecord.Framework.Config;
using System.Reflection;
using Castle.ActiveRecord;
using System;
using System.IO;
using System.Configuration;
using FileProccesor.Services;

namespace Rest.Service
{
    public class Importador:IImportador
    {
        private static IFiber _workerFiber = new PoolFiber();
        private static IChannel<string> _importar= new Channel<string>();

        private static void InitDatabase()
        {
            var config = ActiveRecordSectionHandler.Instance;
            var assemblyLibrary = Assembly.Load("FileProccesor");

            ActiveRecordStarter.Initialize(assemblyLibrary, config);

            switch (ConfigurationManager.AppSettings["Enviroment"])
            {
                case "Development":
                    ActiveRecordStarter.DropSchema();
                    ActiveRecordStarter.CreateSchema();
                    break;
                case "Testing":
                    ActiveRecordStarter.UpdateSchema();
                    break;
                default:
                    break;
            }
        }

        public Importador()
        {
            InitDatabase();
            _workerFiber.Start();
            _importar.Subscribe(_workerFiber, Work);
        }        

        public void ConvertFilesInCommands(string Path)
        {
            foreach (var item in Directory.GetFiles(Path))
            {
                _importar.Publish(item);
            }
        }


        public void Work(string filepath)
        {
            var archivo = filepath.Split(new[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);            

            var importador = ImportadorFactory.ReturnImportador(filepath);

            importador.Persistir(filepath);
            WorkflowFidecard.Registro();            
        }
    }

    public interface IImportador
    {
        void ConvertFilesInCommands(string Path);
    }
}