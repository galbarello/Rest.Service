using Nancy;
using System.Dynamic;
using Simple.Data;
using System.Collections.Generic;
using Retlang.Fibers;
using Retlang.Channels;
using System.IO;
namespace Rest.Service
{
    public class HomeModule:BaseModule
    {
        public HomeModule(IDBFactory dbFactory):base(dbFactory ,"/api")
        {
            
            Get["/puntos/{origen}/{empresa}/{id}/{extension}"] = x =>
            {
                return GetPuntos(x);
            };

            Get["/movimientos/{origen}/{empresa}/{id}/{extension}"] = x =>
            {
                return GetMovimientos(x);
            };

            Get["/importador"] = x =>
            {
                return string.Format("Directiorio de operaciones:{0}",
                    string.Format(@"{0}{1}\", Path.GetDirectoryName(typeof(Bootstrapper).Assembly.CodeBase)
                    .Replace(@"file:\", string.Empty)
                    .Replace("bin", string.Empty),"inbox"));
            };
           
        }

        private Response GetMovimientos(dynamic x)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return HttpStatusCode.NotFound;            

            return GetMovimientosForCustomer(x);            
        }

        private Response GetMovimientosForCustomer(dynamic x)
        {
            Punto persona = DB.Personas.FindByNroDocumentoAndCodEmpresa(x.id, x.empresa);
            var cuenta = DB.Clientes.FindByNroDocAndCodEmpresa(x.id, x.empresa);

            var numero_cuenta = cuenta == null ? persona.Cuenta : cuenta.NroCuenta;

            var doc = GetCorporativo(persona.Cuenta, x.empresa);
            var response = new List<Movimiento>();
            if (x.origen == "corporativo")
               response= MovimientosCorporativos(persona.CodEmpresa,persona.Cuenta,doc);
            else
                response = MovimientosParticulares(persona.CodEmpresa, persona.Cuenta, doc);

            if (x.extension == "json")
                return Response.AsJson(response);
            else
                return Response.AsXml(response);
        }

        private IEnumerable<Movimiento> MovimientosParticulares(int empresa,int cuenta,string doc)
        {
            
             return DB.Movimientos_Particulares(empresa,cuenta,doc)                            
                .ToList<Movimiento>();           
            
        }

        private IEnumerable<Movimiento> MovimientosCorporativos(int empresa, int cuenta, string doc)
        {

            return DB.Movimientos_Corporativos(empresa, cuenta, doc)
               .ToList<Movimiento>();        
        }

        private Response GetPuntos(dynamic x)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return HttpStatusCode.NotFound;

            Punto response = DB.Personas.FindByNroDocumentoAndCodEmpresa(x.id, x.empresa);

            return GetPuntosForCustomer(x, response);            
        }

        private Response GetPuntosForCustomer(dynamic x, Punto response)
        {
            if (x.origen == "corporativo")
                PuntosCorporativos(response);
            else
                PuntosParticulares(response);

           if (x.extension == "json")
                return Response.AsJson(response);
            else
                return Response.AsXml(response);
        }

        private void PuntosCorporativos(Punto response)
        {
            var doc = GetCorporativo(response.Cuenta, response.CodEmpresa);

            var SumaPuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                .Where(DB.Cuentas_Corrientes.Movimiento == 0 || DB.Cuentas_Corrientes.Movimiento == 2)
                .Where(DB.Cuentas_Corrientes.NroDoc == doc).ToList();

            var CanjePuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                .Where(DB.Cuentas_Corrientes.Movimiento == 1 || DB.Cuentas_Corrientes.Movimiento == 4)
                .Where(DB.Cuentas_Corrientes.NroDoc == doc).ToList();

            Sumador(response, SumaPuntos, CanjePuntos);
        }

        private static void Sumador(Punto response, dynamic SumaPuntos, dynamic CanjePuntos)
        {
            var puntosSume = SumaPuntos[0].Puntos == null ? 0 : SumaPuntos[0].Puntos;
            var puntosCanje = CanjePuntos[0].Puntos == null ? 0 : CanjePuntos[0].Puntos;

            var montoSuma = SumaPuntos[0].Monto == null ? 0 : SumaPuntos[0].Monto;
            var montoCanje = CanjePuntos[0].Monto == null ? 0 : CanjePuntos[0].Monto;

            response.Puntos = puntosSume - puntosCanje;
            response.MontoCompra = montoSuma - montoCanje;
        }

        private void PuntosParticulares(Punto response)
        {
            var doc = GetCorporativo(response.Cuenta, response.CodEmpresa);

            var SumaPuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                .Where(DB.Cuentas_Corrientes.Movimiento == 0 || DB.Cuentas_Corrientes.Movimiento == 2)
                .Where(DB.Cuentas_Corrientes.NroDoc !=doc).ToList();

            var CanjePuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                .Where(DB.Cuentas_Corrientes.Movimiento == 1 || DB.Cuentas_Corrientes.Movimiento == 4)
                .Where(DB.Cuentas_Corrientes.NroDoc != doc).ToList();

            Sumador(response, SumaPuntos, CanjePuntos);
        }

        private string GetCorporativo(int cuenta, int empresa)
        {
            var corporativo = DB.Personas
                .FindByCodEmpresaAndCuentaAndTipoDocumento(empresa, cuenta, "CUI");
            return corporativo.NroDocumento;
        }        
            
    }    
}