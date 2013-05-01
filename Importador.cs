using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using FileProccesor.Services;
using Retlang.Channels;
using Retlang.Fibers;

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