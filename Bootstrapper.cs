using System;
using System.Collections.Generic;
using System.IO;
using Nancy;
using Nancy.Bootstrapper;
using TinyIoC;

namespace Rest.Service
{
     public class Bootstrapper : DefaultNancyBootstrapper
    {
        byte[] favicon;
        private static IImportador _importador;

        private const int CACHE_SECONDS = 150;
        private readonly Dictionary<string, Tuple<DateTime, Response, int>> cachedResponses = new Dictionary<string, Tuple<DateTime, Response, int>>();

        protected override void  ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            StaticConfiguration.DisableErrorTraces=true;
            _importador = new Importador();

            base.ApplicationStartup(container, pipelines);

            pipelines.BeforeRequest += CheckCache;
            pipelines.AfterRequest += SetCache;
        }

        public void SetCache(NancyContext context)
        {
            if (ValidateCache(context)) return;
            this.cachedResponses[context.Request.Path]= new Tuple<DateTime, Response, int>
                (DateTime.Now, context.Response, CACHE_SECONDS);          
        }    

        public Response CheckCache(NancyContext context)
        {
            if (!ValidateCache(context))
            {

                var monitorPath = string.Format(@"{0}{1}\", Path.GetDirectoryName(typeof(Bootstrapper).Assembly.CodeBase)
                    .Replace(@"file:\", string.Empty)
                    .Replace("bin", string.Empty), "inbox");
                _importador.ConvertFilesInCommands(monitorPath);
            }
            return null;
        }

        private bool ValidateCache(NancyContext context)
        {
            Tuple<DateTime, Response, int> cacheEntry;

            if (this.cachedResponses.TryGetValue(context.Request.Path, out cacheEntry))
                return (cacheEntry.Item1.AddSeconds(cacheEntry.Item3) > DateTime.Now); 
            return false;
        } 

        protected override byte[] DefaultFavIcon
        {
            get
            {
                if (favicon == null)
                {
                    //TODO: remember to replace 'AssemblyName' with the prefix of the resource
                    using (var resourceStream = GetType().Assembly.GetManifestResourceStream("Rest.Service.favicon.ico"))
                    {
                        var tempFavicon = new byte[resourceStream.Length];
                        resourceStream.Read(tempFavicon, 0, (int)resourceStream.Length);
                        favicon = tempFavicon;
                    }
                }
                return favicon;
            }
        }
    }
}